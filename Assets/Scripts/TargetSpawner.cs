using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    public GameObject targetPrefab;
    public float spawnIntervalMin = 1f;
    public float spawnIntervalMax = 3f;
    public int maxActiveTargets = 5;
    public float wallZPosition = -7f;
    public float wallXMin = -10f;
    public float wallXMax = 15f;
    public float wallYMin = 0f;
    public float wallYMax = 2f;

    private float spawnTimer;
    private int activeTargetCount;

    void Start()
    {
        spawnTimer = Random.Range(spawnIntervalMin, spawnIntervalMax);
        activeTargetCount = 0;
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0 && activeTargetCount < maxActiveTargets)
        {
            SpawnTarget();
            spawnTimer = Random.Range(spawnIntervalMin, spawnIntervalMax);
        }
    }

    void SpawnTarget()
    {
        // Adjust for the ShootingRange prefab's rotation (e.g., (0, 90, 0))
        // Local X (spread across wall) aligns with world Z
        // Local Z (fixed on wall) aligns with world -X
        float randomZ = Random.Range(wallXMin, wallXMax); // Spread along world Z (wall's width)
        float randomY = Random.Range(wallYMin, wallYMax); // Spread along world Y (wall's height)
        float fixedX = -wallZPosition; // Fixed position along world X (wall's depth)

        Vector3 spawnPosition = new Vector3(fixedX, randomY, randomZ);

        GameObject newTarget = Instantiate(targetPrefab, spawnPosition, Quaternion.Euler(0, 0, 0));
        newTarget.tag = "Target";
        activeTargetCount++;
    }

    public void OnTargetDestroyed()
    {
        activeTargetCount--;
        if (activeTargetCount < 0) activeTargetCount = 0;
    }
}