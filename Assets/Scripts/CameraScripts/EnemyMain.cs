using UnityEngine;
using System;

public class EnemyMain : MonoBehaviour
{
    public static event Action OnAnyEnemyDeath;

    void OnDestroy()
    {
        OnAnyEnemyDeath?.Invoke();
    }
}