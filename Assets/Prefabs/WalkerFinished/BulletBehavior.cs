using System.Diagnostics;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    private Transform targetTransform;
    private float impactGlowDuration;
    private Color impactGlowColor;
    private Renderer bulletRenderer;
    private Material originalMaterial;
    private float glowStartTime;
    private bool hasHit = false;

    public int damageAmount = 10; // Default damage, ensure PlayerHealth.TakeDamage accepts a float

    void Start()
    {
        bulletRenderer = GetComponent<Renderer>();
        if (bulletRenderer != null)
        {
            originalMaterial = bulletRenderer.material; // Store the original material
        }

        // --- Rigidbody and Collider Setup Reminders ---
        // For OnTriggerEnter to work, at least one of the colliding objects
        // MUST have a Rigidbody. It's common to put it on the moving object.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            // Add a Rigidbody if one doesn't exist.
            // This is crucial for trigger events to fire.
            rb = gameObject.AddComponent<Rigidbody>();
        }
        // Set Rigidbody to IsKinematic if you're controlling movement manually
        // and don't want physics forces (like gravity) to affect it.
        rb.isKinematic = true;

        // Ensure the bullet has a Collider and it's set to Is Trigger
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider == null)
        {
            UnityEngine.Debug.LogWarning("BulletBehavior: No Collider found on this GameObject. OnTriggerEnter will not work without one!", this);
        }
        else if (!bulletCollider.isTrigger)
        {
            UnityEngine.Debug.LogWarning("BulletBehavior: Collider is not set to 'Is Trigger'. Change it in the Inspector for OnTriggerEnter to work!", this);
        }
    }

    public void SetTarget(Transform target, float glowDuration, Color glowColor)
    {
        targetTransform = target;
        impactGlowDuration = glowDuration;
        impactGlowColor = glowColor;
    }

    void Update()
    {
        // Handle impact glow
        if (hasHit && glowStartTime > 0 && Time.time < glowStartTime + impactGlowDuration)
        {
            if (bulletRenderer != null)
            {
                // Ensure the material has emission enabled
                bulletRenderer.material.EnableKeyword("_EMISSION");
                bulletRenderer.material.SetColor("_EmissionColor", Color.Lerp(Color.black, impactGlowColor, (Time.time - glowStartTime) / impactGlowDuration));
            }
        }
        else if (hasHit && glowStartTime > 0 && bulletRenderer != null)
        {
            // Revert to original material and destroy after glow
            bulletRenderer.material = originalMaterial;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the bullet has already hit something
        if (hasHit)
        {
            return; // Exit if already hit to prevent multiple hits
        }

        // Try to get the PlayerHealth component from the collided object
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        // If the collided object has a PlayerHealth component, it's the player
        if (playerHealth != null)
        {
            // The bullet has hit the player
            playerHealth.TakeDamage(damageAmount);
            hasHit = true; // Mark as hit

            // Initiate glow effect
            if (bulletRenderer != null)
            {
                glowStartTime = Time.time;
            }

            // Optionally disable the bullet's collider immediately after hitting
            // to prevent further trigger events with other objects or the same object
            Collider bulletCollider = GetComponent<Collider>();
            if (bulletCollider != null)
            {
                bulletCollider.enabled = false;
            }

            return; // Exit after handling player hit
        }

        // If the bullet was set to target a specific transform and it hits that target
        if (targetTransform != null && other.transform == targetTransform)
        {
            hasHit = true; // Mark as hit

            // Apply the glow effect
            if (bulletRenderer != null)
            {
                glowStartTime = Time.time;
            }

            // Optionally disable the collider to prevent further triggers
            Collider bulletCollider = GetComponent<Collider>();
            if (bulletCollider != null)
            {
                bulletCollider.enabled = false;
            }
        }
        // Add a condition here if you want the bullet to destroy on hitting *anything* else
         else if (!hasHit) // If it hits something that's not the player or target
         {
             Destroy(gameObject); // Destroy immediately without glow
         }
    }
}
