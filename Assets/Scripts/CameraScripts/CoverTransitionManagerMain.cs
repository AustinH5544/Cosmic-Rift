using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CoverTransitionManagerMain : MonoBehaviour
{
    [SerializeField] private CinemachineSplineDolly dolly;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private List<float> splineStops;
    [SerializeField] private EnemyWaveManager waveManager;

    private float timer = 0f;
    private float startPosition;
    private float endPosition;
    private int currentIndex = 1;

    private bool isMoving = false;

    public bool IsInCombat { get; private set; } = false;
    public int CurrentIndex => currentIndex;
    public List<float> SplineStops => splineStops; // Added public getter

    void Start()
    {
        if (splineStops.Count < 2)
        {
            Debug.LogError("You need at least 2 spline stops.");
            return;
        }

        dolly.CameraPosition = splineStops[0];
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
                waveManager.SpawnWave(currentIndex - 1);
            }
        }

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