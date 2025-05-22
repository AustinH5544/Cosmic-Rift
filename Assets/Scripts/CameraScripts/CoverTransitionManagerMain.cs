using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CoverTransitionManagerMain : MonoBehaviour
{
    [SerializeField] private CinemachineSplineDolly dolly;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private List<float> splineStops; // Normalized spline positions (e.g., 0f, 0.3f, 0.6f, 1f)
    [SerializeField] private EnemyWaveManager waveManager;

    private float timer = 0f;
    private float startPosition;
    private float endPosition;
    private int currentIndex = 1; // index of the next spline stop
    private bool isMoving = false;

    public bool IsInCombat { get; private set; } = false;

    void Start()
    {
        if (splineStops.Count < 2)
        {
            Debug.LogError("You need at least 2 spline stops.");
            return;
        }

        // Set camera to initial position
        dolly.CameraPosition = splineStops[0];

        // Start moving to the first real cover point
        MoveToCover(currentIndex);
    }

    void Update()
    {
        if (isMoving)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / moveDuration);
            dolly.CameraPosition = Mathf.Lerp(startPosition, endPosition, progress);

            if (progress >= 1f)
            {
                Debug.Log("[Transition] Arrived at target position");
                isMoving = false;
                IsInCombat = true;

                // Spawn wave that matches the cover position we just arrived at
                waveManager.SpawnWave(currentIndex - 1);
            }
        }

        // Move to next cover point after wave is cleared
        if (!isMoving && IsInCombat && waveManager.IsWaveCleared())
        {
            currentIndex++;
            if (currentIndex < splineStops.Count)
            {
                MoveToCover(currentIndex);
            }
            else
            {
                Debug.Log("All cover points completed.");
            }
        }
    }

    public void MoveToCover(int index)
    {
        if (index < 0 || index >= splineStops.Count)
        {
            Debug.LogWarning("Invalid cover index");
            return;
        }

        timer = 0f;
        IsInCombat = false;
        isMoving = true;

        startPosition = dolly.CameraPosition;
        endPosition = splineStops[index];
        Debug.Log($"[MoveToCover] StartPos: {startPosition}, EndPos: {endPosition}, Index: {index}");
    }
}