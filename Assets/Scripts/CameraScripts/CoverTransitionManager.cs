using UnityEngine;
using Unity.Cinemachine;

public class CoverTransitionManager : MonoBehaviour
{
    public CinemachineSplineDolly dolly;
    public float moveDuration = 2f;
    public Transform[] coverPoints; // Add cover stop positions (e.g., 0f, 0.5f, 1f)
    public int currentIndex = 0;

    private float timer = 0f;
    private bool isMoving = false;

    public bool IsInCombat { get; private set; } = false;

    void Start()
    {
        MoveToCover(currentIndex); // Start at first cover point
    }

    void Update()
    {
        if (!isMoving || dolly == null) return;

        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / moveDuration);
        dolly.CameraPosition = Mathf.Lerp(0, 1, progress);

        if (progress >= 1f)
        {
            isMoving = false;
            IsInCombat = true; // Combat is ready!
        }
    }

    public void MoveToCover(int index)
    {
        if (index < 0 || index >= coverPoints.Length) return;

        // Reset
        timer = 0f;
        isMoving = true;
        IsInCombat = false;

        // Set camera to new spline start and move to next position
        dolly.CameraPosition = 0f; // assuming path goes 0 -> 1 per cover
        currentIndex = index;
    }
}