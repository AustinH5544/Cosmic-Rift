using UnityEngine; // This line still makes 'Debug' accessible directly,
                   // but explicitly writing UnityEngine.Debug is safer.

public class BossCleanUp : MonoBehaviour
{
    // Public variables to assign the child GameObjects in the Inspector.
    public GameObject child1;
    public GameObject child2;

    // Reference to the ChildRespawner script, which we now know is on one of the children.
    private ChildRespawner childRespawnerToStop;

    // Flag to ensure we only stop/remove the ChildRespawner once.
    private bool respawnerActioned = false; // Renamed for clarity, covers both stop and remove

    void Start()
    {
        // Basic error checking: Ensure child references are set.
        if (child1 == null || child2 == null)
        {
            UnityEngine.Debug.LogError("BossCleanUp: Please assign both child GameObjects in the Inspector.", this);
            enabled = false;
            return;
        }

        // Try to find the ChildRespawner script on either child1 or child2.
        if (child1 != null)
        {
            childRespawnerToStop = child1.GetComponent<ChildRespawner>();
            if (childRespawnerToStop != null)
            {
                UnityEngine.Debug.Log($"BossCleanUp: ChildRespawner found on {child1.name}.");
            }
        }

        if (childRespawnerToStop == null && child2 != null)
        {
            childRespawnerToStop = child2.GetComponent<ChildRespawner>();
            if (childRespawnerToStop != null)
            {
                UnityEngine.Debug.Log($"BossCleanUp: ChildRespawner found on {child2.name}.");
            }
        }

        if (childRespawnerToStop == null)
        {
            UnityEngine.Debug.LogWarning("BossCleanUp: ChildRespawner script not found on either specified child GameObject. The respawner will not be actioned (stopped/removed).", this);
        }
    }

    void Update()
    {
        // --- Logic for the ChildRespawner ---
        if (childRespawnerToStop != null && !respawnerActioned)
        {
            // If either child (child1 or child2) is destroyed (becomes null in Unity)
            // OR if the ChildRespawner's *own* GameObject (the one it's attached to) is destroyed (becomes null)
            if (child1 == null || child2 == null || childRespawnerToStop.gameObject == null)
            {
                // Stop any running coroutines before destroying the component.
                childRespawnerToStop.StopAllCoroutines();

                // Destroy the component (or disable it, depending on your preference)
                UnityEngine.Object.Destroy(childRespawnerToStop); // Use UnityEngine.Object.Destroy for components

                UnityEngine.Debug.Log($"BossCleanUp: ChildRespawner component removed from its GameObject because one or more children are dead.");
                respawnerActioned = true; // Set the flag to true to prevent further action.
            }
        }

        // --- Logic for destroying the Boss object based on children's children ---
        bool child1HasNoChildren = (child1 != null && child1.transform.childCount == 0);
        bool child2HasNoChildren = (child2 != null && child2.transform.childCount == 0);

        bool child1ConditionMet = (child1 == null || child1HasNoChildren);
        bool child2ConditionMet = (child2 == null || child2HasNoChildren);

        if (child1ConditionMet && child2ConditionMet)
        {
            UnityEngine.Debug.Log("BossCleanUp: Both children (or their remnants) have no children of their own. Destroying boss object: " + gameObject.name);
            UnityEngine.Object.Destroy(gameObject); // Use UnityEngine.Object.Destroy for GameObjects
        }
    }
}
