using UnityEngine;

public class ShootingEnemy : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform playerTransform; // Assign your Player GameObject's Transform here
    public Transform shootingPoint; // Assign the empty GameObject at the gun barrel/muzzle
    public float fireRate = 1f; // Bullets per second
    public float bulletSpeed = 5f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f; // Speed at which the enemy rotates to face the player

    [Header("Dynamic Hit Chance")]
    [Tooltip("Points of hit chance gained per second while seeing player.")]
    public float hitChanceGainRate = 10f;
    [Tooltip("Points of hit chance lost per second while not seeing player.")]
    public float hitChanceLossRate = 20f;
    [Range(0f, 100f)]
    [Tooltip("Maximum hit chance percentage the enemy can achieve.")]
    public float maxHitChance = 90f;
    [Tooltip("Layers that should block the enemy's line of sight (e.g., Environment, Walls).")]
    public LayerMask lineOfSightObstacleLayer;

    // Removed: public float missOffsetMin; public float missOffsetMax;

    [Header("Dynamic Miss Offset Calculation")]
    [Tooltip("Extra padding added to player's size + bullet's size to ensure misses don't hit.")]
    public float missBuffer = 0.5f; // Extra distance to guarantee a clear miss
    [Tooltip("Multiplier used to determine the maximum miss distance relative to the minimum.")]
    public float missRangeMultiplier = 3.0f; // Max miss = min miss * this multiplier

    [SerializeField]
    [Tooltip("The actual fluctuating hit chance percentage (0 to maxHitChance).")]
    private float currentHitChance = 0f;

    [SerializeField]
    [Tooltip("Dynamically calculated minimum distance from player's center for a 'miss' shot.")]
    private float _calculatedMissOffsetMin; // Stores the calculated minimum miss distance

    [SerializeField]
    [Tooltip("Dynamically calculated maximum distance from player's center for a 'miss' shot.")]
    private float _calculatedMissOffsetMax; // Stores the calculated maximum miss distance


    private float nextFireTime;
    private UnityEngine.Collider playerMainCollider;

    [Header("Impact Settings")]
    public float impactGlowDuration = 0.2f;
    public Color impactGlowColor = UnityEngine.Color.red;

    void Start()
    {
        // Get the player's main collider component at the start of the game
        if (playerTransform != null)
        {
            playerMainCollider = playerTransform.GetComponent<UnityEngine.Collider>();
            if (playerMainCollider == null)
            {
                UnityEngine.Debug.LogError("ShootingEnemy: Player Transform does not have a Collider component! Please ensure your player's root GameObject has a Collider attached.", playerTransform);
            }
        }
        currentHitChance = 0f; // Start with minimum hit chance

        // --- DYNAMIC MISS OFFSET CALCULATION ---
        if (playerMainCollider != null && bulletPrefab != null)
        {
            // Get player's horizontal "radius" (half of its widest XZ extent)
            // This is a robust way to get the player's effective width for horizontal misses.
            float playerHorizontalRadius = Mathf.Max(playerMainCollider.bounds.extents.x, playerMainCollider.bounds.extents.z);

            // Get bullet's effective radius from its collider
            float bulletRadius = 0f;
            Collider bulletCollider = bulletPrefab.GetComponent<Collider>();
            if (bulletCollider != null)
            {
                // For a typical bullet, the maximum extent in XZ is a reasonable "radius"
                bulletRadius = Mathf.Max(bulletCollider.bounds.extents.x, bulletCollider.bounds.extents.z);
                // If you know your bullet is a sphere or capsule, you could use:
                // SphereCollider sc = bulletCollider as SphereCollider; if (sc != null) bulletRadius = sc.radius;
                // CapsuleCollider cc = bulletCollider as CapsuleCollider; if (cc != null) bulletRadius = cc.radius;
            }

            // Calculate minimum miss offset: player horizontal radius + bullet radius + buffer
            _calculatedMissOffsetMin = playerHorizontalRadius + bulletRadius + missBuffer;
            // Ensure min offset is at least a very small positive number to prevent issues with tiny colliders
            _calculatedMissOffsetMin = Mathf.Max(_calculatedMissOffsetMin, 0.1f); // 0.1f as a practical minimum

            // Calculate maximum miss offset: min offset * multiplier
            _calculatedMissOffsetMax = _calculatedMissOffsetMin * missRangeMultiplier;

            UnityEngine.Debug.Log($"Enemy '{gameObject.name}' calculated miss offsets: Min = {_calculatedMissOffsetMin:F2}, Max = {_calculatedMissOffsetMax:F2}. (Player R: {playerHorizontalRadius:F2}, Bullet R: {bulletRadius:F2})");
        }
        else
        {
            UnityEngine.Debug.LogWarning("ShootingEnemy: Cannot calculate dynamic miss offsets. Ensure PlayerTransform has a Collider and BulletPrefab has a Collider.", this);
            // Fallback to sensible defaults if calculation fails (though it should be caught by errors above)
            _calculatedMissOffsetMin = 1.0f;
            _calculatedMissOffsetMax = 3.0f;
        }
    }

    void Update()
    {
        if (playerTransform != null && shootingPoint != null)
        {
            UpdateHitChance();
            RotateTowardsPlayer();

            if (UnityEngine.Time.time >= nextFireTime)
            {
                ShootAtPlayerWithChance();
                nextFireTime = UnityEngine.Time.time + 1f / fireRate;
            }
        }
    }

    void UpdateHitChance()
    {
        bool canSeePlayer = CheckLineOfSight();

        if (canSeePlayer)
        {
            currentHitChance += hitChanceGainRate * UnityEngine.Time.deltaTime;
        }
        else
        {
            currentHitChance -= hitChanceLossRate * UnityEngine.Time.deltaTime;
        }

        currentHitChance = UnityEngine.Mathf.Clamp(currentHitChance, 0f, maxHitChance);
    }

    bool CheckLineOfSight()
    {
        if (playerMainCollider == null) return false;

        UnityEngine.Vector3 rayOrigin = transform.position + UnityEngine.Vector3.up * 0.5f;
        UnityEngine.Vector3 rayDirection = (playerMainCollider.bounds.center - rayOrigin).normalized;
        float rayDistance = UnityEngine.Vector3.Distance(rayOrigin, playerMainCollider.bounds.center);

        UnityEngine.RaycastHit hit;
        if (UnityEngine.Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, lineOfSightObstacleLayer))
        {
            return hit.collider.transform == playerTransform;
        }
        return true;
    }

    void RotateTowardsPlayer()
    {
        UnityEngine.Vector3 directionToPlayer = playerTransform.position - shootingPoint.position;
        directionToPlayer.y = 0;
        directionToPlayer.Normalize();

        if (directionToPlayer == UnityEngine.Vector3.zero) return;

        UnityEngine.Quaternion desiredShootingPointWorldRot = UnityEngine.Quaternion.LookRotation(directionToPlayer);
        UnityEngine.Quaternion currentShootingPointToParentRot = UnityEngine.Quaternion.Inverse(transform.rotation) * shootingPoint.rotation;
        UnityEngine.Quaternion targetEnemyRotation = desiredShootingPointWorldRot * UnityEngine.Quaternion.Inverse(currentShootingPointToParentRot);

        targetEnemyRotation.x = 0;
        targetEnemyRotation.z = 0;
        targetEnemyRotation.Normalize();

        transform.rotation = UnityEngine.Quaternion.Slerp(transform.rotation, targetEnemyRotation, UnityEngine.Time.deltaTime * rotationSpeed);
    }

    void ShootAtPlayerWithChance()
    {
        UnityEngine.Vector3 targetShootPosition;
        float roll = UnityEngine.Random.value * 100f;
        bool isHit = (roll <= currentHitChance);

        if (isHit)
        {
            if (playerMainCollider != null)
            {
                targetShootPosition = playerMainCollider.bounds.center;
            }
            else
            {
                UnityEngine.Debug.LogWarning("ShootingEnemy: Player collider not found for precise hit aim, falling back to playerTransform.position.", playerTransform);
                targetShootPosition = playerTransform.position;
            }
        }
        else
        {
            // Use the dynamically calculated min/max offsets
            float missDistance = UnityEngine.Random.Range(_calculatedMissOffsetMin, _calculatedMissOffsetMax);
            float missAngle = UnityEngine.Random.Range(0f, 360f);

            float offsetX = missDistance * UnityEngine.Mathf.Cos(missAngle * UnityEngine.Mathf.Deg2Rad);
            float offsetZ = missDistance * UnityEngine.Mathf.Sin(missAngle * UnityEngine.Mathf.Deg2Rad);

            if (playerMainCollider != null)
            {
                targetShootPosition = playerMainCollider.bounds.center + new UnityEngine.Vector3(offsetX, 0f, offsetZ);
            }
            else
            {
                targetShootPosition = playerTransform.position + new UnityEngine.Vector3(offsetX, 0f, offsetZ);
            }
        }

        UnityEngine.Vector3 shootDirection = (targetShootPosition - shootingPoint.position).normalized;

        if (bulletPrefab != null)
        {
            GameObject bullet = UnityEngine.Object.Instantiate(bulletPrefab, shootingPoint.position, UnityEngine.Quaternion.identity);

            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            BulletMover bulletMover = bullet.GetComponent<BulletMover>();

            if (bulletMover != null)
            {
                bulletMover.SetDirectionAndSpeed(shootDirection, bulletSpeed, playerTransform, isHit);
            }

            if (bulletBehavior != null && playerTransform != null)
            {
                bulletBehavior.SetTarget(playerTransform, impactGlowDuration, impactGlowColor);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Bullet Prefab is not assigned on " + gameObject.name);
        }
    }
}