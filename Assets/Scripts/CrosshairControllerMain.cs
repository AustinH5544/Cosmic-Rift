using UnityEngine;
using TMPro;
using System.Diagnostics;
using System;

public class CrosshairControllerMain : MonoBehaviour
{
    public Texture2D crosshairTexture;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI ammoText;
    public AudioClip shootSound;
    public AudioClip reloadSound;

    private Color crosshairColor;
    private KeyCode shootKey;
    private Rect crosshairRect;

    private float reloadTime = 2f;
    private bool isReloading = false;
    private float reloadStartTime;
    private int maxAmmo = 12;
    private int currentAmmo;

    private int score = 0;
    private int shotsFired = 0;
    private int shotsHit = 0;

    private AudioSource audioSource;

    public LayerMask shootableLayers;

    private GameManagerMain gameManager;

    void Start()
    {
        // Get crosshair color from PlayerPrefs, default to red
        int colorIndex = PlayerPrefs.GetInt("CrosshairColorIndex", 0);
        crosshairColor = colorIndex switch
        {
            0 => Color.red,
            1 => Color.green,
            2 => Color.blue,
            _ => Color.red
        };

        // Get shoot key from PlayerPrefs, default to Mouse0 (left click)
        shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ShootKey", KeyCode.Mouse0.ToString()));

        // If no crosshair texture is assigned, create a default one
        if (crosshairTexture == null)
        {
            crosshairTexture = new Texture2D(10, 10);
            for (int y = 0; y < crosshairTexture.height; y++)
            {
                for (int x = 0; x < crosshairTexture.width; x++)
                {
                    crosshairTexture.SetPixel(x, y, crosshairColor);
                }
            }
            crosshairTexture.Apply(); // Apply pixel changes to the texture
        }

        currentAmmo = maxAmmo; // Initialize current ammo to max ammo

        Cursor.visible = false; // Hide the default system cursor

        // Add an AudioSource component to this GameObject for playing sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        // Set audio volume from PlayerPrefs, default to 1f
        audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Find the GameManagerMain instance in the scene
        gameManager = FindObjectOfType<GameManagerMain>();
        if (gameManager == null)
        {
            UnityEngine.Debug.LogError("CrosshairControllerMain: GameManagerMain not found in the scene!");
        }

        UpdateUI(); // Initial UI update
    }

    void Update()
    {
        // Ensure the cursor remains hidden during gameplay
        if (Cursor.visible)
        {
            Cursor.visible = false;
        }

        // Calculate the crosshair position based on mouse input
        Vector2 mousePosition = Input.mousePosition;
        crosshairRect = new Rect(mousePosition.x - crosshairTexture.width / 2f, Screen.height - mousePosition.y - crosshairTexture.height / 2f, crosshairTexture.width, crosshairTexture.height);

        // If the player cannot shoot (e.g., game paused, game over, or not in combat)
        if (!CanShoot())
        {
            // Check if reloading is complete
            if (isReloading && Time.time >= reloadStartTime + reloadTime)
            {
                FinishReload();
            }
            return; // Exit Update early if shooting is not allowed
        }

        // Handle shooting input
        if (Input.GetKeyDown(shootKey) && CanShoot())
        {
            Shoot();
        }
        // Handle reload input (R key or shooting when ammo is 0)
        else if (Input.GetKeyDown(KeyCode.R) && currentAmmo != maxAmmo || (currentAmmo == 0 && Input.GetKeyDown(shootKey)))
        {
            Reload();
        }

        // Update audio source volume based on PlayerPrefs
        if (audioSource != null)
        {
            audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        UpdateUI(); // Update UI elements every frame
    }

    // Called for rendering and handling GUI events
    void OnGUI()
    {
        // Only draw the crosshair if it's allowed to be shown
        if (!CanShowCrosshair()) return;

        GUI.color = crosshairColor; // Set GUI color to crosshair color
        GUI.DrawTexture(crosshairRect, crosshairTexture); // Draw the crosshair texture
        GUI.color = Color.white; // Reset GUI color to white
    }

    // Handles the shooting logic
    void Shoot()
    {
        shotsFired++; // Increment shots fired count
        currentAmmo--; // Decrease current ammo

        // Create a ray from the camera through the mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Perform a raycast to detect hits on shootable layers
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, shootableLayers))
        {
            // Check if the hit object has the "Target" tag
            if (hit.collider.CompareTag("Target"))
            {
                // Try to get the FlyerEnemy component
                FlyerEnemy enemy = hit.collider.GetComponent<FlyerEnemy>();

                if (enemy != null)
                {
                    shotsHit++; // Increment shots hit count
                    score += 10; // Increase score
                    int damageToDeal = 20;
                    enemy.TakeDamage(damageToDeal); // Deal damage to the enemy
                }
                else
                {
                    // If it's a target but not a FlyerEnemy (e.g., a static target)
                    shotsHit++; // Increment shots hit count
                    score += 10; // Increase score
                    Destroy(hit.collider.gameObject); // Destroy the hit object
                }
            }
        }

