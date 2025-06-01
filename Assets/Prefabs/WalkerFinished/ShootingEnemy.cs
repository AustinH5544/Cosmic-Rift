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
    [Tooltip("Points of hit chance gained per second while player is exposed (not in cover).")]
    public float hitChanceGainRate = 10f;
    [Tooltip("Points of hit chance lost per second while player is in cover.")]
    public float hitChanceLossRate = 20f;
    [Range(0f, 100f)]
    [Tooltip("Maximum hit chance percentage the enemy can achieve.")]
    public float maxHitChance = 90f;

    [Header("Dynamic Miss Offset Calculation")]
    [Tooltip("Extra padding added to player's size + bullet's size to ensure misses don't hit.")]
    public float missBuffer = 0.5f; // Extra distance to guarantee a clear miss
    [Tooltip("Multiplier used to determine the maximum miss distance relative to the minimum.")]
    public float missRangeMultiplier = 3.0f; // Max miss = min miss * this multiplier

    [Header("Impact Settings")]
    public float impactGlowDuration = 0.2f;
    // Removed impactGlowColor from public variable as it will now be determined by hit/miss
    public Color hitGlowColor = UnityEngine.Color.red; // New: Color for a hit bullet
    public Color missGlowColor = UnityEngine.Color.blue; // New: Color for a miss bullet (optional, can be black/clear)

    // Audio settings for shooting sound
    [Header("Audio Settings")]
    public AudioClip shootSound; // Sound to play when Walker fires
    private AudioSource audioSource; // AudioSource to play the sound

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
    private CoverControllerMain coverController;

    void Start()
    {
        currentHitChance = 0f; // Start with minimum hit chance

        if (UnityEngine.Camera.main != null)
        {
            playerTransform = UnityEngine.Camera.main.transform;
            UnityEngine.Debug.Log($"ShootingEnemy: Player Transform found as '{playerTransform.name}' (Main Camera).", this);
            InitializePlayerCollider();
        }
        else
        {
            UnityEngine.Debug.LogError("ShootingEnemy: No 'Main Camera' found! Ensure your main camera is tagged 'MainCamera' in the Inspector. This enemy cannot function without a player target.", this);
            enabled = false; // Disable this script if no player is found
            return;
        }

        coverController = UnityEngine.Object.FindFirstObjectByType<CoverControllerMain>();
        if (coverController == null)
        {
            UnityEngine.Debug.LogError("ShootingEnemy: CoverControllerMain script not found in the scene! Ensure it's attached to an active GameObject.", this);
            enabled = false; // Disable if no cover controller is found
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        CalculateDynamicMissOffsets();
    }

    private void InitializePlayerCollider()
    {
        if (playerTransform == null)
        {
            UnityEngine.Debug.LogError("ShootingEnemy: Player Transform is null during collider initialization. This should not happen if Start() executed correctly.", this);
            return;
        }

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

    private void CalculateDynamicMissOffsets()
    {
        if (playerMainCollider != null && bulletPrefab != null)
        {
            float playerHorizontalRadius = Mathf.Max(playerMainCollider.bounds.extents.x, playerMainCollider.bounds.extents.z);

            float bulletRadius = 0f;
            Collider bulletCollider = bulletPrefab.GetComponent<Collider>();
            if (bulletCollider != null)
            {
                bulletRadius = Mathf.Max(bulletCollider.bounds.extents.x, bulletCollider.bounds.extents.z);
            }

            _calculatedMissOffsetMin = playerHorizontalRadius + bulletRadius + missBuffer;
            _calculatedMissOffsetMin = Mathf.Max(_calculatedMissOffsetMin, 0.1f);

            _calculatedMissOffsetMax = _calculatedMissOffsetMin * missRangeMultiplier;

            UnityEngine.Debug.Log($"ShootingEnemy: Calculated miss offsets: Min = {_calculatedMissOffsetMin:F2}, Max = {_calculatedMissOffsetMax:F2}. (Player R: {playerHorizontalRadius:F2}, Bullet R: {bulletRadius:F2})", this);
        }
        else
        {
            UnityEngine.Debug.LogWarning("ShootingEnemy: Cannot calculate dynamic miss offsets. Ensure Main Camera has a Collider (or a 'Sphere' child with one) and BulletPrefab has a Collider. Using default miss offsets.", this);
            _calculatedMissOffsetMin = 1.0f;
            _calculatedMissOffsetMax = 3.0f;
        }
    }

    void Update()
    {
        if (audioSource != null)
        {
            audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        if (playerTransform != null && shootingPoint != null && coverController != null)
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
            if (playerTransform == null)
            {
                UnityEngine.Debug.LogWarning("ShootingEnemy: Player Transform (Main Camera) is missing! Cannot operate. Has the Main Camera been destroyed or untagged?", this);
            }
            if (shootingPoint == null)
            {
                UnityEngine.Debug.LogWarning("ShootingEnemy: Shooting Point Transform is missing! Cannot operate.", this);
            }
            if (coverController == null)
            {
                UnityEngine.Debug.LogWarning("ShootingEnemy: CoverControllerMain is missing! Cannot determine player cover status.", this);
            }
        }
    }

    void UpdateHitChance()
    {
        bool playerExposed = !coverController.IsInCover();

        if (playerExposed)
        {
            currentHitChance += hitChanceGainRate * UnityEngine.Time.deltaTime;
        }
        else // Player is in cover
        {
            currentHitChance -= hitChanceLossRate * UnityEngine.Time.deltaTime;
        }

        currentHitChance = UnityEngine.Mathf.Clamp(currentHitChance, 0f, maxHitChance);
    }

    void RotateTowardsPlayer()
    {
        UnityEngine.Vector3 directionToPlayerHorizontal = playerTransform.position - shootingPoint.position;
        directionToPlayerHorizontal.y = 0;
        directionToPlayerHorizontal.Normalize();

        if (directionToPlayerHorizontal == UnityEngine.Vector3.zero) return;

        UnityEngine.Quaternion desiredShootingPointWorldRot = UnityEngine.Quaternion.LookRotation(directionToPlayerHorizontal);
        UnityEngine.Quaternion currentShootingPointToParentRot = UnityEngine.Quaternion.Inverse(transform.rotation) * shootingPoint.rotation;
        UnityEngine.Quaternion targetEnemyRotation = desiredShootingPointWorldRot * UnityEngine.Quaternion.Inverse(currentShootingPointToParentRot);

        targetEnemyRotation.x = 0;
        targetEnemyRotation.z = 0;
        targetEnemyRotation.Normalize();

        transform.rotation = UnityEngine.Quaternion.Slerp(transform.rotation, targetEnemyRotation, UnityEngine.Time.deltaTime * rotationSpeed);

        UnityEngine.Debug.DrawRay(transform.position, transform.forward * 5f, UnityEngine.Color.red, 0.1f);

        UnityEngine.Vector3 actualAimTarget = (playerMainCollider != null) ? playerMainCollider.bounds.center : playerTransform.position;
        UnityEngine.Vector3 actualShootDirection = (actualAimTarget - shootingPoint.position).normalized;

        UnityEngine.Debug.DrawRay(shootingPoint.position, actualShootDirection * 5f, UnityEngine.Color.blue, 0.1f);
        UnityEngine.Debug.DrawRay(actualAimTarget, UnityEngine.Vector3.up * 1f, UnityEngine.Color.green, 0.1f);
    }

    void ShootAtPlayerWithChance()
    {
        UnityEngine.Vector3 targetShootPosition;
        float roll = UnityEngine.Random.value * 100f;
        bool isHit = (roll <= currentHitChance); // This determines if the bullet *should* hit

        // Determine if the bullet should be red (only if it's an intended hit)
        bool thisIsRedBullet = isHit;
        Color currentImpactGlowColor = isHit ? hitGlowColor : missGlowColor; // Use hitGlowColor if it's a hit, else missGlowColor

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
            float missDistance = UnityEngine.Random.Range(_calculatedMissOffsetMin, _calculatedMissOffsetMax);
            float missAngle = UnityEngine.Random.Range(0f, 360f);

            float offsetX = missDistance * UnityEngine.Mathf.Cos(missAngle * UnityEngine.Mathf.Deg2Rad);
            float offsetZ = missDistance * UnityEngine.Mathf.Sin(missAngle * UnityEngine.Mathf.Deg2Rad);

            targetShootPosition = new UnityEngine.Vector3(
                baseAimTarget.x + offsetX,
                baseAimTarget.y,
                baseAimTarget.z + offsetZ
            );
        }

        UnityEngine.Vector3 shootDirection = (targetShootPosition - shootingPoint.position).normalized;

        if (bulletPrefab != null)
        {
            GameObject bullet = UnityEngine.Object.Instantiate(bulletPrefab, shootingPoint.position, UnityEngine.Quaternion.LookRotation(shootDirection));

            if (audioSource != null && shootSound != null)
            {
                audioSource.PlayOneShot(shootSound);
            }

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
                // Pass the 'thisIsRedBullet' flag and the conditional glow color
                bulletBehavior.SetTarget(playerTransform, impactGlowDuration, currentImpactGlowColor, thisIsRedBullet);
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