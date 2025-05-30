using UnityEngine;

public class BulletMover : MonoBehaviour
{
    private UnityEngine.Vector3 moveDirection;
    private float moveSpeed;
    private UnityEngine.Transform targetTransform; // Still needed for the 'destroy if passed target' logic

    // Removed: predictionDistance, predictionRadius, playerLayer, playerMainCollider

    [Header("Warning Color")]
    public UnityEngine.Color warningColor = UnityEngine.Color.red;
    public float fadeSpeed = 5f; // How fast the warning color fades in/out

    private UnityEngine.Renderer bulletRenderer;
    private UnityEngine.Material bulletInstanceMaterial; // Unique material instance for this bullet
    private UnityEngine.Color originalBaseColor; // Store the original base color of the bullet
    private bool isPredictingHit = false; // Now set directly by the enemy

    void Awake()
    {
        bulletRenderer = GetComponent<UnityEngine.Renderer>();
        if (bulletRenderer != null)
        {
            bulletInstanceMaterial = bulletRenderer.material; // Get a unique material instance
            originalBaseColor = bulletInstanceMaterial.color; // Store its initial base color
        }
        else
        {
            UnityEngine.Debug.LogError("BM: No Renderer found on " + gameObject.name + "!");
        }
    }

    /// <summary>
    /// Sets the initial movement direction, speed, and whether this bullet is an intended hit.
    /// </summary>
    /// <param name="direction">The movement direction of the bullet.</param>
    /// <param name="speed">The movement speed of the bullet.</param>
    /// <param name="isIntendedHit">True if this shot is an intended hit by the enemy, false otherwise.</param>
    public void SetDirectionAndSpeed(UnityEngine.Vector3 direction, float speed, UnityEngine.Transform target, bool isIntendedHit)
    {
        moveDirection = direction;
        moveSpeed = speed;
        targetTransform = target; // Still used for destroy condition
        isPredictingHit = isIntendedHit; // Direct assignment for the red glow
    }

    void Update()
    {
        // Move the bullet directly by updating its position
        transform.position += moveDirection * moveSpeed * UnityEngine.Time.deltaTime;

        // Apply Warning Color or revert to original based on predetermined hit status
        if (bulletInstanceMaterial != null)
        {
            if (isPredictingHit) // This is now directly controlled by the enemy's hit/miss roll
            {
                bulletInstanceMaterial.color = UnityEngine.Color.Lerp(bulletInstanceMaterial.color, warningColor, UnityEngine.Time.deltaTime * fadeSpeed);
            }
            else
            {
                bulletInstanceMaterial.color = UnityEngine.Color.Lerp(bulletInstanceMaterial.color, originalBaseColor, UnityEngine.Time.deltaTime * fadeSpeed);
            }
        }

        // Destroy the bullet if it has passed the target player
        if (targetTransform != null)
        {
            UnityEngine.Vector3 directionToTarget = targetTransform.position - transform.position;
            if (UnityEngine.Vector3.Dot(directionToTarget, moveDirection) < -0.1f)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }
        // Failsafe: Destroy the bullet after a maximum lifetime
        UnityEngine.Object.Destroy(gameObject, 10f);
    }
}