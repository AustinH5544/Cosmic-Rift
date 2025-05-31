// ChildDestroyNotifier.cs
using UnityEngine;

public class ChildDestroyNotifier : MonoBehaviour
{
    private ChildRespawner parentRespawner;

    void Start()
    {
        // Find the ChildRespawner on the parent GameObject
        parentRespawner = GetComponentInParent<ChildRespawner>();
        if (parentRespawner == null)
        {
            UnityEngine.Debug.LogError("ChildDestroyNotifier: No ChildRespawner found on parent of " + gameObject.name);
        }
    }

    void OnDestroy()
    {
        // When this child object is destroyed, tell the respawner to initiate a new cycle
        if (parentRespawner != null)
        {
            parentRespawner.InitiateRespawnCycle();
        }
    }
}