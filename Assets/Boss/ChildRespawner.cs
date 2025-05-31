// ChildRespawner.cs (Modified to ensure repeated respawn *after destruction*)
using System.Diagnostics;
using UnityEngine;

public class ChildRespawner : MonoBehaviour
{
    public GameObject childPrefab; // Assign your child prefab in the Inspector
    public float respawnDelay = 3f; // Time in seconds before respawning

    private GameObject currentChild;
    private Vector3 initialChildLocalPosition; // Store local position
    private Quaternion initialChildLocalRotation; // Store local rotation
    private bool hasChildBeenSpawnedOnce = false;

    void Start()
    {
        if (childPrefab == null)
        {
            UnityEngine.Debug.LogError("Child Prefab is not assigned in ChildRespawner on " + gameObject.name);
            return;
        }

        // Capture initial local transform if a child exists from the start
        if (transform.childCount > 0)
        {
            initialChildLocalPosition = transform.GetChild(0).localPosition;
            initialChildLocalRotation = transform.GetChild(0).localRotation;
            currentChild = transform.GetChild(0).gameObject; // Reference existing child
            hasChildBeenSpawnedOnce = true;
        }
        else
        {
            // If no child exists, spawn one immediately for the first time
            // This is the initial spawn to get the cycle going
            RespawnChildNow();
        }
    }

    // This method is called by the child's OnDestroy
    public void InitiateRespawnCycle()
    {
        // Cancel any pending respawn to avoid multiple calls if triggered rapidly
        CancelInvoke("RespawnChildNow");
        Invoke("RespawnChildNow", respawnDelay);
        UnityEngine.Debug.Log("Respawn scheduled in " + respawnDelay + " seconds.");
    }

    // This method actually performs the respawn
    void RespawnChildNow()
    {
        if (currentChild != null)
        {
            Destroy(currentChild); // Ensure old child is gone before spawning new
        }

        if (childPrefab != null)
        {
            // Instantiate as a child of this GameObject
            currentChild = Instantiate(childPrefab, transform); // Instantiate as child

            // Set local position/rotation only if it's not the first spawn,
            // or if we explicitly want to reset it for the first spawn.
            // For simplicity, we'll always apply the stored initial.
            currentChild.transform.localPosition = initialChildLocalPosition;
            currentChild.transform.localRotation = initialChildLocalRotation;

            UnityEngine.Debug.Log("Child object respawned!");
            hasChildBeenSpawnedOnce = true; // Mark that a child has now been spawned
        }
    }
}