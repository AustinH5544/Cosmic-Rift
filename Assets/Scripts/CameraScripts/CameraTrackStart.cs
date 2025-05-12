using UnityEngine;
using Unity.Cinemachine;

public class CameraTrackStart : MonoBehaviour
{
    public CinemachineSplineDolly dolly;
    public float travelTime = 2f; // Time in seconds to complete movement

    private float timer = 0f;
    private bool isMoving = true;

    void Start()
    {
        if (dolly != null)
            dolly.CameraPosition = 0f; // Ensure it starts at the beginning
    }

    void Update()
    {
        if (!isMoving || dolly == null)
            return;

        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / travelTime);

        dolly.CameraPosition = progress;

        if (progress >= 1f)
            isMoving = false; // Stop once it reaches the end
    }
}