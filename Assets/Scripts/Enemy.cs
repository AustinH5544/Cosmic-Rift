using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    public static event Action OnAnyEnemyDeath;

    public float despawnTime = 5f;
    private float timer;
    private bool hasNotified = false;

    void Start()
    {
        timer = despawnTime;
        Debug.Log($"Enemy {gameObject.name} initialized with despawnTime: {despawnTime}");
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        timer -= Time.deltaTime;
        Debug.Log($"Enemy {gameObject.name} timer: {timer}");
        if (timer <= 0 && !hasNotified)
        {
            NotifySpawner();
            hasNotified = true;
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (!hasNotified)
        {
            NotifySpawner();
            hasNotified = true;
        }
        OnAnyEnemyDeath?.Invoke();
    }

    void NotifySpawner()
    {
        TargetSpawner spawner = FindObjectOfType<TargetSpawner>();
        if (spawner != null)
        {
            Debug.Log($"Enemy {gameObject.name} notifying TargetSpawner.");
            spawner.OnTargetDestroyed();
        }
        else
        {
            Debug.LogWarning($"Enemy {gameObject.name} could not find TargetSpawner!");
        }
    }
}