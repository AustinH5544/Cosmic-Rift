using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100; // Maximum health of the player
    private int currentHealth;  // Current health of the player
    public GameManagerMain gameManager; // Reference to GameManagerMain for game over
    public float invulnerabilityDuration = 2f; // How long the player is invulnerable after taking damage
    private bool isInvulnerable = false;
    void Start()
    {
        currentHealth = maxHealth; // Initialize health to max
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount < 0) return; // Prevent negative damage
       
           if (currentHealth <= 0)
            {
            Die();
            }
            else
            {
                if (!isInvulnerable)
                {  
                    currentHealth = Mathf.Max(0, currentHealth - damageAmount); // Reduce health, clamp to 0
                    UnityEngine.Debug.Log($"Player took {damageAmount} damage. Current Health: {currentHealth}");
                    StartCoroutine(BecomeTemporarilyInvulnerable());
                }
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
    private IEnumerator BecomeTemporarilyInvulnerable()
    {
        isInvulnerable = true; // Set the flag to true
        Debug.Log("Player became invulnerable for " + invulnerabilityDuration + " seconds."); // Added debug log

        // Wait for the duration of invulnerability
        yield return new WaitForSeconds(invulnerabilityDuration);

        isInvulnerable = false; // Reset the flag
        Debug.Log("Player is no longer invulnerable.");
            }
}
