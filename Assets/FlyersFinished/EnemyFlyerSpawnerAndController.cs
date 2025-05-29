using UnityEngine;
using System.Collections; // Required for IEnumerator
using System.Collections.Generic; // Required for List and Dictionary

public class EnemySpawnerAndController : MonoBehaviour
{
    public GameObject enemyPrefab; // Assign your main enemy prefab in the Inspector
    public BoxCollider flightAreaCollider; // Assign the Box Collider that defines the flight and spawn area

    public int numberOfEnemies = 5; // How many enemies to spawn
    public float flySpeed = 3f; // Speed at which enemies fly

    [Range(0f, 1f)] // Restrict to 0-1 for ratio
    public float minFlightHeightRatio = 0.1f; // Min height as a ratio of collider's total height (0 = bottom, 1 = top)
    [Range(0f, 1f)]
    public float maxFlightHeightRatio = 0.9f; // Max height as a ratio of collider's total height (0 = bottom, 1 = top)

    public float attackSpeedMultiplier = 2f; // Speed multiplier when an enemy attacks
    public float attackCooldown = 5f; // Time between attacks
    public GameObject player; // Assign the Player GameObject in the Inspector
    public float attackReturnDelay = 1f; // Delay before enemy returns to pattern after attack

    [Header("Attack Behavior")]
    public float attackRotationOffset = 90f; // Degrees to offset rotation when attacking (e.g., for a side attack)
    public float attackSuccessDistance = 3f; // Distance from player at which the attack is considered successful

    [Header("Glow Settings")]
    public Color attackGlowColor = Color.red; // Color for the attacking enemy's glow
    public float glowIntensity = 2.0f; // Intensity of the glow (e.g., 1.0 to 5.0, directly scales EmissionColor)

    // Gizmos are commented out in OnDrawGizmos, so this variable is no longer explicitly used for drawing.
    // However, it's kept as a public variable as it might be useful for future debugging or re-enabling Gizmos.
    // [Header("Gizmo Settings")]
    // public Color attackDirectionGizmoColor = Color.magenta; 

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer; // Assign the layer(s) your obstacles are on (e.g., "Default", "Environment")
    public float avoidanceRayLength = 2f; // How far ahead to cast a ray for obstacles
    public float avoidanceSphereRadius = 0.5f; // Radius of the sphere for obstacle detection
    public float avoidanceForce = 5f;          // How strongly to turn away from detected obstacles
    public float rotationSpeed = 5f;           // How fast the enemy rotates to face its movement direction

    [Header("Funnel Spawn Settings")]
    public Transform funnelSpawnPoint; // The starting point for enemies to spawn
    public Transform funnelTargetPoint; // The point enemies funnel towards before regular flight
    public float spawnDelay = 0.5f; // Delay between spawning each enemy

    [Header("Spawner Destruction")]
    public float spawnerDestroyDelay = 2f; // Delay before the spawner is destroyed after all enemies are defeated

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Dictionary<GameObject, Coroutine> enemyFlightCoroutines = new Dictionary<GameObject, Coroutine>();
    private float lastAttackTime;
    private Dictionary<GameObject, Vector3> enemyCurrentTargets = new Dictionary<GameObject, Vector3>();
    private HashSet<GameObject> funnelCompletedEnemies = new HashSet<GameObject>();


    void Start()
    {
        // --- Validation Checks ---
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Prefab is not assigned! Please assign an enemy prefab in the Inspector.");
            return;
        }
        if (flightAreaCollider == null)
        {
            Debug.LogError("Flight Area Collider is not assigned! Please assign a Box Collider in the Inspector.");
            return;
        }
        if (player == null)
        {
            Debug.LogError("Player GameObject is not assigned! Please assign the Player GameObject in the Inspector.");
            return;
        }
        if (funnelSpawnPoint == null)
        {
            Debug.LogError("Funnel Spawn Point is not assigned! Please assign a Transform in the Inspector.");
            return;
        }
        if (funnelTargetPoint == null)
        {
            Debug.LogError("Funnel Target Point is not assigned! Please assign a Transform in the Inspector.");
            return;
        }

