// MultiChildSpawner.cs
// Attach this script to an empty GameObject in your Unity scene.
// Assign your child prefab to the 'Child Prefab' slot in the Inspector.

using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for List<T>
using UnityEngine;

public class MultiChildSpawner : MonoBehaviour
{
    [Tooltip("The prefab GameObject that will be instantiated as a child.")]
    public GameObject childPrefab;

    [Tooltip("The maximum number of children to keep active at any given time during the initial spawning phase.")]
    [Range(1, 10)] // A slider in the Inspector, limiting max children from 1 to 10.
    public int maxChildren = 3; // Default to 3 children initially.

    [Tooltip("The total number of children to spawn before switching to single-child maintenance mode. Set to 0 for infinite initial spawning.")]
    [Range(0, 100)] // Allows setting a limit from 0 (infinite) to 100.
    public int spawnLimit = 6; // Default to switching after 6 children are spawned.

    [Tooltip("The time in seconds to wait before attempting to spawn the next child.")]
    public float spawnInterval = 2f; // Default time spacing.

    [Tooltip("The local position relative to this spawner's transform where new children will be instantiated.")]
    public Vector3 spawnLocalPosition = Vector3.zero; // Default to spawning at the spawner's pivot.

    [Tooltip("The local rotation relative to this spawner's transform where new children will be instantiated.")]
    public Quaternion spawnLocalRotation = Quaternion.identity; // Default to no local rotation offset.

    // A list to keep track of all currently active (non-null) child GameObjects.
    private List<GameObject> activeChildren = new List<GameObject>();

    // Tracks the total number of children ever spawned by this spawner.
    private int totalChildrenSpawned = 0;

    // New: Flag to indicate if the initial spawn limit has been reached.
    private bool spawnLimitReached = false;

    // A reference to the coroutine, so we can stop it if needed (e.g., when the spawner is disabled).
    private Coroutine spawnManagementCoroutine;

    void Start()
    {
        // Basic error checking: ensure a prefab is assigned.
        if (childPrefab == null)
        {
            UnityEngine.Debug.LogError("Child Prefab is not assigned in MultiChildSpawner on " + gameObject.name + ". Please assign a prefab in the Inspector.");
            // Disable the script to prevent further errors if no prefab is set.
            enabled = false;
            return;
        }

        // Start the coroutine that will manage the spawning of children.
        spawnManagementCoroutine = StartCoroutine(ManageChildSpawning());
        UnityEngine.Debug.Log("MultiChildSpawner started. Initial max active children: " + maxChildren + ", spawn interval: " + spawnInterval + "s. Total spawn limit before single-child mode: " + (spawnLimit == 0 ? "Infinite" : spawnLimit.ToString()));
    }

    /// <summary>
    /// Coroutine to continuously manage the spawning of children.
    /// It checks the count of active children and spawns new ones if below the maximum,
    /// waiting for the specified interval between each new spawn.
    /// </summary>
    IEnumerator ManageChildSpawning()
    {
        while (true) // This loop runs indefinitely while the script is active.
        {
            // Clean up our list: remove any entries that are now null (meaning the child GameObject has been destroyed).
            activeChildren.RemoveAll(child => child == null);

            if (!spawnLimitReached) // Initial spawning phase: spawn up to maxChildren until spawnLimit is hit
            {
                // Check if the total spawn limit has been reached (if spawnLimit is not 0 for infinite spawning).
                if (spawnLimit > 0 && totalChildrenSpawned >= spawnLimit)
                {
                    UnityEngine.Debug.Log($"Initial spawn limit of {spawnLimit} reached. Transitioning to single child maintenance mode.");
                    spawnLimitReached = true; // Set flag to true to switch behavior
                    // Yield to allow the new state to apply in the next iteration
                    yield return new WaitForSeconds(0.5f);
                    continue; // Skip the rest of this iteration and re-evaluate with the new state
                }

                // If not yet at the total spawn limit, continue spawning up to maxChildren
                if (activeChildren.Count < maxChildren)
                {
                    SpawnSingleChild();
                    yield return new WaitForSeconds(spawnInterval);
                }
                else
                {
                    // If we already have the maximum number of children, wait a short period before checking again.
                    yield return new WaitForSeconds(0.5f);
                }
            }
            else // After spawn limit reached: maintain only one active child
            {
                if (activeChildren.Count == 0)
                {
                    // If no child is active, spawn one.
                    SpawnSingleChild();
                    yield return new WaitForSeconds(spawnInterval); // Use spawnInterval for consistency
                }
                else
                {
                    // If one or more children are active, just wait.
                    // The system will naturally converge to one as existing children are destroyed.
                    yield return new WaitForSeconds(0.5f); // Check more frequently to react to destruction
                }
            }
        }
    }

    /// <summary>
    /// Helper method to instantiate and configure a single child GameObject.
    /// </summary>
    void SpawnSingleChild()
    {
        // Instantiate the child prefab. We pass 'transform' as the parent,
        // so the new child will appear under this GameObject in the Hierarchy.
        GameObject newChild = Instantiate(childPrefab, transform);

        // Apply the specified local position and rotation relative to the spawner.
        newChild.transform.localPosition = spawnLocalPosition;
        newChild.transform.localRotation = spawnLocalRotation;

        // Add the newly spawned child to our list of active children.
        activeChildren.Add(newChild);

        // Increment the total count of children spawned.
        totalChildrenSpawned++;

        // Log a message to the console for debugging purposes.
        string mode = spawnLimitReached ? " (Maintenance Mode)" : "";
        UnityEngine.Debug.Log($"Spawned child {activeChildren.Count}/{(spawnLimitReached ? 1 : maxChildren)} (Total: {totalChildrenSpawned}/{spawnLimit}){mode}. Next potential spawn in {spawnInterval} seconds.");
    }

    /// <summary>
    /// Called when the GameObject this script is attached to is disabled.
    /// It's good practice to stop ongoing coroutines to prevent errors or unexpected behavior.
    /// </summary>
    void OnDisable()
    {
        if (spawnManagementCoroutine != null)
        {
            StopCoroutine(spawnManagementCoroutine);
            UnityEngine.Debug.Log("MultiChildSpawner coroutine stopped due to OnDisable.");
        }
    }

    /// <summary>
    /// Called when the GameObject this script is attached to is destroyed.
    /// This ensures the coroutine is stopped even if the GameObject is destroyed directly.
    /// </summary>
    void OnDestroy()
    {
        if (spawnManagementCoroutine != null)
        {
            StopCoroutine(spawnManagementCoroutine);
            UnityEngine.Debug.Log("MultiChildSpawner coroutine stopped due to OnDestroy.");
        }
    }
}
