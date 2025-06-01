using UnityEngine;
using Unity.Cinemachine;

public class CoverControllerMain : MonoBehaviour
{
    [Tooltip("The Cinemachine Spline Dolly component controlling the camera's position.")]
    public CinemachineSplineDolly dolly;

    [Tooltip("The Y-offset for the camera when the player is in cover (ducked).")]
    public float duckY = -1f;

    [Tooltip("The Y-offset for the camera when the player is standing.")]
    public float standY = 0f;

    [Tooltip("The speed at which the camera transitions between ducking and standing. Higher value means faster transition.")]
    public float transitionSpeed = 5f; // Higher = faster

    // CHANGE: Initialize isInCover to false and targetY to standY
    private bool isInCover = false; // Start NOT in cover by default
    private float targetY;
    private bool canControlCover = false; // New flag to enable/disable cover control

    private CoverTransitionManagerMain transitionManager; // Reference to the CoverTransitionManagerMain

    void Start()
    {
        // CHANGE: Initialize to the stood position
        targetY = standY;
        SetSplineY(standY); // Set initial camera Y to standY directly

        // Find the CoverTransitionManagerMain instance in the scene
        transitionManager = UnityEngine.Object.FindFirstObjectByType<CoverTransitionManagerMain>();
        if (transitionManager == null)
        {
            UnityEngine.Debug.LogError("CoverControllerMain: CoverTransitionManagerMain script not found in the scene! Please ensure it's in the scene.");
        }

        // Initially, cover control is disabled, and the player is forced out of cover.
        // The ForceOutOfCover() method will handle this state.
        ForceOutOfCover();
    }

    void Update()
    {
        // Declare offset once at the beginning of Update
        Vector3 offset = dolly.SplineOffset;

        // Only allow cover changes if allowed by the canControlCover flag
        if (!canControlCover)
        {
            // If not allowed to control cover, ensure the player is forced out of cover (standing)
            targetY = standY; // Force to standing position
            isInCover = false; // Not in cover

            offset.y = UnityEngine.Mathf.Lerp(offset.y, targetY, UnityEngine.Time.deltaTime * transitionSpeed);
            dolly.SplineOffset = offset;
            return; // Exit Update if not in combat (or not allowed to control cover)
        }

        // If the Spacebar is held down, the player stands up (not in cover); otherwise, they duck (in cover).
        if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.Space)) // Use GetKey to check if the key is held down
        {
            targetY = standY;
            isInCover = false;
        }
        else
        {
            targetY = duckY;
            isInCover = true;
        }

        // Assign to the already declared 'offset'
        offset.y = UnityEngine.Mathf.Lerp(offset.y, targetY, UnityEngine.Time.deltaTime * transitionSpeed);
        dolly.SplineOffset = offset;
    }

    /// <summary>
    /// Returns true if the player is currently in cover (ducked down).
    /// </summary>
    public bool IsInCover()
    {
        return isInCover;
    }

    /// <summary>
    /// Sets whether the player can control cover (duck/stand).
    /// </summary>
    /// <param name="canControl">True to enable cover control, false to disable.</param>
    public void AllowCoverControl(bool canControl)
    {
        canControlCover = canControl;
    }

    /// <summary>
    /// Forces the player out of cover and disables cover control.
    /// </summary>
    public void ForceOutOfCover()
    {
        targetY = standY;
        isInCover = false;
        canControlCover = false; // Disable cover control
    }

    /// <summary>
    /// Sets the Cinemachine Spline Dolly's Y offset directly. Useful for initial positioning or forced states.
    /// </summary>
    /// <param name="y">The Y offset value to set for the spline dolly.</param>
    private void SetSplineY(float y)
    {
        Vector3 offset = dolly.SplineOffset;
        offset.y = y;
        dolly.SplineOffset = offset;
    }
}
