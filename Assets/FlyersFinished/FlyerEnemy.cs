using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class FlyerEnemy : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100; // The maximum health of the enemy
    private int currentHealth;  // The current health of the enemy

    [Header("Hitbox Settings")]
    // Assign the Collider component that acts as this enemy's hitbox.
    public Collider hitboxCollider;

    void Awake()
    {
        // Initialize current health to max health when the enemy is created.
        currentHealth = maxHealth;
    }

    void Start()
    {
        // --- Validation Checks ---
        if (hitboxCollider == null)
        {
            UnityEngine.Debug.LogError($"Hitbox Collider is not assigned on {gameObject.name}! Please assign a Collider in the Inspector.", this);
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Call this method to apply damage to the enemy.
    /// </summary>
    /// <param name="damageAmount">The amount of health to subtract.</param>
    public void TakeDamage(int damageAmount)
    {
        if (damageAmount < 0) damageAmount = 0;
        if (currentHealth <= 0) return; // Already dead, prevent multiple death calls

        currentHealth -= damageAmount;
        UnityEngine.Debug.Log($"{gameObject.name} took {damageAmount} damage. Current Health: {currentHealth}");

        // Check if health has dropped to or below zero.
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the enemy's death sequence, disabling its hitbox and notifying the spawner before destroying it.
    /// </summary>
    void Die()
    {
        UnityEngine.Debug.Log($"{gameObject.name} has been defeated! Despawning...");

        // Disable the hitbox collider to prevent further damage.
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Notify the EnemySpawnerAndController that this enemy has died.
        EnemySpawnerAndController spawner = FindObjectOfType<EnemySpawnerAndController>();
        if (spawner != null)
        {
            spawner.NotifyEnemyDeath(this.gameObject);
        }
        else
        {
            UnityEngine.Debug.LogWarning("EnemySpawnerAndController not found. Cannot notify spawner of enemy death.");
        }

        // Disable this script and any other movement scripts on the enemy
        // to prevent it from flying or behaving abnormally before destruction.
        enabled = false; // Disables this script

        // Immediately destroy the GameObject
        Destroy(gameObject);
    }
}
