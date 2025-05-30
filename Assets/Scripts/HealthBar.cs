using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider; // Reference to the health bar Slider
    public PlayerHealth playerHealth; // Reference to the player's health

    void Start()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
        }
        if (playerHealth == null)
        {
            playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            UpdateHealthBar();
        }
    }

    void Update()
    {
        if (playerHealth != null)
        {
            UpdateHealthBar();
        }
    }

    void UpdateHealthBar()
    {
        healthSlider.value = playerHealth.GetHealthPercentage();
    }
}
