using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Diagnostics; // This using directive is not strictly necessary for the provided code and can be removed if not used elsewhere.

public class FlyerEnemy : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100; // The maximum health of the enemy
    [HideInInspector] // Hide currentHealth from the Inspector as it's managed internally
    public int currentHealth;   // The current health of the enemy

    [Header("Hitbox Settings")]
    // Assign the Collider component that acts as this enemy's hitbox.
    public Collider hitboxCollider;

    // Optional: Reference to the spawner that created this enemy.
    // Assign this when the enemy is instantiated by the spawner.
    // public EnemySpawnerAndController spawnerReference;

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
            enabled = false; // Disable this script if essential components are missing
            return;
        }
    }

    /// <summary>
    /// Call this method to apply damage to the enemy.
    /// </summary>
    /// <param name="damageAmount">The amount of health to subtract.</param>
    public void TakeDamage(int damageAmount)
    {
        // Ensure damageAmount is not negative
        if (damageAmount < 0)
        {
            damageAmount = 0;
        }

        // If already dead, prevent further damage or death calls
        if (currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damageAmount;
        UnityEngine.Debug.Log($"{gameObject.name} took {damageAmount} damage. Current Health: {currentHealth}");

        // Check if health has dropped to or below zero.
        if (currentHealth <= 0)
        {
            // Start the death sequence
            Die();
        }
    }

    /// <summary>
    /// Handles the enemy's death sequence.
    /// </summary>
    void Die()
    {
        UnityEngine.Debug.Log($"{gameObject.name} has been defeated! Initiating despawn sequence...");

        // Prevent multiple calls to Die() if health drops below zero multiple times
        if (currentHealth <= 0 && !IsInvoking("DelayedDestroy")) // Check if already dying
        {
            // Disable the hitbox collider immediately to prevent further damage.
            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = false;
            }

            // Disable this script to stop any ongoing enemy behavior (e.g., movement).
            enabled = false;

            // Notify the EnemySpawnerAndController that this enemy has died.
            // It's crucial this happens *before* the GameObject is destroyed.
            EnemySpawnerAndController spawner = FindObjectOfType<EnemySpawnerAndController>();
            if (spawner != null)
            {
                spawner.NotifyEnemyDeath(this.gameObject);
            }
            else
            {
                UnityEngine.Debug.LogWarning("EnemySpawnerAndController not found. Cannot notify spawner of enemy death. Ensure it exists in the scene.", this);
            }

            // Start a coroutine to destroy the GameObject after a very short delay.
            // This ensures the notification has a chance to be processed.
            StartCoroutine(DelayedDestroy(0.1f)); // 0.1 seconds delay
        }
    }

    /// <summary>
    /// Coroutine to destroy the GameObject after a specified delay.
    /// </summary>
    /// <param name="delay">The time in seconds to wait before destroying the object.</param>
    IEnumerator DelayedDestroy(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified delay

        // Only destroy if the GameObject hasn't already been destroyed by another process
        if (gameObject != null)
        {
            UnityEngine.Debug.Log($"{gameObject.name}: Destroying GameObject after delay.");
            Destroy(gameObject);
        }
    }
}
