using UnityEngine;
using System.Collections.Generic;

public class EnemyWaveManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> enemyPrefabs;     // Prefabs with multiple children (tagged "Target")
    [SerializeField] private List<Transform> spawnPoints;       // Each is a parent with multiple spawn positions

    private int enemiesAlive = 0;

    void OnEnable()
    {
        Enemy.OnAnyEnemyDeath += HandleEnemyDeath;
    }

    void OnDisable()
    {
        Enemy.OnAnyEnemyDeath -= HandleEnemyDeath;
    }

    public void SpawnWave(int index)
    {
        // Clean up previous enemies
        foreach (var oldTarget in GameObject.FindGameObjectsWithTag("Target"))
        {
            Destroy(oldTarget);
        }

        if (index < 0 || index >= enemyPrefabs.Count || index >= spawnPoints.Count)
        {
            Debug.LogWarning("Invalid index for enemy prefab or spawn point: " + index);
            return;
        }

        GameObject wave = Instantiate(enemyPrefabs[index], Vector3.zero, Quaternion.identity);
        enemiesAlive = 0;

        Transform spawnGroup = spawnPoints[index];
        List<Transform> spawnLocations = new List<Transform>();
        foreach (Transform child in spawnGroup)
        {
            spawnLocations.Add(child);
        }

        int spawnIndex = 0;

        foreach (Transform child in wave.transform)
        {
            if (child.CompareTag("Target"))
            {
                Transform spawnPoint = spawnLocations[spawnIndex % spawnLocations.Count];
                child.position = spawnPoint.position;

                enemiesAlive++;
                spawnIndex++;
            }
            else
            {
                Debug.LogWarning($"Enemy prefab child '{child.name}' is missing the 'Target' tag.");
            }
        }

        Debug.Log($"[SpawnWave] Wave {index} spawned with {enemiesAlive} enemies.");
    }

    void HandleEnemyDeath()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        Debug.Log($"[EnemyDeath] enemiesAlive = {enemiesAlive}");
    }

    public bool IsWaveCleared()
    {
        return enemiesAlive <= 0;
    }
}