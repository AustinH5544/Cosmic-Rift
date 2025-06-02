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
        Debug.Log($"TargetSpawner initialized. Max active targets: {maxActiveTargets}");
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0 && activeTargetCount < maxActiveTargets)
        {
            SpawnTarget();
            spawnTimer = Random.Range(spawnIntervalMin, spawnIntervalMax);
            Debug.Log($"Spawning new target. Active targets: {activeTargetCount}");
        }
    }

    void SpawnTarget()
    {
        float randomZ = Random.Range(wallXMin, wallXMax);
        float randomY = Random.Range(wallYMin, wallYMax);
        float fixedX = -wallZPosition;

        
        Vector3 spawnPosition = new Vector3(fixedX, randomY, randomZ);

        GameObject newTarget = Instantiate(targetPrefab, spawnPosition, Quaternion.Euler(0, 180, 0));
        newTarget.tag = "Target";
        activeTargetCount++;
    }

    public void OnTargetDestroyed()
    {
        activeTargetCount--;
        if (activeTargetCount < 0) activeTargetCount = 0;
        Debug.Log($"Target destroyed. Active targets now: {activeTargetCount}");
    }
}