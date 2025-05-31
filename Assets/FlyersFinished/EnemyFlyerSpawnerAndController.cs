using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnerAndController : MonoBehaviour
{
    // Event triggered when any enemy dies
    public static event Action OnAnyEnemyDeath;

    // Prefab for the enemy GameObject
    public GameObject enemyPrefab;
    // Collider defining the flight area for enemies
    public BoxCollider flightAreaCollider;

    // Number of enemies to spawn
    public int numberOfEnemies = 5;
    // Base speed for enemy flight
    public float flySpeed = 3f;

    // Ratios to define the minimum and maximum flight height within the collider
    [Range(0f, 1f)]
    public float minFlightHeightRatio = 0.1f;
    [Range(0f, 1f)]
    public float maxFlightHeightRatio = 0.9f;

    // Multiplier for enemy speed during attack
    public float attackSpeedMultiplier = 2f;
    // Cooldown duration between enemy attacks
    public float attackCooldown = 5f;
    // Reference to the player GameObject
    public GameObject player;
    // Delay before an attacking enemy returns to random flight
    public float attackReturnDelay = 1f;

    [Header("Attack Behavior")]
    // Offset for enemy rotation during attack to face the player correctly
    public float attackRotationOffset = 90f;
    // Distance at which an attack is considered successful
    public float attackSuccessDistance = 3f;
    // Damage dealt to the player when hit
    public int damageAmount = 10;

    [Header("Glow Settings")]
    // Color of the enemy glow when attacking
    public Color attackGlowColor = Color.red;
    // Intensity of the glow
    public float glowIntensity = 2.0f;

    [Header("Gizmo Settings")]
    // Color for the attack direction gizmo in the editor
    public Color attackDirectionGizmoColor = Color.magenta;

    [Header("Obstacle Avoidance")]
    // Layer mask for obstacles enemies should avoid
    public LayerMask obstacleLayer;
    // Length of the raycast for obstacle detection
    public float avoidanceRayLength = 2f;
    // Radius of the spherecast for obstacle detection
    public float avoidanceSphereRadius = 0.5f;
    // Force applied to avoid obstacles
    public float avoidanceForce = 5f;
    // Speed at which enemies rotate
    public float rotationSpeed = 5f;

    [Header("Funnel Spawn Settings")]
    // Point where enemies are initially spawned
    public Transform funnelSpawnPoint;
    // Target point enemies move towards after spawning (funneling)
    public Transform funnelTargetPoint;
    // Delay between spawning each enemy
    public float spawnDelay = 0.5f;

    [Header("Spawner Destruction")]
    // Delay before the spawner destroys itself after all enemies are defeated
    public float spawnerDestroyDelay = 2f;

    // List of currently spawned enemy GameObjects
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    // Dictionary to store coroutines for each enemy's flight behavior
    private Dictionary<GameObject, Coroutine> enemyFlightCoroutines = new Dictionary<GameObject, Coroutine>();
    // Timestamp of the last attack
    private float lastAttackTime;
    // Dictionary to store the current target position for each enemy
    private Dictionary<GameObject, Vector3> enemyCurrentTargets = new Dictionary<GameObject, Vector3>();
    // HashSet to track enemies that have completed the initial funneling phase
    private HashSet<GameObject> funnelCompletedEnemies = new HashSet<GameObject>();

    // Flag to track if all initial enemies have been spawned and have completed their funneling
    // This flag is no longer used for spawner destruction, but kept for potential other logic.
    private bool initialSpawningAndFunnelingComplete = false;

    // NEW: Flag to track if all enemies specified by 'numberOfEnemies' have been instantiated
    private bool allEnemiesInstantiated = false;

    void Start()
    {
        // Validate required assignments in the Inspector
        if (enemyPrefab == null)
        {
            UnityEngine.Debug.LogError("Enemy Prefab is not assigned! Please assign an enemy prefab in the Inspector.");
            return;
        }
        if (flightAreaCollider == null)
        {
            UnityEngine.Debug.LogError("Flight Area Collider is not assigned! Please assign a Box Collider in the Inspector.");
            return;
        }
        if (funnelSpawnPoint == null)
        {
            UnityEngine.Debug.LogError("Funnel Spawn Point is not assigned! Please assign a Transform in the Inspector.");
            return;
        }
        if (funnelTargetPoint == null)
        {
            UnityEngine.Debug.LogError("Funnel Target Point is not assigned! Please assign a Transform in the Inspector.");
            return;
        }

        // Automatically assign the player if the Main Camera is tagged correctly
        if (Camera.main != null)
        {
            player = Camera.main.gameObject;
            UnityEngine.Debug.Log("Player automatically assigned to Main Camera.");
        }
        else
        {
            UnityEngine.Debug.LogError("No Main Camera found! Please ensure your main camera is tagged 'MainCamera' in the Inspector.");
            return;
        }

        // Start the sequential spawning process
        StartCoroutine(SpawnEnemiesSequentially());
        // Initialize last attack time to allow immediate first attack if conditions met
        lastAttackTime = -attackCooldown;
    }

    void Update()
    {
        // Check if it's time for an attack and if there are enemies ready to attack
        if (Time.time - lastAttackTime > attackCooldown && funnelCompletedEnemies.Count > 0)
        {
            StartCoroutine(AttackPlayer());
            lastAttackTime = Time.time;
        }

        // NEW: Backup mechanism to destroy the spawner if it gets stuck.
        // This triggers if all enemies were initially instantiated, and
        // the spawner's transform only has its two funnel points as children remaining.
        // 'numberOfEnemies > 0' ensures this backup doesn't trigger immediately if no enemies are meant to spawn.
        // 'this.enabled' check prevents multiple destruction attempts if already triggered.
        if (allEnemiesInstantiated && transform.childCount == 2 && numberOfEnemies > 0 && this.enabled)
        {
            UnityEngine.Debug.LogWarning("Backup Trigger: Spawner detected only funnel point children after all enemies were instantiated. Initiating destruction safety.");
            StartCoroutine(DestroySpawnerAfterDelay());
            this.enabled = false; // Disable script to prevent repeated checks
        }
    }

    /// <summary>
    /// Spawns enemies one by one with a delay and initiates their initial funneling flight.
    /// After all enemies are spawned, it waits for them to complete funneling.
    /// </summary>
    IEnumerator SpawnEnemiesSequentially()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            // Instantiate enemy at the funnel spawn point
            GameObject newEnemy = Instantiate(enemyPrefab, funnelSpawnPoint.position, Quaternion.identity);
            newEnemy.transform.parent = this.transform; // Parent to spawner for organization

            spawnedEnemies.Add(newEnemy); // Add to list of spawned enemies

            SetEnemyGlow(newEnemy, false); // Ensure glow is off initially

            // Set initial target for funneling
            enemyCurrentTargets[newEnemy] = funnelTargetPoint.position;

            // Start the flight coroutine for the new enemy
            Coroutine flightCoroutine = StartCoroutine(FlyAround(newEnemy));
            enemyFlightCoroutines.Add(newEnemy, flightCoroutine);

            yield return new WaitForSeconds(spawnDelay); // Wait before spawning next enemy
        }

        // NEW: Mark that all initial enemies have been instantiated.
        allEnemiesInstantiated = true;

        // After all enemies are instantiated, wait for them to complete their initial funneling
        // This loop ensures 'initialSpawningAndFunnelingComplete' is only set when all enemies
        // have reached the funnelTargetPoint at least once.
        while (funnelCompletedEnemies.Count < numberOfEnemies)
        {
            yield return null; // Wait for the next frame
        }

        initialSpawningAndFunnelingComplete = true; // Mark initial setup as complete
        UnityEngine.Debug.Log("All initial enemies have completed funneling. Spawner's initial job is done.");
    }

    /// <summary>
    /// Generates a random position within the defined flight area collider bounds,
    /// respecting the minimum and maximum flight height ratios.
    /// </summary>
    /// <returns>A random Vector3 position within the flight area.</returns>
    Vector3 GetRandomPositionInColliderBounds()
    {
        Bounds bounds = flightAreaCollider.bounds;

        // Calculate flight height limits based on ratios
        float minFlightY = bounds.min.y + (bounds.size.y * minFlightHeightRatio);
        float maxFlightY = bounds.min.y + (bounds.size.y * maxFlightHeightRatio);

        // Generate random coordinates within the bounds
        float randomX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
        float randomY = UnityEngine.Random.Range(minFlightY, maxFlightY);
        float randomZ = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }

    /// <summary>
    /// Coroutine for enemy flight behavior, including moving towards a target,
    /// obstacle avoidance, and marking funnel completion.
    /// </summary>
    /// <param name="enemy">The enemy GameObject to control.</param>
    IEnumerator FlyAround(GameObject enemy)
    {
        SetEnemyGlow(enemy, false); // Ensure glow is off when flying randomly

        // Get the current target, or a new random one if not set
        Vector3 targetPosition = enemyCurrentTargets.ContainsKey(enemy) ? enemyCurrentTargets[enemy] : GetRandomPositionInColliderBounds();

        while (true)
        {
            // Exit coroutine if enemy is null or inactive
            if (enemy == null || !enemy.activeInHierarchy)
            {
                yield break;
            }

            // Check if enemy has reached its current target
            if (UnityEngine.Vector3.Distance(enemy.transform.position, targetPosition) < 0.5f)
            {
                // If the target was the funnel target and enemy hasn't completed funneling yet, mark it
                if (UnityEngine.Vector3.Distance(targetPosition, funnelTargetPoint.position) < 0.1f && !funnelCompletedEnemies.Contains(enemy))
                {
                    funnelCompletedEnemies.Add(enemy);
                    UnityEngine.Debug.Log($"Enemy {enemy.name} completed funneling and is now ready to attack.");
                }

                // Set a new random target position
                targetPosition = GetRandomPositionInColliderBounds();
                enemyCurrentTargets[enemy] = targetPosition;
            }

            UnityEngine.Vector3 directionToTarget = (targetPosition - enemy.transform.position).normalized;
            UnityEngine.Vector3 finalMoveDirection = directionToTarget;

            // Obstacle avoidance logic using SphereCast
            RaycastHit hit;
            if (UnityEngine.Physics.SphereCast(enemy.transform.position, avoidanceSphereRadius, enemy.transform.forward, out hit, avoidanceRayLength, obstacleLayer))
            {
                UnityEngine.Vector3 hitNormal = hit.normal;
                UnityEngine.Vector3 avoidanceDirection = UnityEngine.Vector3.zero;

                // Determine avoidance direction based on hit normal
                float dotProductUp = UnityEngine.Vector3.Dot(enemy.transform.up, hitNormal);
                if (Mathf.Abs(dotProductUp) > 0.8f) // If hitting a ceiling or floor
                {
                    avoidanceDirection = UnityEngine.Vector3.Cross(enemy.transform.forward, enemy.transform.up).normalized * (Mathf.Sign(dotProductUp) * -1);
                }
                else // If hitting a wall
                {
                    avoidanceDirection = UnityEngine.Vector3.Cross(enemy.transform.up, hitNormal).normalized;
                    // Add vertical component to avoidance for better navigation
                    if (enemy.transform.position.y < flightAreaCollider.bounds.center.y)
                        avoidanceDirection += UnityEngine.Vector3.up * 0.5f;
                    else
                        avoidanceDirection += UnityEngine.Vector3.down * 0.5f;
                }

                // Smoothly blend towards avoidance direction
                finalMoveDirection = UnityEngine.Vector3.Lerp(finalMoveDirection, avoidanceDirection.normalized, avoidanceForce * Time.deltaTime).normalized;
                if (finalMoveDirection == UnityEngine.Vector3.zero) finalMoveDirection = directionToTarget; // Fallback
            }

            // Rotate enemy to face the movement direction
            if (finalMoveDirection != UnityEngine.Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(finalMoveDirection);
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Move the enemy
            enemy.transform.position = UnityEngine.Vector3.MoveTowards(enemy.transform.position, enemy.transform.position + finalMoveDirection, flySpeed * Time.deltaTime);

            yield return null; // Wait for the next frame
        }
    }

    /// <summary>
    /// Coroutine for an enemy attacking the player.
    /// An eligible enemy is chosen, flies towards the player, deals damage, and then returns to random flight.
    /// </summary>
    IEnumerator AttackPlayer()
    {
        // Ensure there are enemies ready to attack and the player exists
        if (funnelCompletedEnemies.Count == 0) yield break;
        if (player == null)
        {
            UnityEngine.Debug.LogWarning("Player GameObject is null. Cannot initiate attack.");
            yield break;
        }

        // Select a random enemy from those that have completed funneling
        List<GameObject> eligibleAttackers = new List<GameObject>(funnelCompletedEnemies);
        if (eligibleAttackers.Count == 0) yield break;

        GameObject attackingEnemy = eligibleAttackers[UnityEngine.Random.Range(0, eligibleAttackers.Count)];

        // Validate the chosen enemy
        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy) yield break;

        // Remove the enemy from the funnelCompletedEnemies set as it's now attacking
        funnelCompletedEnemies.Remove(attackingEnemy);

        // Stop the current flight coroutine for the attacking enemy
        if (enemyFlightCoroutines.ContainsKey(attackingEnemy) && enemyFlightCoroutines[attackingEnemy] != null)
        {
            StopCoroutine(enemyFlightCoroutines[attackingEnemy]);
            enemyFlightCoroutines.Remove(attackingEnemy);
        }

        SetEnemyGlow(attackingEnemy, true); // Enable glow to indicate attack mode

        float originalFlySpeed = flySpeed;
        float currentEnemyAttackSpeed = originalFlySpeed;

        // Move towards the player until attack success distance is reached
        while (attackingEnemy != null && attackingEnemy.activeInHierarchy && player != null && UnityEngine.Vector3.Distance(attackingEnemy.transform.position, player.transform.position) > attackSuccessDistance)
        {
            // Lerp speed to attack speed
            currentEnemyAttackSpeed = Mathf.Lerp(currentEnemyAttackSpeed, originalFlySpeed * attackSpeedMultiplier, Time.deltaTime * 5f);

            UnityEngine.Vector3 moveDirection = (player.transform.position - attackingEnemy.transform.position).normalized;

            // Rotate enemy to face the player, with an offset
            UnityEngine.Vector3 directionToPlayerRaw = (player.transform.position - attackingEnemy.transform.position).normalized;
            if (directionToPlayerRaw != UnityEngine.Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayerRaw);
                targetRotation *= Quaternion.Euler(0, attackRotationOffset, 0); // Apply attack rotation offset
                attackingEnemy.transform.rotation = Quaternion.Slerp(attackingEnemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * attackSpeedMultiplier);
            }

            // CORRECTED: Move the attacking enemy towards its current position + moveDirection
            attackingEnemy.transform.position = UnityEngine.Vector3.MoveTowards(attackingEnemy.transform.position, attackingEnemy.transform.position + moveDirection, currentEnemyAttackSpeed * Time.deltaTime);

            enemyCurrentTargets[attackingEnemy] = player.transform.position; // Update target for gizmo

            yield return null;
        }

        // If enemy became null or inactive during attack, reset glow and exit
        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy)
        {
            SetEnemyGlow(attackingEnemy, false);
            yield break;
        }

        // Apply damage to the player
        UnityEngine.Debug.Log($"Flier {attackingEnemy.name} reached attack success distance and hit the player.");
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
            UnityEngine.Debug.Log($"Flier {attackingEnemy.name} dealt {damageAmount} damage to the player.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Flier {attackingEnemy.name} could not find PlayerHealth on the player.");
        }

        // Set a new random target for the enemy to return to random flight
        enemyCurrentTargets[attackingEnemy] = GetRandomPositionInColliderBounds();

        yield return new WaitForSeconds(attackReturnDelay); // Wait before returning to flight

        // If enemy became null or inactive during delay, reset glow and exit
        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy)
        {
            SetEnemyGlow(attackingEnemy, false);
            yield break;
        }

        // Restart the flight coroutine for the enemy
        Coroutine newFlightCoroutine = StartCoroutine(FlyAround(attackingEnemy));
        enemyFlightCoroutines.Add(attackingEnemy, newFlightCoroutine);

        // Add the enemy back to the funnelCompletedEnemies set, ready for next attack cycle
        funnelCompletedEnemies.Add(attackingEnemy);
    }

    /// <summary>
    /// Enables or disables the emission glow on an enemy's material.
    /// </summary>
    /// <param name="enemy">The enemy GameObject.</param>
    /// <param name="enableGlow">True to enable glow, false to disable.</param>
    void SetEnemyGlow(GameObject enemy, bool enableGlow)
    {
        if (enemy == null) return;

        Renderer enemyRenderer = enemy.GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            Material enemyMaterial = enemyRenderer.material;

            if (enableGlow)
            {
                enemyMaterial.EnableKeyword("_EMISSION"); // Enable emission
                enemyMaterial.SetColor("_EmissionColor", attackGlowColor * glowIntensity); // Set glow color and intensity
            }
            else
            {
                enemyMaterial.DisableKeyword("_EMISSION"); // Disable emission
                enemyMaterial.SetColor("_EmissionColor", UnityEngine.Color.black); // Set emission color to black
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Enemy {enemy.name} has no Renderer component to apply glow to.");
        }
    }

    /// <summary>
    /// Called by an enemy when it dies. Cleans up references and checks for spawner destruction.
    /// </summary>
    /// <param name="deadEnemy">The GameObject of the enemy that died.</param>
    public void NotifyEnemyDeath(GameObject deadEnemy)
    {
        UnityEngine.Debug.Log($"Spawner received death notification for {deadEnemy.name}. Stopping flight and cleaning up.");

        SetEnemyGlow(deadEnemy, false); // Ensure glow is off for dead enemy

        // Stop and remove the flight coroutine for the dead enemy
        if (enemyFlightCoroutines.ContainsKey(deadEnemy))
        {
            StopCoroutine(enemyFlightCoroutines[deadEnemy]);
            enemyFlightCoroutines.Remove(deadEnemy);
        }

        // Remove the dead enemy from all tracking lists
        spawnedEnemies.Remove(deadEnemy);
        funnelCompletedEnemies.Remove(deadEnemy);
        enemyCurrentTargets.Remove(deadEnemy);

        OnAnyEnemyDeath?.Invoke(); // Invoke the static event

        // If all enemies are defeated, start the spawner destruction countdown.
        if (spawnedEnemies.Count == 0)
        {
            UnityEngine.Debug.Log("All enemies defeated! Starting spawner destruction countdown.");
            StartCoroutine(DestroySpawnerAfterDelay());
        }
    }

    /// <summary>
    /// Coroutine to destroy the spawner GameObject after a delay.
    /// </summary>
    IEnumerator DestroySpawnerAfterDelay()
    {
        yield return new WaitForSeconds(spawnerDestroyDelay);
        UnityEngine.Debug.Log("Destroying spawner.");
        Destroy(gameObject);
    }

    /// <summary>
    /// Draws Gizmos in the editor for visualization of flight area, spawn points,
    /// and enemy targets/attack directions.
    /// </summary>
    void OnDrawGizmos()
    {
        // Draw flight area collider bounds
        if (flightAreaCollider != null)
        {
            UnityEngine.Gizmos.color = UnityEngine.Color.green;
            UnityEngine.Gizmos.DrawWireCube(flightAreaCollider.bounds.center, flightAreaCollider.bounds.size);

            // Draw flight height limits
            Bounds bounds = flightAreaCollider.bounds;
            float minFlightY = bounds.min.y + (bounds.size.y * minFlightHeightRatio);
            float maxFlightY = bounds.min.y + (bounds.size.y * maxFlightHeightRatio);
            UnityEngine.Vector3 heightCenter = new UnityEngine.Vector3(bounds.center.x, (minFlightY + maxFlightY) / 2f, bounds.center.z);
            UnityEngine.Vector3 heightSize = new UnityEngine.Vector3(bounds.size.x, maxFlightY - minFlightY, bounds.size.z);
            UnityEngine.Gizmos.color = UnityEngine.Color.cyan;
            UnityEngine.Gizmos.DrawWireCube(heightCenter, heightSize);
        }

        // Draw funnel spawn and target points
        if (funnelSpawnPoint != null)
        {
            UnityEngine.Gizmos.color = UnityEngine.Color.yellow;
            UnityEngine.Gizmos.DrawSphere(funnelSpawnPoint.position, 0.5f);
            if (funnelTargetPoint != null)
            {
                UnityEngine.Gizmos.DrawLine(funnelSpawnPoint.position, funnelTargetPoint.position);
            }
        }
        if (funnelTargetPoint != null)
        {
            UnityEngine.Gizmos.color = UnityEngine.Color.blue;
            UnityEngine.Gizmos.DrawSphere(funnelTargetPoint.position, 0.5f);
        }

        // Draw lines to current targets or player for each enemy
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy == null || player == null) continue;

            // If enemy is attacking (not in flight coroutine), draw line to player
            if (!enemyFlightCoroutines.ContainsKey(enemy))
            {
                UnityEngine.Gizmos.color = attackDirectionGizmoColor;
                UnityEngine.Gizmos.DrawLine(enemy.transform.position, player.transform.position);
            }
            // If enemy is flying, draw line to its current random target
            else if (enemyCurrentTargets.ContainsKey(enemy))
            {
                // Differentiate color based on funnel completion
                if (funnelCompletedEnemies.Contains(enemy))
                {
                    UnityEngine.Gizmos.color = UnityEngine.Color.white; // Ready to attack
                }
                else
                {
                    UnityEngine.Gizmos.color = UnityEngine.Color.grey; // Still funneling
                }
                UnityEngine.Gizmos.DrawLine(enemy.transform.position, enemyCurrentTargets[enemy]);
                UnityEngine.Gizmos.DrawSphere(enemyCurrentTargets[enemy], 0.2f);
            }
        }
    }
}
