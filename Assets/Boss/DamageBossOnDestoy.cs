using System.Diagnostics;
using UnityEngine;

public class DamageBossOnDestroy : MonoBehaviour
{
    private BossHP bossHP;
    private ChildRespawner childRespawner;

    void Start()
    {
        // --- Existing BossHP and ChildRespawner setup ---
        GameObject bossObject = GameObject.FindWithTag("Boss");
        if (bossObject != null)
        {
            bossHP = bossObject.GetComponent<BossHP>();
            if (bossHP == null)
            {
                UnityEngine.Debug.LogError("BossHP script not found on the Boss object.");
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Boss object with 'Boss' tag not found in the scene.");
        }

        childRespawner = GetComponentInParent<ChildRespawner>();
        if (childRespawner == null)
        {
            UnityEngine.Debug.LogError("ChildRespawner script not found on the parent of " + gameObject.name + ". Ensure this object is a child of a GameObject with ChildRespawner.");
        }

        // --- New code to make the object glow bright red ---
        Renderer objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            // Check if the material supports emission.
            // Standard shader typically uses "_EMISSION" keyword and "_EmissionColor" property.
            Material material = objectRenderer.material;

            // Set the main color of the object to red
            material.color = Color.red;

            // Enable emission and set the emission color for a glow effect
            // The intensity (e.g., * 5f) can be adjusted to make it brighter or dimmer.
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.red * 5f); // Adjust 5f for desired glow intensity

            UnityEngine.Debug.Log("Object " + gameObject.name + " material set to glow bright red.");
        }
        else
        {
            UnityEngine.Debug.LogWarning("No Renderer component found on " + gameObject.name + ". Cannot make it glow.");
        }
    }

    void OnDestroy()
    {
        // --- Existing OnDestroy logic ---
        if (bossHP != null)
        {
            bossHP.TakeDamage(1);
        }
        else
        {
            UnityEngine.Debug.LogWarning("BossHP not found. Cannot deal damage.");
        }

        if (childRespawner != null)
        {
            // Call the new method to initiate the respawn cycle
            childRespawner.InitiateRespawnCycle();
        }
        else
        {
            UnityEngine.Debug.LogWarning("ChildRespawner not found. Cannot initiate respawn.");
        }
    }
}