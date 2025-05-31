using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnerAndController : MonoBehaviour
{
    public static event Action OnAnyEnemyDeath;

    public GameObject enemyPrefab;
    public BoxCollider flightAreaCollider;

    public int numberOfEnemies = 5;
    public float flySpeed = 3f;

    [Range(0f, 1f)]
    public float minFlightHeightRatio = 0.1f;
    [Range(0f, 1f)]
    public float maxFlightHeightRatio = 0.9f;

    public float attackSpeedMultiplier = 2f;
    public float attackCooldown = 5f;
    public GameObject player;
    public float attackReturnDelay = 1f;

    [Header("Attack Behavior")]
    public float attackRotationOffset = 90f;
    public float attackSuccessDistance = 3f;
    public int damageAmount = 10; // Damage dealt when Flier hits the player

    [Header("Glow Settings")]
    public Color attackGlowColor = Color.red;
    public float glowIntensity = 2.0f;

    [Header("Gizmo Settings")]
    public Color attackDirectionGizmoColor = Color.magenta;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public float avoidanceRayLength = 2f;
    public float avoidanceSphereRadius = 0.5f;
    public float avoidanceForce = 5f;
    public float rotationSpeed = 5f;

    [Header("Funnel Spawn Settings")]
    public Transform funnelSpawnPoint;
    public Transform funnelTargetPoint;
    public float spawnDelay = 0.5f;

    [Header("Spawner Destruction")]
    public float spawnerDestroyDelay = 2f;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Dictionary<GameObject, Coroutine> enemyFlightCoroutines = new Dictionary<GameObject, Coroutine>();
    private float lastAttackTime;
    private Dictionary<GameObject, Vector3> enemyCurrentTargets = new Dictionary<GameObject, Vector3>();
    private HashSet<GameObject> funnelCompletedEnemies = new HashSet<GameObject>();

    void Start()
    {
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

        StartCoroutine(SpawnEnemiesSequentially());
        lastAttackTime = -attackCooldown;
    }

    void Update()
    {
        if (Time.time - lastAttackTime > attackCooldown && funnelCompletedEnemies.Count > 0)
        {
            StartCoroutine(AttackPlayer());
            lastAttackTime = Time.time;
        }
    }

    IEnumerator SpawnEnemiesSequentially()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            GameObject newEnemy = Instantiate(enemyPrefab, funnelSpawnPoint.position, Quaternion.identity);
            newEnemy.transform.parent = this.transform;

            spawnedEnemies.Add(newEnemy);

            SetEnemyGlow(newEnemy, false);

            enemyCurrentTargets[newEnemy] = funnelTargetPoint.position;

            Coroutine flightCoroutine = StartCoroutine(FlyAround(newEnemy));
            enemyFlightCoroutines.Add(newEnemy, flightCoroutine);

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    Vector3 GetRandomPositionInColliderBounds()
    {
        Bounds bounds = flightAreaCollider.bounds;

        float minFlightY = bounds.min.y + (bounds.size.y * minFlightHeightRatio);
        float maxFlightY = bounds.min.y + (bounds.size.y * maxFlightHeightRatio);

        float randomX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
        float randomY = UnityEngine.Random.Range(minFlightY, maxFlightY);
        float randomZ = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }

    IEnumerator FlyAround(GameObject enemy)
    {
        SetEnemyGlow(enemy, false);

        Vector3 targetPosition = enemyCurrentTargets.ContainsKey(enemy) ? enemyCurrentTargets[enemy] : GetRandomPositionInColliderBounds();

        while (true)
        {
            if (enemy == null || !enemy.activeInHierarchy)
            {
                yield break;
            }

            if (Vector3.Distance(enemy.transform.position, targetPosition) < 0.5f)
            {
                if (Vector3.Distance(targetPosition, funnelTargetPoint.position) < 0.1f && !funnelCompletedEnemies.Contains(enemy))
                {
                    funnelCompletedEnemies.Add(enemy);
                    Debug.Log($"Enemy {enemy.name} completed funneling and is now ready to attack.");
                }

                targetPosition = GetRandomPositionInColliderBounds();
                enemyCurrentTargets[enemy] = targetPosition;
            }

            Vector3 directionToTarget = (targetPosition - enemy.transform.position).normalized;
            Vector3 finalMoveDirection = directionToTarget;

            RaycastHit hit;
            if (Physics.SphereCast(enemy.transform.position, avoidanceSphereRadius, enemy.transform.forward, out hit, avoidanceRayLength, obstacleLayer))
            {
                Vector3 hitNormal = hit.normal;
                Vector3 avoidanceDirection = Vector3.zero;

                float dotProductUp = Vector3.Dot(enemy.transform.up, hitNormal);
                if (Mathf.Abs(dotProductUp) > 0.8f)
                {
                    avoidanceDirection = Vector3.Cross(enemy.transform.forward, enemy.transform.up).normalized * (Mathf.Sign(dotProductUp) * -1);
                }
                else
                {
                    avoidanceDirection = Vector3.Cross(enemy.transform.up, hitNormal).normalized;
                    if (enemy.transform.position.y < flightAreaCollider.bounds.center.y)
                        avoidanceDirection += Vector3.up * 0.5f;
                    else
                        avoidanceDirection += Vector3.down * 0.5f;
                }

                finalMoveDirection = Vector3.Lerp(finalMoveDirection, avoidanceDirection.normalized, avoidanceForce * Time.deltaTime).normalized;
                if (finalMoveDirection == Vector3.zero) finalMoveDirection = directionToTarget;
            }

            if (finalMoveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(finalMoveDirection);
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, enemy.transform.position + finalMoveDirection, flySpeed * Time.deltaTime);

            yield return null;
        }
    }

    IEnumerator AttackPlayer()
    {
        if (funnelCompletedEnemies.Count == 0) yield break;
        if (player == null)
        {
            Debug.LogWarning("Player GameObject is null. Cannot initiate attack.");
            yield break;
        }

        List<GameObject> eligibleAttackers = new List<GameObject>(funnelCompletedEnemies);
        if (eligibleAttackers.Count == 0) yield break;

        GameObject attackingEnemy = eligibleAttackers[UnityEngine.Random.Range(0, eligibleAttackers.Count)];

        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy) yield break;

        funnelCompletedEnemies.Remove(attackingEnemy);

        if (enemyFlightCoroutines.ContainsKey(attackingEnemy) && enemyFlightCoroutines[attackingEnemy] != null)
        {
            StopCoroutine(enemyFlightCoroutines[attackingEnemy]);
            enemyFlightCoroutines.Remove(attackingEnemy);
        }

        SetEnemyGlow(attackingEnemy, true);

        float originalFlySpeed = flySpeed;
        float currentEnemyAttackSpeed = originalFlySpeed;

        while (attackingEnemy != null && attackingEnemy.activeInHierarchy && player != null && Vector3.Distance(attackingEnemy.transform.position, player.transform.position) > attackSuccessDistance)
        {
            currentEnemyAttackSpeed = Mathf.Lerp(currentEnemyAttackSpeed, originalFlySpeed * attackSpeedMultiplier, Time.deltaTime * 5f);

            Vector3 moveDirection = (player.transform.position - attackingEnemy.transform.position).normalized;

            Vector3 directionToPlayerRaw = (player.transform.position - attackingEnemy.transform.position).normalized;
            if (directionToPlayerRaw != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayerRaw);
                targetRotation *= Quaternion.Euler(0, attackRotationOffset, 0);
                attackingEnemy.transform.rotation = Quaternion.Slerp(attackingEnemy.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * attackSpeedMultiplier);
            }

            attackingEnemy.transform.position = Vector3.MoveTowards(attackingEnemy.transform.position, attackingEnemy.transform.position + moveDirection, currentEnemyAttackSpeed * Time.deltaTime);

            enemyCurrentTargets[attackingEnemy] = player.transform.position;

            yield return null;
        }

        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy)
        {
            SetEnemyGlow(attackingEnemy, false);
            yield break;
        }

        // Apply damage when the Flier reaches the player
        Debug.Log($"Flier {attackingEnemy.name} reached attack success distance and hit the player.");
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
            Debug.Log($"Flier {attackingEnemy.name} dealt {damageAmount} damage to the player.");
        }
        else
        {
            Debug.LogWarning($"Flier {attackingEnemy.name} could not find PlayerHealth on the player.");
        }

        enemyCurrentTargets[attackingEnemy] = GetRandomPositionInColliderBounds();

        yield return new WaitForSeconds(attackReturnDelay);

        if (attackingEnemy == null || !attackingEnemy.activeInHierarchy)
        {
            SetEnemyGlow(attackingEnemy, false);
            yield break;
        }

        Coroutine newFlightCoroutine = StartCoroutine(FlyAround(attackingEnemy));
        enemyFlightCoroutines.Add(attackingEnemy, newFlightCoroutine);

        funnelCompletedEnemies.Add(attackingEnemy);
    }

    void SetEnemyGlow(GameObject enemy, bool enableGlow)
    {
        if (enemy == null) return;

        Renderer enemyRenderer = enemy.GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            Material enemyMaterial = enemyRenderer.material;

            if (enableGlow)
            {
                enemyMaterial.EnableKeyword("_EMISSION");
                enemyMaterial.SetColor("_EmissionColor", attackGlowColor * glowIntensity);
            }
            else
            {
                enemyMaterial.DisableKeyword("_EMISSION");
                enemyMaterial.SetColor("_EmissionColor", Color.black);
            }
        }
        else
        {
            Debug.LogWarning($"Enemy {enemy.name} has no Renderer component to apply glow to.");
        }
    }

    public void NotifyEnemyDeath(GameObject deadEnemy)
    {
        Debug.Log($"Spawner received death notification for {deadEnemy.name}. Stopping flight and cleaning up.");

        SetEnemyGlow(deadEnemy, false);

        if (enemyFlightCoroutines.ContainsKey(deadEnemy))
        {
            StopCoroutine(enemyFlightCoroutines[deadEnemy]);
            enemyFlightCoroutines.Remove(deadEnemy);
        }

        spawnedEnemies.Remove(deadEnemy);
        funnelCompletedEnemies.Remove(deadEnemy);
        enemyCurrentTargets.Remove(deadEnemy);

        OnAnyEnemyDeath?.Invoke();

        if (spawnedEnemies.Count == 0)
        {
            Debug.Log("All enemies defeated! Starting spawner destruction countdown.");
            StartCoroutine(DestroySpawnerAfterDelay());
        }
    }

    IEnumerator DestroySpawnerAfterDelay()
    {
        yield return new WaitForSeconds(spawnerDestroyDelay);
        Debug.Log("Destroying spawner.");
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (flightAreaCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(flightAreaCollider.bounds.center, flightAreaCollider.bounds.size);

            Bounds bounds = flightAreaCollider.bounds;
            float minFlightY = bounds.min.y + (bounds.size.y * minFlightHeightRatio);
            float maxFlightY = bounds.min.y + (bounds.size.y * maxFlightHeightRatio);
            Vector3 heightCenter = new Vector3(bounds.center.x, (minFlightY + maxFlightY) / 2f, bounds.center.z);
            Vector3 heightSize = new Vector3(bounds.size.x, maxFlightY - minFlightY, bounds.size.z);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(heightCenter, heightSize);
        }

        if (funnelSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(funnelSpawnPoint.position, 0.5f);
            if (funnelTargetPoint != null)
            {
                Gizmos.DrawLine(funnelSpawnPoint.position, funnelTargetPoint.position);
            }
        }
        if (funnelTargetPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(funnelTargetPoint.position, 0.5f);
        }

        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy == null || player == null) continue;

            if (!enemyFlightCoroutines.ContainsKey(enemy))
            {
                Gizmos.color = attackDirectionGizmoColor;
                Gizmos.DrawLine(enemy.transform.position, player.transform.position);
            }
            else if (enemyCurrentTargets.ContainsKey(enemy))
            {
                if (funnelCompletedEnemies.Contains(enemy))
                {
                    Gizmos.color = Color.white;
                }
                else
                {
                    Gizmos.color = Color.grey;
                }
                Gizmos.DrawLine(enemy.transform.position, enemyCurrentTargets[enemy]);
                Gizmos.DrawSphere(enemyCurrentTargets[enemy], 0.2f);
            }
        }
    }
}