        StartCoroutine(SpawnEnemiesSequentially());
        lastAttackTime = -attackCooldown; // Allow immediate attack (this will now be gated by funnelCompletedEnemies)
    }

    void Update()
    {
        // Only allow an attack if enough time has passed AND there are enemies that have completed funneling.
        if (Time.time - lastAttackTime > attackCooldown && funnelCompletedEnemies.Count > 0)
        {
            StartCoroutine(AttackPlayer());
            lastAttackTime = Time.time;
        }
    }

    /// <summary>
    /// Coroutine to spawn enemies one by one with a delay, and set their initial target to funnelTargetPoint.
    /// </summary>
    IEnumerator SpawnEnemiesSequentially()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            // Instantiate the enemy at the designated funnel spawn point.
            GameObject newEnemy = Instantiate(enemyPrefab, funnelSpawnPoint.position, Quaternion.identity);
            // Make the new enemy a child of this spawner GameObject.
            newEnemy.transform.parent = this.transform;

            spawnedEnemies.Add(newEnemy);

            // Ensure the enemy is not glowing initially
            SetEnemyGlow(newEnemy, false); // Turn off glow on spawn

            // Set the initial target for the enemy to the funnel target point.
            enemyCurrentTargets[newEnemy] = funnelTargetPoint.position;

            // Start the flight coroutine for the new enemy.
            Coroutine flightCoroutine = StartCoroutine(FlyAround(newEnemy));
            enemyFlightCoroutines.Add(newEnemy, flightCoroutine);

            // Wait for the specified delay before spawning the next enemy.
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    /// <summary>
    /// Returns a random position within the defined flight area collider bounds,
    /// respecting the min/max flight height ratios.
    /// </summary>
    Vector3 GetRandomPositionInColliderBounds()
    {
        Bounds bounds = flightAreaCollider.bounds;

        float minFlightY = bounds.min.y + (bounds.size.y * minFlightHeightRatio);
        float maxFlightY = bounds.min.y + (bounds.size.y * maxFlightHeightRatio);

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(minFlightY, maxFlightY);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }

    /// <summary>
    /// Coroutine for enemy flight behavior, including funneling, random flight, and obstacle avoidance.
    /// </summary>
    /// <param name="enemy">The enemy GameObject to control.</param>
    IEnumerator FlyAround(GameObject enemy)
    {
        // Ensure glow is off when flying around normally
        SetEnemyGlow(enemy, false); // Ensure glow is off when in normal flight

        // Initialize targetPosition with the enemy's current target (either funnel target or random).
        Vector3 targetPosition = enemyCurrentTargets.ContainsKey(enemy) ? enemyCurrentTargets[enemy] : GetRandomPositionInColliderBounds();

        while (true)
        {
            // IMPORTANT: Check if the enemy GameObject still exists and is active.
            // If the enemy has been destroyed or is dying, this coroutine should stop.
            if (enemy == null || !enemy.activeInHierarchy)
            {
                yield break;
            }

            // If the enemy has reached its current target (within a small threshold),
            // get a new random target within the flight bounds.
            // This also handles the transition from funneling to regular flight.
            if (Vector3.Distance(enemy.transform.position, targetPosition) < 0.5f)
            {
                // If the enemy just reached the funnel target point, mark it as ready to attack.
                if (Vector3.Distance(targetPosition, funnelTargetPoint.position) < 0.1f && !funnelCompletedEnemies.Contains(enemy))
                {
                    funnelCompletedEnemies.Add(enemy);
                    Debug.Log($"Enemy {enemy.name} completed funneling and is now ready to attack.");
                }

                targetPosition = GetRandomPositionInColliderBounds(); // Get a new random target
                enemyCurrentTargets[enemy] = targetPosition; // Update the dictionary with the new target
            }

            // Calculate the desired direction towards the current target.
            Vector3 directionToTarget = (targetPosition - enemy.transform.position).normalized;
            Vector3 finalMoveDirection = directionToTarget; // Start with target direction as the base.

            // --- Obstacle Avoidance ---
            RaycastHit hit;
            // Cast a sphere-shaped ray forward to detect obstacles.
            if (Physics.SphereCast(enemy.transform.position, avoidanceSphereRadius, enemy.transform.forward, out hit, avoidanceRayLength, obstacleLayer))
            {
                Vector3 hitNormal = hit.normal; // Normal of the surface hit by the ray.
                Vector3 avoidanceDirection = Vector3.zero;

                // Determine avoidance direction based on the hit normal.
                // If hitting a mostly vertical surface (dot product with up is close to 1 or -1),
                // turn left or right.
                float dotProductUp = Vector3.Dot(enemy.transform.up, hitNormal);
                if (Mathf.Abs(dotProductUp) > 0.8f) // If hit normal is mostly aligned with enemy's up/down
                {
                    // Avoid by turning perpendicular to current forward and up.
                    avoidanceDirection = Vector3.Cross(enemy.transform.forward, enemy.transform.up).normalized * (Mathf.Sign(dotProductUp) * -1);
                }
                else // If hitting a more horizontal or angled surface
                {
                    // Avoid by turning perpendicular to enemy's up and hit normal.
                    avoidanceDirection = Vector3.Cross(enemy.transform.up, hitNormal).normalized;
                    // Add a slight vertical component to avoid getting stuck on floors/ceilings.
                    if (enemy.transform.position.y < flightAreaCollider.bounds.center.y)
                        avoidanceDirection += Vector3.up * 0.5f;
                    else
                        avoidanceDirection += Vector3.down * 0.5f;
                }

                // Lerp between the target direction and the avoidance direction for smooth turning.
                finalMoveDirection = Vector3.Lerp(finalMoveDirection, avoidanceDirection.normalized, avoidanceForce * Time.deltaTime).normalized;
                // Fallback if avoidance calculation results in zero vector.
                if (finalMoveDirection == Vector3.zero) finalMoveDirection = directionToTarget;
            }

            // --- Rotation ---
            // Only rotate if there's a valid movement direction.
            if (finalMoveDirection != Vector3.zero)
            {
                // Calculate the target rotation to look towards the movement direction.
                Quaternion targetRotation = Quaternion.LookRotation(finalMoveDirection);
                // Smoothly interpolate the enemy's rotation towards the target rotation.
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // --- Movement ---
            // Move the enemy in the final calculated direction.
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, enemy.transform.position + finalMoveDirection, flySpeed * Time.deltaTime);

            yield return null; // Wait for the next frame.
        }
    }

    /// <summary>
    /// Coroutine for an enemy to attack the player.
    /// </summary>
    IEnumerator AttackPlayer()
    {
        // Only proceed if there are enemies ready to attack.
        if (funnelCompletedEnemies.Count == 0) yield break;

        // Select a random enemy ONLY from those that have completed funneling.
        List<GameObject> eligibleAttackers = new List<GameObject>(funnelCompletedEnemies);
        GameObject attackingEnemy = eligibleAttackers[Random.Range(0, eligibleAttackers.Count)];

        // Ensure the selected enemy hasn't died while waiting for its turn
        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy) yield break;

        // Temporarily remove the attacking enemy from the ready list so it's not picked again immediately.
        funnelCompletedEnemies.Remove(attackingEnemy);

        // Set the attacking enemy's initial target for the attack phase to the player's position.
        // This captures the player's position when the attack starts.
        Vector3 attackTarget = player.transform.position;
        enemyCurrentTargets[attackingEnemy] = attackTarget; // Store this for Gizmos if needed

        // Stop the current flight coroutine for the attacking enemy.
        if (enemyFlightCoroutines.ContainsKey(attackingEnemy) && enemyFlightCoroutines[attackingEnemy] != null)
        {
            StopCoroutine(enemyFlightCoroutines[attackingEnemy]);
            enemyFlightCoroutines.Remove(attackingEnemy);
        }

        SetEnemyGlow(attackingEnemy, true); // Turn on glow when attacking!

        float originalFlySpeed = flySpeed;
        float currentEnemyAttackSpeed = originalFlySpeed;

        // --- Attack Phase ---
        // Continue attacking until the enemy is within attackSuccessDistance of the attack target.
        while (attackingEnemy != null && attackingEnemy.activeInHierarchy && Vector3.Distance(attackingEnemy.transform.position, attackTarget) > attackSuccessDistance)
        {
            // Smoothly increase the enemy's speed towards the attack speed multiplier.
            currentEnemyAttackSpeed = Mathf.Lerp(currentEnemyAttackSpeed, originalFlySpeed * attackSpeedMultiplier, Time.deltaTime * 5f);

            // Movement is directly towards the stored attackTarget.
            Vector3 moveDirection = (attackTarget - attackingEnemy.transform.position).normalized;

            // Rotation includes the attackRotationOffset.
            if (player != null) // Keep rotation oriented towards the live player if available
            {
                Vector3 directionToPlayerRaw = (player.transform.position - attackingEnemy.transform.position).normalized;
                if (directionToPlayerRaw != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayerRaw);
                    targetRotation *= Quaternion.Euler(0, attackRotationOffset, 0);
                    attackingEnemy.transform.rotation = Quaternion.Slerp(attackingEnemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * attackSpeedMultiplier);
                }
            }

            // Move the attacking enemy towards the attack target.
            attackingEnemy.transform.position = Vector3.MoveTowards(attackingEnemy.transform.position, attackingEnemy.transform.position + moveDirection, currentEnemyAttackSpeed * Time.deltaTime);

            yield return null; // Wait for the next frame.
        }

        // --- Post-Attack Phase ---
        // Check if the enemy is still valid.
        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy)
        {
            SetEnemyGlow(attackingEnemy, false); // Ensure glow is off if enemy was destroyed mid-attack
            yield break;
        }

        // IMMEDIATELY set a new random target for the enemy to start heading towards
        // after the attack is considered successful, while the delay still plays out.
        enemyCurrentTargets[attackingEnemy] = GetRandomPositionInColliderBounds();

        // Wait for the specified delay after the attack has reached its success distance.
        yield return new WaitForSeconds(attackReturnDelay);

        // Check if the enemy is still valid after the delay.
        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy)
        {
            SetEnemyGlow(attackingEnemy, false); // Ensure glow is off if enemy was destroyed during delay
            yield break;
        }

        // Resume the enemy's regular flight pattern.
        // If the enemy hasn't been destroyed, start the FlyAround coroutine with its new target.
        Coroutine newFlightCoroutine = StartCoroutine(FlyAround(attackingEnemy));
        enemyFlightCoroutines.Add(attackingEnemy, newFlightCoroutine);

        // Add the enemy back to the funnelCompletedEnemies set so it can attack again later.
        funnelCompletedEnemies.Add(attackingEnemy);
    }

    /// <summary>
    /// Controls the emission (glow) of an enemy's material.
    /// Requires the enemy's material to have the Emission property enabled in its shader (e.g., Standard, URP Lit).
    /// </summary>
    /// <param name="enemy">The enemy GameObject.</param>
    /// <param name="enableGlow">True to enable glow, false to disable.</param>
    void SetEnemyGlow(GameObject enemy, bool enableGlow)
    {
        if (enemy == null) return;

        Renderer enemyRenderer = enemy.GetComponentInChildren<Renderer>(); // Use GetComponentsInChildren to catch models with sub-meshes
        if (enemyRenderer != null)
        {
            // Access the material. Using .material creates a new instance, .sharedMaterial changes the asset.
            // For per-instance glow, .material is correct.
            Material enemyMaterial = enemyRenderer.material;

            if (enableGlow)
            {
                // Enable emission and set the color
                enemyMaterial.EnableKeyword("_EMISSION");
                enemyMaterial.SetColor("_EmissionColor", attackGlowColor * glowIntensity);
            }
            else
            {
                // Disable emission
                enemyMaterial.DisableKeyword("_EMISSION");
                enemyMaterial.SetColor("_EmissionColor", Color.black); // Reset emission color to black
            }
        }
        else
        {
            Debug.LogWarning($"Enemy {enemy.name} has no Renderer component to apply glow to.");
        }
    }

    /// <summary>
    /// Call this method when an enemy is defeated to stop its flight coroutine
    /// and remove it from tracking lists.
    /// </summary>
    /// <param name="deadEnemy">The GameObject of the defeated enemy.</param>
    public void NotifyEnemyDeath(GameObject deadEnemy)
    {
        Debug.Log($"Spawner received death notification for {deadEnemy.name}. Stopping flight and cleaning up.");

        // Ensure glow is off before destroying (good practice for pooled objects too)
        SetEnemyGlow(deadEnemy, false); // Turn off glow on death

        // Stop the flight coroutine if it exists
        if (enemyFlightCoroutines.ContainsKey(deadEnemy))
        {
            StopCoroutine(enemyFlightCoroutines[deadEnemy]);
            enemyFlightCoroutines.Remove(deadEnemy);
        }

        // Remove from spawned enemies list
        spawnedEnemies.Remove(deadEnemy);

        // Remove from ready-to-attack list
        funnelCompletedEnemies.Remove(deadEnemy);

        // Remove from current targets dictionary
        enemyCurrentTargets.Remove(deadEnemy);

        // Check if all enemies are defeated and destroy the spawner if so.
        if (spawnedEnemies.Count == 0)
        {
            Debug.Log("All enemies defeated! Starting spawner destruction countdown.");
            StartCoroutine(DestroySpawnerAfterDelay());
        }
    }

    /// <summary>
    /// Coroutine to destroy the spawner GameObject after a specified delay.
    /// </summary>
    IEnumerator DestroySpawnerAfterDelay()
    {
        yield return new WaitForSeconds(spawnerDestroyDelay);
        Debug.Log("Destroying spawner.");
        Destroy(this.gameObject); // Destroy the GameObject this script is attached to
    }


    /// <summary>
    /// Draws Gizmos in the editor for visualization of flight area and funnel points.
    /// Gizmos for individual enemy flight paths and attack directions are commented out.
    /// </summary>
    void OnDrawGizmos()
    {
        // Draw the main flight area collider bounds.
        if (flightAreaCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(flightAreaCollider.bounds.center, flightAreaCollider.bounds.size);

            // Draw the restricted flight height area.
            Bounds bounds = flightAreaCollider.bounds;
            float minFlightY = bounds.min.y + (bounds.size.y * minFlightHeightRatio);
            float maxFlightY = bounds.min.y + (bounds.size.y * maxFlightHeightRatio);
            Vector3 heightCenter = new Vector3(bounds.center.x, (minFlightY + maxFlightY) / 2f, bounds.center.z);
            Vector3 heightSize = new Vector3(bounds.size.x, maxFlightY - minFlightY, bounds.size.z);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(heightCenter, heightSize);
        }

        // Draw the funnel spawn and target points.
        if (funnelSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(funnelSpawnPoint.position, 0.5f); // Draw a sphere for the spawn point
            if (funnelTargetPoint != null)
            {
                Gizmos.DrawLine(funnelSpawnPoint.position, funnelTargetPoint.position); // Draw a line between them
            }
        }
        if (funnelTargetPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(funnelTargetPoint.position, 0.5f); // Draw a sphere for the target point
        }


        // --- Commented out Gizmos for individual enemy behavior ---
        /*
        foreach (GameObject enemy in spawnedEnemies) 
        {
            if (enemy == null || player == null) continue;
            
            // If an enemy's flight coroutine is not active, it's either attacking or paused before returning.
            // Gizmo for attacking enemies.
            if (!enemyFlightCoroutines.ContainsKey(enemy))
            {
                Gizmos.color = attackDirectionGizmoColor;
                Gizmos.DrawLine(enemy.transform.position, player.transform.position); // Draw to current player position for gizmo clarity
            }
            // For enemies that are flying around, show their current target
            else if (enemyCurrentTargets.ContainsKey(enemy))
            {
                // Use a different color for enemies that are ready to attack vs. still funneling
                if (funnelCompletedEnemies.Contains(enemy))
                {
                    Gizmos.color = Color.white; // Ready to attack
                }
                else
                {
                    Gizmos.color = Color.grey; // Still funneling
                }
                Gizmos.DrawLine(enemy.transform.position, enemyCurrentTargets[enemy]);
                Gizmos.DrawSphere(enemyCurrentTargets[enemy], 0.2f);
            }
        }
        */
    }
}