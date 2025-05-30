using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100; // Maximum health of the player
    private int currentHealth;  // Current health of the player
    public GameManagerMain gameManager; // Reference to GameManagerMain for game over

    void Start()
    {
        currentHealth = maxHealth; // Initialize health to max
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount < 0) return; // Prevent negative damage
        currentHealth = Mathf.Max(0, currentHealth - damageAmount); // Reduce health, clamp to 0
        Debug.Log($"Player took {damageAmount} damage. Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player has died!");
        if (gameManager != null)
        {
            gameManager.ShowGameOverScreen(); // Trigger game over screen
        }
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth; // Return health as a percentage (0 to 1)
    }
}
