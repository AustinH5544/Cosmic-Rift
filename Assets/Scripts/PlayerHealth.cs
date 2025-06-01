using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100; // Maximum health of the player
    private int currentHealth;  // Current health of the player
    public GameManagerMain gameManager; // Reference to GameManagerMain for game over
    public float invulnerabilityDuration = .1f; // How long the player is invulnerable after taking damage
    private bool isInvulnerable = false;

    // NEW: Audio settings for damage sound
    [Header("Audio Settings")]
    public AudioClip damageSound; // Sound to play when player takes damage
    private AudioSource audioSource; // AudioSource to play the sound

    void Start()
    {
        currentHealth = maxHealth; // Initialize health to max

        // NEW: Initialize AudioSource for damage sound
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    void Update()
    {
        if (audioSource != null)
        {
            audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }
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

                if (audioSource != null && damageSound != null)
                {
                    audioSource.PlayOneShot(damageSound);
                }

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
        isInvulnerable = true;
        Debug.Log("Player became invulnerable for " + invulnerabilityDuration + " seconds.");

        // Wait for the duration of invulnerability
        yield return new WaitForSeconds(invulnerabilityDuration);

        isInvulnerable = false; // Reset the flag
        Debug.Log("Player is no longer invulnerable.");
    }
}