using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;

public class SplineKnotLogger : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;

    [ContextMenu("Log Knot Normalized Positions")]
    private void LogKnotNormalizedPositions()
    {
        if (splineContainer == null)
        {
            Debug.LogWarning("SplineContainer not assigned.");
            return;
        }

        var spline = splineContainer.Spline;
        int knotCount = spline.Count;

        Debug.Log($"Spline has {knotCount} knots:");

        for (int i = 0; i < knotCount; i++)
        {
            float normalized = spline.ConvertIndexUnit(i, PathIndexUnit.Knot, PathIndexUnit.Normalized);
            Debug.Log($"Knot {i}: Normalized Position = {normalized:F3}");
        }
    }
}