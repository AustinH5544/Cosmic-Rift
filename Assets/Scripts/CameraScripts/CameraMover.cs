using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraMover : MonoBehaviour
{
    public CinemachineSplineDolly dolly;
    public float moveDuration = 2f;

    public void MoveCameraToTable()
    {
        StartCoroutine(MoveAlongSpline());
    }

    private IEnumerator MoveAlongSpline()
    {
        float elapsedTime = 0f;
        float startPos = dolly.CameraPosition;
        float endPos = 1f; // Assuming Normalized units

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            dolly.CameraPosition = Mathf.Lerp(startPos, endPos, elapsedTime / moveDuration);
            yield return null;
        }

        dolly.CameraPosition = endPos;
    }
}