        // Play the shoot sound if available
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // If ammo runs out, automatically initiate reload
        if (currentAmmo == 0)
        {
            Reload();
        }
    }

    // Initiates the reload process
    void Reload()
    {
        if (isReloading) return; // Prevent multiple reloads

        isReloading = true; // Set reloading flag to true
        reloadStartTime = Time.time; // Record reload start time
        // Schedule FinishReload to be called after reloadTime seconds
        Invoke(nameof(FinishReload), reloadTime);

        // Play the reload sound if available
        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
    }

    // Completes the reload process
    void FinishReload()
    {
        currentAmmo = maxAmmo; // Restore ammo to max
        isReloading = false; // Set reloading flag to false
    }

    // Called when the GameObject is destroyed
    void OnDestroy()
    {
        // Destroy the dynamically created crosshair texture if it's not an asset
        if (crosshairTexture != null && crosshairTexture.name == "")
        {
            Destroy(crosshairTexture);
        }

        Cursor.visible = true; // Make the system cursor visible again
    }

    // Determines if the player is currently allowed to shoot
    bool CanShoot()
    {
        if (gameManager != null)
        {
            // Cannot shoot if the game is paused or over
            if (gameManager.IsPaused() || gameManager.IsGameOver())
            {
                return false;
            }
        }

        // Check if the player is in combat via CoverTransitionManagerMain
        var coverManager = UnityEngine.Object.FindFirstObjectByType<CoverTransitionManagerMain>();
        bool inCombat = coverManager != null && coverManager.IsInCombat;

        // Check if there's ammo and not currently reloading
        bool canShootAmmo = currentAmmo > 0 && !isReloading;

        // Can shoot only if in combat AND has ammo/not reloading
        return inCombat && canShootAmmo;
    }

    // Determines if the crosshair should be displayed
    bool CanShowCrosshair()
    {
        // Always show the crosshair during pause or game over menus to allow clicking buttons
        if (gameManager != null)
        {
            if (gameManager.IsPaused() || gameManager.IsGameOver())
            {
                return true;
            }
        }

        // Hide the crosshair while reloading during gameplay
        if (isReloading)
        {
            return false;
        }

        // Show the crosshair in all other cases (e.g., normal gameplay, out of ammo but not reloading)
        return true;
    }

    // Updates the UI elements (score and ammo)
    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score; // Update score display
        }
        if (ammoText != null)
        {
            // If reloading, display "Reloading...", otherwise display current ammo
            if (isReloading)
            {
                ammoText.text = "Reloading...";
            }
            else
            {
                ammoText.text = currentAmmo + "/" + maxAmmo; // Update ammo display
            }
        }
    }

    // Public method to get the current score
    public int GetScore()
    {
        return score;
    }

    // Public method to calculate and get accuracy
    public float GetAccuracy()
    {
        return shotsFired > 0 ? (float)shotsHit / shotsFired * 100f : 0f;
    }

    // Resets game statistics
    public void ResetStats()
    {
        score = 0;
        shotsFired = 0;
        shotsHit = 0;
        currentAmmo = maxAmmo;
        // If currently reloading, cancel the invoke and reset the flag
        if (isReloading)
        {
            CancelInvoke(nameof(FinishReload));
            isReloading = false;
        }
        UpdateUI(); // Update UI after resetting stats
    }
}
