using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    public static event Action OnAnyEnemyDeath;

    void OnDestroy()
    {
        OnAnyEnemyDeath?.Invoke();
    }
}