using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BossSpawnerAndController : MonoBehaviour
{
    // Event triggered when any enemy dies
    public static event Action OnAnyEnemyDeath;

    // Prefab for the enemy GameObject
    public GameObject enemyPrefab;
    // Collider defining the flight area for enemies
    public BoxCollider flightAreaCollider;

    // Number of enemies to spawn initially
    public int numberOfEnemies = 5;
    // Base speed for enemy flight
    public float flySpeed = 3f;

    // Ratios to define the minimum and maximum flight height within the collider
    [Range(0f, 1f)]
    public float minFlightHeightRatio = 0.1f;
    [Range(0f, 1f)]
    public float maxFlightHeightRatio = 0.9f;

    [Header("Obstacle Avoidance")]
    // Layer mask for obstacles enemies should avoid
    public LayerMask obstacleLayer;
    // Length of the raycast for obstacle detection
    public float avoidanceRayLength = 2f;
    // Radius of the spherecast for obstacle detection
    public float avoidanceSphereRadius = 0.5f;
    // Force applied to avoid obstacles
    public float avoidanceForce = 5f;
    // Speed at which enemies rotate (This variable will now be unused in FlyAround)
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
    // Dictionary to store the current target position for each enemy
    private Dictionary<GameObject, Vector3> enemyCurrentTargets = new Dictionary<GameObject, Vector3>();
    // HashSet to track enemies that have completed the initial funneling phase
    private HashSet<GameObject> funnelCompletedEnemies = new HashSet<GameObject>();

    // Flag to track if all initial enemies have been instantiated
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

        // Start the sequential spawning process
        StartCoroutine(SpawnEnemiesSequentially());
    }

    void Update()
    {
        // Backup mechanism to destroy the spawner if it gets stuck.
        // This triggers if all enemies were initially instantiated, and
        // the spawner's transform only has its two funnel points as children remaining.
        // 'numberOfEnemies > 0' ensures this backup doesn't trigger immediately if no enemies are meant to spawn.
        // 'this.enabled' check prevents multiple destruction attempts if already triggered.
        //
        // NOTE: This backup system ensures the spawner is cleaned up even if the primary
        // destruction condition (all enemies defeated) somehow fails.
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

            // Set initial target for funneling
            enemyCurrentTargets[newEnemy] = funnelTargetPoint.position;

            // Start the flight coroutine for the new enemy
            Coroutine flightCoroutine = StartCoroutine(FlyAround(newEnemy));
            enemyFlightCoroutines.Add(newEnemy, flightCoroutine);

            yield return new WaitForSeconds(spawnDelay); // Wait before spawning next enemy
        }

        // Mark that all initial enemies have been instantiated.
        allEnemiesInstantiated = true;

        // After all enemies are instantiated, wait for them to complete their initial funneling
        while (funnelCompletedEnemies.Count < numberOfEnemies)
        {
            yield return null; // Wait for the next frame
        }

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
    /// and obstacle avoidance. The model will not rotate based on movement.
    /// </summary>
    /// <param name="enemy">The enemy GameObject to control.</param>
    IEnumerator FlyAround(GameObject enemy)
    {
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
                    UnityEngine.Debug.Log($"Enemy {enemy.name} completed funneling.");
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
                if (UnityEngine.Mathf.Abs(dotProductUp) > 0.8f) // If hitting a ceiling or floor
                {
                    avoidanceDirection = UnityEngine.Vector3.Cross(enemy.transform.forward, enemy.transform.up).normalized * (UnityEngine.Mathf.Sign(dotProductUp) * -1);
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

            // --- THE FIX: REMOVED ROTATION LOGIC ---
            // The enemy's rotation will remain as it was when instantiated.
            // If you want it to explicitly stay at the prefab's initial rotation, you could
            // add: enemy.transform.rotation = initialRotation;
            // However, since it's already instantiated with a specific rotation,
            // simply not changing it achieves the desired "no rotation" effect.

            // Move the enemy
            enemy.transform.position = UnityEngine.Vector3.MoveTowards(enemy.transform.position, enemy.transform.position + finalMoveDirection, flySpeed * Time.deltaTime);

            yield return null; // Wait for the next frame
        }
    }

    /// <summary>
    /// Called by an enemy when it dies. Cleans up references and checks for spawner destruction.
    /// The spawner will now destroy itself when all initially spawned enemies are defeated.
    /// </summary>
    /// <param name="deadEnemy">The GameObject of the enemy that died.</param>
    public void NotifyEnemyDeath(GameObject deadEnemy)
    {
        UnityEngine.Debug.Log($"Spawner received death notification for {deadEnemy.name}. Stopping flight and cleaning up.");

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
        if (spawnedEnemies.Count == 0 && allEnemiesInstantiated) // Ensure all enemies were meant to be spawned
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
        Destroy(transform.parent.gameObject);
    }

    /// <summary>
    /// Draws Gizmos in the editor for visualization of flight area, spawn points,
    /// and enemy targets.
    /// </summary>
    void OnDrawGizmos()
    {
        // Draw flight area collider bounds
        if (flightAreaCollider != null)
        {
            Gizmos.color = UnityEngine.Color.green;
            Gizmos.DrawWireCube(flightAreaCollider.bounds.center, flightAreaCollider.bounds.size);

            // Draw flight height limits
            Bounds bounds = flightAreaCollider.bounds;
            float minFlightY = bounds.min.y + (bounds.size.y * minFlightHeightRatio);
            float maxFlightY = bounds.min.y + (bounds.size.y * maxFlightHeightRatio);
            UnityEngine.Vector3 heightCenter = new UnityEngine.Vector3(bounds.center.x, (minFlightY + maxFlightY) / 2f, bounds.center.z);
            UnityEngine.Vector3 heightSize = new UnityEngine.Vector3(bounds.size.x, maxFlightY - minFlightY, bounds.size.z);
            Gizmos.color = UnityEngine.Color.cyan;
            Gizmos.DrawWireCube(heightCenter, heightSize);
        }

        // Draw funnel spawn and target points
        if (funnelSpawnPoint != null)
        {
            Gizmos.color = UnityEngine.Color.yellow;
            Gizmos.DrawSphere(funnelSpawnPoint.position, 0.5f);
            if (funnelTargetPoint != null)
            {
                Gizmos.DrawLine(funnelSpawnPoint.position, funnelTargetPoint.position);
            }
        }
        if (funnelTargetPoint != null)
        {
            Gizmos.color = UnityEngine.Color.blue;
            Gizmos.DrawSphere(funnelTargetPoint.position, 0.5f);
        }

        // Draw lines to current targets for each enemy
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy == null) continue;

            if (enemyCurrentTargets.ContainsKey(enemy))
            {
                // Differentiate color based on funnel completion
                if (funnelCompletedEnemies.Contains(enemy))
                {
                    Gizmos.color = UnityEngine.Color.white;
                }
                else
                {
                    Gizmos.color = UnityEngine.Color.grey;
                }
                Gizmos.DrawLine(enemy.transform.position, enemyCurrentTargets[enemy]);
                Gizmos.DrawSphere(enemyCurrentTargets[enemy], 0.2f);
            }
        }
    }
}