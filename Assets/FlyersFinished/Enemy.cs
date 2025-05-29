using UnityEngine;
using System.Collections; // Required for IEnumerator

public class Enemy : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100; // The maximum health of the enemy
    private int currentHealth;   // The current health of the enemy

    [Header("Hitbox Settings")]
    // Assign the Collider component that acts as this enemy's hitbox.
    // This collider should typically be set as 'Is Trigger' in the Inspector.
    public Collider hitboxCollider;

    [Header("Physics Settings")]
    // Assign the Rigidbody component from the enemy GameObject.
    // This is needed to make the enemy fall when defeated.
    public Rigidbody enemyRigidbody;


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
            Debug.LogError($"Hitbox Collider is not assigned on {gameObject.name}! Please assign a Collider in the Inspector.", this);
            enabled = false;
            return;
        }

        if (enemyRigidbody == null)
        {
            Debug.LogError($"Rigidbody is not assigned on {gameObject.name}! Please assign the Rigidbody component in the Inspector.", this);
            enabled = false;
            return;
        }

        // Ensure rigidbody is kinematic and gravity is off by default for flying enemies.
        // This assumes the enemy starts in a non-falling state.
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;
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
        Debug.Log($"{gameObject.name} took {damageAmount} damage. Current Health: {currentHealth}");

        // Check if health has dropped to or below zero.
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the enemy's death sequence, making it fall and notifying the spawner.
    /// </summary>
    void Die()
    {
        Debug.Log($"{gameObject.name} has been defeated! Now falling...");

        // Disable the hitbox collider to prevent further damage.
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Make the rigidbody fall by enabling gravity and disabling kinematic.
        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = false;
            enemyRigidbody.useGravity = true;
        }

        // Notify the EnemySpawnerAndController that this enemy has died.
        EnemySpawnerAndController spawner = FindObjectOfType<EnemySpawnerAndController>();
        if (spawner != null)
        {
            spawner.NotifyEnemyDeath(this.gameObject);
        }
        else
        {
            Debug.LogWarning("EnemySpawnerAndController not found. Cannot notify spawner of enemy death.");
        }

        // Disable this script and any other movement scripts on the enemy
        // to prevent it from flying or behaving abnormally after death.
        enabled = false; // Disables this EnemyHealthAndAnimation script
        // Example for other scripts (uncomment and adjust as needed):
        // GetComponent<EnemyMovementScript>().enabled = false;
        // GetComponent<EnemyAttackScript>().enabled = false;

        // The object will now fall due to gravity.
        // If you need to destroy it after it falls off-screen or after a delay,
        // you would add that logic here (e.g., a separate script for despawning fallen enemies).
    }

    /// <summary>
    /// This method is called when another collider enters this object's trigger collider.
    /// Use this to detect projectiles or other damage sources.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    void OnTriggerEnter(Collider other)
    {
        // Example: If a GameObject with the tag "Projectile" hits the enemy.
        // Make sure your projectile has a Collider (Is Trigger) and a Rigidbody.
        if (other.CompareTag("Projectile"))
        {
            // Assuming your projectile has a script that defines its damage.
            // For demonstration, let's just apply a fixed damage amount.
            int damage = 20; // Example damage amount
            TakeDamage(damage);

            // Optionally destroy the projectile on impact.
            Destroy(other.gameObject);
        }
    }
}
