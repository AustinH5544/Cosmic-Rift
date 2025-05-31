using UnityEngine;

public class BossCleanUp : MonoBehaviour
{
    // Public variables to assign the child GameObjects in the Inspector.
    // Assign these references in the Unity Editor after attaching this script to your boss object.
    public GameObject child1;
    public GameObject child2;

    // Reference to the ChildRespawner script, assumed to be on the same GameObject.
    private ChildRespawner childRespawner;

    // Flag to ensure we only stop the ChildRespawner once.
    private bool respawnerStopped = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the ChildRespawner component from this GameObject.
        // If the ChildRespawner is on a different GameObject, you'll need to adjust this line.
        childRespawner = GetComponent<ChildRespawner>();

        // Basic error checking: Ensure child references are set.
        if (child1 == null || child2 == null)
        {
            Debug.LogError("BossCleanUp: Please assign both child GameObjects in the Inspector.", this);
            // Optionally disable this script if children are not assigned to prevent errors.
            enabled = false;
        }

        // Basic error checking: Ensure ChildRespawner script is found.
        if (childRespawner == null)
        {
            Debug.LogWarning("BossCleanUp: ChildRespawner script not found on this GameObject. " +
                             "The script will not be stopped.", this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if either child is destroyed and if the respawner hasn't been stopped yet.
        // 'child1 == null' checks if the GameObject reference is no longer valid (i.e., it has been destroyed).
        if ((child1 == null || child2 == null) && !respawnerStopped)
        {
            // If the ChildRespawner exists, disable it.
            if (childRespawner != null)
            {
                childRespawner.enabled = false;
                Debug.Log("BossCleanUp: ChildRespawner script has been stopped because one or more children are dead.");
            }
            respawnerStopped = true; // Set the flag to true to prevent stopping it again.
        }

        // Check if both children are destroyed.
        if (child1 == null && child2 == null)
        {
            Debug.Log("BossCleanUp: All children are destroyed. Destroying boss object: " + gameObject.name);
            // Destroy this GameObject (the boss object).
            Destroy(gameObject);
        }
    }
}
