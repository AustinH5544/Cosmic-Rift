using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CoverTransitionManagerMain : MonoBehaviour
{
    [SerializeField, Tooltip("The Cinemachine Spline Dolly component that controls the camera's position along the spline.")]
    private CinemachineSplineDolly dolly;

    [SerializeField, Tooltip("The duration in seconds it takes to move between cover points.")]
    private float moveDuration = 2f;

    [SerializeField, Tooltip("A list of spline positions (0-1 range) representing distinct cover stops.")]
    private List<float> splineStops;

    [SerializeField, Tooltip("Reference to the EnemyWaveManager to handle enemy spawning.")]
    private EnemyWaveManager waveManager;

    private float timer = 0f; // Timer for tracking movement progress
    private float startPosition; // Starting spline position for a move
    private float endPosition; // Ending spline position for a move
    private int currentIndex = 1; // Current index in splineStops, starting at the first combat point

    private bool isMoving = false; // True when the camera is transitioning between cover points
    private bool isInCombat = false; // True when the player is at a cover point and engaging enemies

    private TimerMain timerScript; // Reference to the TimerMain script
    private CoverControllerMain coverController; // Reference to the CoverControllerMain script

    /// <summary>
    /// Public property indicating whether the player is currently in a combat phase.
    /// </summary>
    public bool IsInCombat { get { return isInCombat; } private set { isInCombat = value; } }

    /// <summary>
    /// The current index of the cover point the player is at or moving towards.
    /// </summary>
    public int CurrentIndex => currentIndex;

    /// <summary>
    /// The list of defined spline stop positions.
    /// </summary>
    public List<float> SplineStops => splineStops;

    void Start()
    {
        if (splineStops.Count < 2)
        {
            UnityEngine.Debug.LogError("CoverTransitionManagerMain: You need at least 2 spline stops defined in the 'Spline Stops' list to function properly (start and at least one combat point).");
            return;
        }

        // Find and assign references to other necessary scripts
        timerScript = UnityEngine.Object.FindFirstObjectByType<TimerMain>();
        if (timerScript == null)
        {
            UnityEngine.Debug.LogError("CoverTransitionManagerMain: TimerMain script not found in the scene! Ensure it is present.");
        }

        coverController = UnityEngine.Object.FindFirstObjectByType<CoverControllerMain>(); // Get reference to CoverControllerMain
        if (coverController == null)
        {
            UnityEngine.Debug.LogError("CoverTransitionManagerMain: CoverControllerMain script not found in the scene! Ensure it is present.");
        }

        if (waveManager == null)
        {
            UnityEngine.Debug.LogError("CoverTransitionManagerMain: EnemyWaveManager is not assigned. Please assign it in the Inspector.");
        }

        // Initialize the camera position to the first spline stop (usually a non-combat starting point)
        dolly.CameraPosition = splineStops[0];

        // Start the game by moving to the first combat cover point
        MoveToCover(currentIndex);
    }

    void Update()
    {
        // Handle camera movement between cover points
        if (isMoving)
        {
            timer += UnityEngine.Time.deltaTime;
            float progress = UnityEngine.Mathf.Clamp01(timer / moveDuration); // Calculate movement progress (0 to 1)
            dolly.CameraPosition = UnityEngine.Mathf.Lerp(startPosition, endPosition, progress);

            if (progress >= 1f)
            {
                UnityEngine.Debug.Log("[Transition] Arrived at target position. Entering combat phase.");
                isMoving = false; // Movement finished
                IsInCombat = true; // Player is now in combat

                // Allow cover controls again once combat starts
                if (coverController != null)
                {
                    coverController.AllowCoverControl(true);
                }

                // Trigger the first wave of enemies for the current combat point
                if (waveManager != null)
                {
                    waveManager.SpawnWave(currentIndex - 1);
                }
            }
        }

        // Check if current combat wave is cleared and initiate move to next cover
        if (!isMoving && IsInCombat && waveManager != null && waveManager.IsWaveCleared())
        {
            UnityEngine.Debug.Log("[Combat] Wave cleared. Preparing to move to next cover.");
            currentIndex++; // Advance to the next cover point index

            if (currentIndex < splineStops.Count)
            {
                // Grant extra time upon clearing a wave, if a timer exists
                if (timerScript != null)
                {
                    timerScript.AddTime(10f); // Example: add 10 seconds to the timer
                }
                MoveToCover(currentIndex); // Start moving to the next cover point
            }
            else
            {
                UnityEngine.Debug.Log("All cover points completed. Game objectives might be complete!");
                // Implement game completion logic here (e.g., end game screen)
            }
        }
    }

    /// <summary>
    /// Initiates a movement of the camera to a specified cover point index along the spline.
    /// </summary>
    /// <param name="index">The index of the target cover point in the splineStops list.</param>
    public void MoveToCover(int index)
    {
        if (index < 0 || index >= splineStops.Count)
        {
            UnityEngine.Debug.LogWarning($"CoverTransitionManagerMain: Invalid cover index '{index}' requested. Index must be within the bounds of 'Spline Stops' list.");
            return;
        }

        UnityEngine.Debug.Log($"[MoveToCover] Initiating move to cover index: {index}");
        timer = 0f; // Reset movement timer
        IsInCombat = false; // Exit combat state during transition
        isMoving = true; // Set moving flag to true

        // Force player out of cover at the start of a transition
        if (coverController != null)
        {
            coverController.ForceOutOfCover();
        }

        // Store current and target spline positions for interpolation
        startPosition = dolly.CameraPosition;
        endPosition = splineStops[index];
        UnityEngine.Debug.Log($"[MoveToCover] Start Position: {startPosition}, End Position: {endPosition}");
    }
}