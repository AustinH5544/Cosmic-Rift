using UnityEngine;

public class EnemyWaveManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;

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
        Debug.Log("Spawning enemy...");

        foreach (var oldTarget in GameObject.FindGameObjectsWithTag("Target"))
        {
            Destroy(oldTarget);
        }

        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemiesAlive = 1;

        Debug.Log($"Enemy spawned at {spawnPoint.position}, enemiesAlive = {enemiesAlive}");
    }

    void HandleEnemyDeath()
    {
        enemiesAlive--;
    }

    public bool IsWaveCleared()
    {
        return enemiesAlive <= 0;
    }
}