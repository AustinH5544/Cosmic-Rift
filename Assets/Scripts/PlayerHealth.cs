using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100; // Maximum health of the player
    private int currentHealth;  // Current health of the player
    public GameManagerMain gameManager; // Reference to GameManagerMain for game over

    public CoverTransitionManagerMain coverTransitionManager; // Reference to CoverTransitionManagerMain
    public CoverControllerMain coverController; // NEW: Reference to CoverControllerMain

    public float invulnerabilityDuration = .1f; // How long the player is invulnerable after taking damage
    private bool isInvulnerable = false;

    // Audio settings for damage sound
    [Header("Audio Settings")]
    public AudioClip damageSound; // Sound to play when player takes damage
    private AudioSource audioSource; // AudioSource to play the sound

    void Start()
    {
        currentHealth = maxHealth; // Initialize health to max

        // Initialize AudioSource for damage sound
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Try to find CoverTransitionManagerMain if not assigned in Inspector
        if (coverTransitionManager == null)
        {
            coverTransitionManager = FindObjectOfType<CoverTransitionManagerMain>();
            if (coverTransitionManager == null)
            {
                UnityEngine.Debug.LogError("PlayerHealth: CoverTransitionManagerMain not found. Player damage will not be disabled during transitions.");
            }
        }

        // NEW: Try to find CoverControllerMain if not assigned in Inspector
        if (coverController == null)
        {
            coverController = FindObjectOfType<CoverControllerMain>();
            if (coverController == null)
            {
                UnityEngine.Debug.LogError("PlayerHealth: CoverControllerMain not found. Player damage will not be disabled when in cover.");
            }
        }
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

        // Prevent damage if not in combat (i.e., transitioning between cover points)
        if (coverTransitionManager != null && !coverTransitionManager.IsInCombat)
        {
            UnityEngine.Debug.Log("Player is currently transitioning and cannot take damage.");
            return;
        }

        // NEW: Prevent damage if the player is currently in cover
        if (coverController != null && coverController.IsInCover())
        {
            UnityEngine.Debug.Log("Player is currently in cover and cannot take damage.");
            return;
        }

        if (!isInvulnerable)
        {
            currentHealth = Mathf.Max(0, currentHealth - damageAmount); // Reduce health, clamp to 0
            UnityEngine.Debug.Log($"Player took {damageAmount} damage. Current Health: {currentHealth}");

            if (audioSource != null && damageSound != null)
            {
                audioSource.PlayOneShot(damageSound);
            }

            // Check for death AFTER applying damage and invulnerability check
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(BecomeTemporarilyInvulnerable());
            }
        }
    }

    void Die()
    {
        UnityEngine.Debug.Log("Player has died!");
        if (gameManager != null)
        {
            gameManager.ShowGameOverScreen(); // Trigger game over screen
        }
        // Optionally, disable player input or other components here
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth; // Return health as a percentage (0 to 1)
    }

    private IEnumerator BecomeTemporarilyInvulnerable()
    {
        isInvulnerable = true;
        UnityEngine.Debug.Log("Player became invulnerable for " + invulnerabilityDuration + " seconds.");

        // Wait for the duration of invulnerability
        yield return new WaitForSeconds(invulnerabilityDuration);

        isInvulnerable = false; // Reset the flag
        UnityEngine.Debug.Log("Player is no longer invulnerable.");
    }
}
