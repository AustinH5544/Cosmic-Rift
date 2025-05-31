using System.Diagnostics;
using UnityEngine;

public class FollowShipSpawnPoint : MonoBehaviour
{
    [Tooltip("The Tag assigned to the 'ShipSpawnPoint' object that this object will follow.")]
    public string targetTag = "ShipSpawnPoint"; // Default tag, make sure to create this tag!

    [Tooltip("If true, the follower will maintain its initial offset from the tagged object.")]
    public bool maintainOffset = false;

    private Transform actualTargetTransform; // This will be the Transform of the found tagged object
    private Vector3 initialPositionOffset;
    private Quaternion initialRotationOffset;
    private Vector3 initialScaleRatio;

    void Awake()
    {
        // Try to find the target in Awake, as it's called early in the lifecycle.
        FindTargetByTag();
    }

    void Start()
    {
        // If the target wasn't found in Awake (e.g., if it's spawned slightly later), try again.
        // Or if this script was enabled after the target already existed.
        if (actualTargetTransform == null)
        {
            FindTargetByTag();
        }

        if (actualTargetTransform == null)
        {
            // If still no target, log an error and disable the script.
            UnityEngine.Debug.LogError($"FollowShipSpawnPoint: Could not find any GameObject with Tag '{targetTag}'. Disabling script.", this);
            enabled = false;
        }
        else
        {
            // Calculate initial offsets if maintaining them
            if (maintainOffset)
            {
                initialPositionOffset = transform.position - actualTargetTransform.position;
                initialRotationOffset = Quaternion.Inverse(actualTargetTransform.rotation) * transform.rotation;
                initialScaleRatio = new Vector3(
                    transform.localScale.x / actualTargetTransform.localScale.x,
                    transform.localScale.y / actualTargetTransform.localScale.y,
                    transform.localScale.z / actualTargetTransform.localScale.z
                );
            }
        }
    }

    // Helper method to find the target object by its tag
    private void FindTargetByTag()
    {
        if (string.IsNullOrEmpty(targetTag))
        {
            UnityEngine.Debug.LogError("FollowShipSpawnPoint: 'Target Tag' is not set. Please assign a tag in the Inspector.", this);
            return;
        }

        // Find the GameObject with the specified tag in the entire scene.
        // Note: GameObject.FindGameObjectWithTag only returns the first one found if multiple exist.
        GameObject taggedObject = GameObject.FindGameObjectWithTag(targetTag);

        if (taggedObject != null)
        {
            actualTargetTransform = taggedObject.transform;
            UnityEngine.Debug.Log($"FollowShipSpawnPoint: Found target with Tag '{targetTag}': {actualTargetTransform.name}", this);
        }
        else
        {
            actualTargetTransform = null; // Ensure it's null if not found
            // Debug.LogWarning($"FollowShipSpawnPoint: No GameObject with Tag '{targetTag}' found in the scene.", this); // Can be noisy
        }
    }

    void LateUpdate()
    {
        if (actualTargetTransform == null)
        {
            return; // Do nothing if there's no target or it was disabled
        }

        if (maintainOffset)
        {
            transform.position = actualTargetTransform.position + initialPositionOffset;
            transform.rotation = actualTargetTransform.rotation * initialRotationOffset;
            transform.localScale = new Vector3(
                actualTargetTransform.localScale.x * initialScaleRatio.x,
                actualTargetTransform.localScale.y * initialScaleRatio.y,
                actualTargetTransform.localScale.z * initialScaleRatio.z
            );
        }
        else
        {
            transform.position = actualTargetTransform.position;
            transform.rotation = actualTargetTransform.rotation;
            transform.localScale = actualTargetTransform.localScale;
        }
    }
}