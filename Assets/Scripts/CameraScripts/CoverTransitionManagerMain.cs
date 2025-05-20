using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CoverTransitionManagerMain : MonoBehaviour
{
    [SerializeField] private CinemachineSplineDolly dolly;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private List<float> splineStops; // Normalized spline positions (0f to 1f)
    [SerializeField] private EnemyWaveManager waveManager;

    private float timer = 0f;
    private float startPosition;
    private float endPosition;
    private int currentIndex = 0;
    private bool isMoving = false;

    public bool IsInCombat { get; private set; } = false;

    void Start()
    {
        currentIndex = 1;
        MoveToCover(currentIndex);
        Debug.Log("splineStops.Count: " + splineStops.Count);
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
                isMoving = false;
                IsInCombat = true;

                // Spawn enemies for this segment
                waveManager.SpawnWave(currentIndex);
            }
        }

        // Proceed if enemies are cleared
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
        Debug.Log($"[Transition] Moving: {isMoving}, InCombat: {IsInCombat}, CameraPos: {dolly.CameraPosition}");
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
    }
}