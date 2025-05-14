using UnityEngine;
using Unity.Cinemachine;

public class CoverController : MonoBehaviour
{
    public CinemachineSplineDolly dolly;
    public float duckY = -1f;
    public float standY = 0f;
    public float transitionSpeed = 5f; // Higher = faster

    private bool isInCover = true;
    private float targetY;

    void Start()
    {
        isInCover = false;
        targetY = standY;
        SetSplineY(standY);
    }

    void Update()
    {
        if (!FindObjectOfType<CoverTransitionManager>().IsInCombat)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleCover();
        }

        // Smooth transition toward targetY
        Vector3 offset = dolly.SplineOffset;
        offset.y = Mathf.Lerp(offset.y, targetY, Time.deltaTime * transitionSpeed);
        dolly.SplineOffset = offset;
    }

    void ToggleCover()
    {
        isInCover = !isInCover;
        targetY = isInCover ? duckY : standY;
    }

    public bool IsInCover()
    {
        return isInCover;
    }

    private void SetSplineY(float y)
    {
        Vector3 offset = dolly.SplineOffset;
        offset.y = y;
        dolly.SplineOffset = offset;
    }
}