using System.Diagnostics;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class EnemyBloodGush : MonoBehaviour
{
    [Tooltip("Assign your Blood Particle System Prefab here.")]
    [SerializeField] private GameObject bloodGushEffectPrefab;

    [Tooltip("Offset the particle system's position relative to the enemy.")]
    public Vector3 effectOffset = new Vector3(0, 0.5f, 0); // Adjust as needed

    void OnDestroy()
    {
        // Check if the application is quitting to prevent errors when stopping play mode
        if (Application.isQuitting)
        {
            return;
        }

        // Instantiate the blood gush effect at the enemy's position
        if (bloodGushEffectPrefab != null)
        {
            // Get the enemy's position and add the offset
            Vector3 spawnPosition = transform.position + effectOffset;

            // Instantiate the blood effect
            GameObject bloodEffect = Instantiate(bloodGushEffectPrefab, spawnPosition, Quaternion.identity);

            // Optional: Destroy the particle system GameObject after its duration
            // You might need to get the ParticleSystem component to find its duration
            ParticleSystem ps = bloodEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(bloodEffect, ps.main.duration);
            }
            else
            {
                // If there's no ParticleSystem component, destroy after a default time
                Destroy(bloodEffect, 3f); // Destroy after 3 seconds if no particle system found
            }
        }
        else
        {
            Debug.LogWarning("Blood Gush Effect Prefab is not assigned on " + gameObject.name);
        }
    }
}