using UnityEngine;

public class ShootingEnemy : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform playerTransform; // Assign your Player GameObject's Transform here (Main Camera)
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
        currentHitChance = 0f; // Start with minimum hit chance

        // Get the player's main collider component at the start of the game
        // Using Camera.main.transform as the player target for consistency with previous discussions.
        if (UnityEngine.Camera.main != null)
        {
            playerTransform = UnityEngine.Camera.main.transform;
            UnityEngine.Debug.Log($"ShootingEnemy: Player Transform found as '{playerTransform.name}' (Main Camera).", this);

            // Try to find the player's main collider on the camera or its "Sphere" child
            InitializePlayerCollider();
        }
        else
        {
            UnityEngine.Debug.LogError("ShootingEnemy: No 'Main Camera' found! Ensure your main camera is tagged 'MainCamera' in the Inspector. This enemy cannot function without a player target.", this);
            enabled = false; // Disable this script if no player is found
            return;
        }

        // Calculate dynamic miss offsets once playerMainCollider is set
        CalculateDynamicMissOffsets();
    }

    // Encapsulates the logic for finding the player's primary collider
    private void InitializePlayerCollider()
    {
        if (playerTransform == null)
        {
            UnityEngine.Debug.LogError("ShootingEnemy: Player Transform is null during collider initialization. This should not happen if Start() executed correctly.", this);
            return;
        }

        // Option 1: Try to find a specific child GameObject named "Sphere"
        Transform playerHitboxChild = playerTransform.Find("Sphere");

        if (playerHitboxChild != null)
        {
            playerMainCollider = playerHitboxChild.GetComponent<UnityEngine.Collider>();
            if (playerMainCollider == null)
            {
                UnityEngine.Debug.LogWarning($"ShootingEnemy: Found 'Sphere' child on '{playerTransform.name}', but it does not have a Collider component! Ensure the child named 'Sphere' has a collider. Falling back to playerTransform's collider.", playerHitboxChild);
            }
            else
            {
                UnityEngine.Debug.Log($"ShootingEnemy: Player Collider found on 'Sphere' child of '{playerTransform.name}'.", playerHitboxChild);
            }
        }

        // Option 2: Fallback to getting the collider directly from the playerTransform (Camera.main) itself
        if (playerMainCollider == null)
        {
            playerMainCollider = playerTransform.GetComponent<UnityEngine.Collider>();
            if (playerMainCollider == null)
            {
                UnityEngine.Debug.LogWarning($"ShootingEnemy: No child named 'Sphere' found on '{playerTransform.name}', AND '{playerTransform.name}' itself does not have a Collider component. Precise aiming may be difficult. Ensure the Main Camera or its 'Sphere' child has a collider.", playerTransform);
            }
            else
            {
                UnityEngine.Debug.Log($"ShootingEnemy: Player Collider found directly on '{playerTransform.name}'.", playerTransform);
            }
        }
    }

    // Encapsulates dynamic miss offset calculation
    private void CalculateDynamicMissOffsets()
    {
        if (playerMainCollider != null && bulletPrefab != null)
        {
            // Get player's horizontal "radius" (half of its widest XZ extent)
            float playerHorizontalRadius = Mathf.Max(playerMainCollider.bounds.extents.x, playerMainCollider.bounds.extents.z);

            // Get bullet's effective radius from its collider
            float bulletRadius = 0f;
            Collider bulletCollider = bulletPrefab.GetComponent<Collider>();
            if (bulletCollider != null)
            {
                bulletRadius = Mathf.Max(bulletCollider.bounds.extents.x, bulletCollider.bounds.extents.z);
            }

            // Calculate minimum miss offset: player horizontal radius + bullet radius + buffer
            _calculatedMissOffsetMin = playerHorizontalRadius + bulletRadius + missBuffer;
            _calculatedMissOffsetMin = Mathf.Max(_calculatedMissOffsetMin, 0.1f); // Ensure a small practical minimum

            // Calculate maximum miss offset: min offset * multiplier
            _calculatedMissOffsetMax = _calculatedMissOffsetMin * missRangeMultiplier;

            UnityEngine.Debug.Log($"ShootingEnemy: Calculated miss offsets: Min = {_calculatedMissOffsetMin:F2}, Max = {_calculatedMissOffsetMax:F2}. (Player R: {playerHorizontalRadius:F2}, Bullet R: {bulletRadius:F2})", this);
        }
        else
        {
            UnityEngine.Debug.LogWarning("ShootingEnemy: Cannot calculate dynamic miss offsets. Ensure Main Camera has a Collider (or a 'Sphere' child with one) and BulletPrefab has a Collider. Using default miss offsets.", this);
            // Fallback to sensible defaults if calculation fails
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
        else
        {
            // Debugging feedback if a critical reference goes missing during runtime
            if (playerTransform == null)
            {
                UnityEngine.Debug.LogWarning("ShootingEnemy: Player Transform (Main Camera) is missing! Cannot operate. Has the Main Camera been destroyed or untagged?", this);
            }
            if (shootingPoint == null)
            {
                UnityEngine.Debug.LogWarning("ShootingEnemy: Shooting Point Transform is missing! Cannot operate.", this);
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

        // Use shootingPoint.position as the source of the line of sight check
        UnityEngine.Vector3 rayOrigin = shootingPoint.position;

        // Target for raycast is the center of the player's main collider for accuracy
        UnityEngine.Vector3 rayTarget = playerMainCollider.bounds.center;

        UnityEngine.Vector3 rayDirection = (rayTarget - rayOrigin).normalized;
        float rayDistance = UnityEngine.Vector3.Distance(rayOrigin, rayTarget);

        UnityEngine.RaycastHit hit;
        // Perform a raycast, ignoring hits with the enemy's own colliders, and only considering obstacles
        // Add a small offset to rayDistance to ensure it doesn't just hit the player's *surface* and assume no obstacle
        if (UnityEngine.Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance + 0.1f, lineOfSightObstacleLayer))
        {
            // If the ray hits something, check if it's the player's collider (or a child of the player's transform)
            return hit.collider.transform == playerMainCollider.transform || hit.collider.transform.IsChildOf(playerTransform);
        }
        // If the ray doesn't hit anything in the obstacle layer, line of sight is clear
        return true;
    }

    // --- Using your provided RotateTowardsPlayer method ---
    void RotateTowardsPlayer()
    {
        // 1. Calculate the horizontal direction from the shootingPoint to the player.
        // This ensures the enemy's body rotates to horizontally align the muzzle.
        UnityEngine.Vector3 directionToPlayerHorizontal = playerTransform.position - shootingPoint.position;
        directionToPlayerHorizontal.y = 0; // Ignore vertical difference for body rotation
        directionToPlayerHorizontal.Normalize();

        if (directionToPlayerHorizontal == UnityEngine.Vector3.zero) return; // Prevent errors if target is at the same horizontal position

        // 2. Calculate the desired world rotation for the shootingPoint if it were pointing directly at the player horizontally.
        UnityEngine.Quaternion desiredShootingPointWorldRot = UnityEngine.Quaternion.LookRotation(directionToPlayerHorizontal);

        // 3. Find the current local rotation of the shootingPoint relative to its parent (the enemy's body).
        // This tells us how the shootingPoint is oriented *within* the enemy's hierarchy.
        UnityEngine.Quaternion currentShootingPointToParentRot = UnityEngine.Quaternion.Inverse(transform.rotation) * shootingPoint.rotation;

        // 4. Calculate the target rotation for the enemy's main body (transform.rotation).
        // This is done by taking the desired world rotation for the shootingPoint
        // and "subtracting" the shootingPoint's local offset rotation.
        UnityEngine.Quaternion targetEnemyRotation = desiredShootingPointWorldRot * UnityEngine.Quaternion.Inverse(currentShootingPointToParentRot);

        // 5. Constrain the enemy's body rotation to only the Y-axis (horizontal).
        // This removes any pitch (X) or roll (Z) components from the enemy's main body.
        targetEnemyRotation.x = 0;
        targetEnemyRotation.z = 0;
        targetEnemyRotation.Normalize(); // Re-normalize after setting X/Z to 0 for a valid quaternion

        // Smoothly interpolate the enemy's main body rotation towards the target.
        transform.rotation = UnityEngine.Quaternion.Slerp(transform.rotation, targetEnemyRotation, UnityEngine.Time.deltaTime * rotationSpeed);

        // --- DEBUG DRAW RAYS ---
        // Visualize the enemy's current forward (RED) - this shows the main body's horizontal orientation
        UnityEngine.Debug.DrawRay(transform.position, transform.forward * 5f, UnityEngine.Color.red, 0.1f);

        // Determine the actual 3D target point for the bullet's aim.
        // This is the player's full 3D position (collider center or transform position).
        UnityEngine.Vector3 actualAimTarget = (playerMainCollider != null) ? playerMainCollider.bounds.center : playerTransform.position;

        // Calculate the actual 3D shooting direction from the muzzle to the player's 3D target.
        // This will be the direction the bullet actually travels.
        UnityEngine.Vector3 actualShootDirection = (actualAimTarget - shootingPoint.position).normalized;

        // Visualize the calculated actual shooting direction from the muzzle (BLUE).
        // This ray shows the precise line from the gun to the player, including vertical aiming.
        UnityEngine.Debug.DrawRay(shootingPoint.position, actualShootDirection * 5f, UnityEngine.Color.blue, 0.1f);

        // Visualize the player's 3D target point (GREEN)
        UnityEngine.Debug.DrawRay(actualAimTarget, UnityEngine.Vector3.up * 1f, UnityEngine.Color.green, 0.1f);
        // --- END DEBUG DRAW RAYS ---
    }


    void ShootAtPlayerWithChance()
    {
        UnityEngine.Vector3 targetShootPosition;
        float roll = UnityEngine.Random.value * 100f;
        bool isHit = (roll <= currentHitChance);

        // Define the base target for aiming. This is where the bullet is *intended* to go.
        UnityEngine.Vector3 baseAimTarget;
        if (playerMainCollider != null)
        {
            baseAimTarget = playerMainCollider.bounds.center;
        }
        else
        {
            baseAimTarget = playerTransform.position;
        }

        if (isHit)
        {
            targetShootPosition = baseAimTarget;
        }
        else // It's a miss
        {
            // Use the dynamically calculated min/max offsets
            float missDistance = UnityEngine.Random.Range(_calculatedMissOffsetMin, _calculatedMissOffsetMax);
            float missAngle = UnityEngine.Random.Range(0f, 360f); // Full circle random miss around the player's horizontal plane

            float offsetX = missDistance * UnityEngine.Mathf.Cos(missAngle * UnityEngine.Mathf.Deg2Rad);
            float offsetZ = missDistance * UnityEngine.Mathf.Sin(missAngle * UnityEngine.Mathf.Deg2Rad);

            // Apply the offset to the base aim target's horizontal plane,
            // then combine with its original Y for the vertical aiming component.
            targetShootPosition = new UnityEngine.Vector3(
                baseAimTarget.x + offsetX,
                baseAimTarget.y, // Maintain original vertical target
                baseAimTarget.z + offsetZ
            );
        }

        // Calculate the normalized direction from the shooting point to the final target position
        // This direction will correctly include vertical aiming as needed.
        UnityEngine.Vector3 shootDirection = (targetShootPosition - shootingPoint.position).normalized;

        if (bulletPrefab != null)
        {
            // *** IMPORTANT FIX: Set the bullet's initial rotation to face the shootDirection ***
            GameObject bullet = UnityEngine.Object.Instantiate(bulletPrefab, shootingPoint.position, UnityEngine.Quaternion.LookRotation(shootDirection));

            BulletBehavior bulletBehavior = bullet.GetComponent<BulletBehavior>();
            BulletMover bulletMover = bullet.GetComponent<BulletMover>();

            if (bulletMover != null)
            {
                bulletMover.SetDirectionAndSpeed(shootDirection, bulletSpeed, playerTransform, isHit);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Bullet prefab '{bulletPrefab.name}' is missing a 'BulletMover' component.", bulletPrefab);
            }

            if (bulletBehavior != null && playerTransform != null)
            {
                bulletBehavior.SetTarget(playerTransform, impactGlowDuration, impactGlowColor);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Bullet prefab '{bulletPrefab.name}' is missing a 'BulletBehavior' component.", bulletPrefab);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Bullet Prefab is not assigned on " + gameObject.name);
        }
    }
}
