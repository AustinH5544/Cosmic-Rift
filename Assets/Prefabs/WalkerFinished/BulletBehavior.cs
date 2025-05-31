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

    void Start()
    {
        bulletRenderer = GetComponent<Renderer>();
        if (bulletRenderer != null)
        {
            originalMaterial = bulletRenderer.material; // Store the original material
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
                bulletRenderer.material.SetColor("_EmissionColor", Color.Lerp(Color.black, impactGlowColor, (Time.time - glowStartTime) / impactGlowDuration));
                bulletRenderer.material.EnableKeyword("_EMISSION");
            }
        }
        else if (hasHit && glowStartTime > 0 && bulletRenderer != null)
        {
            bulletRenderer.material = originalMaterial; // Revert to the original material
            Destroy(gameObject); // Destroy the bullet after the glow
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasHit && other.transform == targetTransform)
        {
            hasHit = true;
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
            // The bullet will be destroyed after the glow duration in the Update function
        }
    }
}