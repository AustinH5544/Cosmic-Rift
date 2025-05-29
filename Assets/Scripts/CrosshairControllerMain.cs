using UnityEngine;
using TMPro; // Add for TextMeshProUGUI

public class CrosshairControllerMain : MonoBehaviour
{
    public Texture2D crosshairTexture;
    public TextMeshProUGUI scoreText; // Changed to TextMeshProUGUI
    public TextMeshProUGUI ammoText; // Changed to TextMeshProUGUI
    public AudioClip shootSound;
    public AudioClip reloadSound;

    private Color crosshairColor;
    private KeyCode shootKey;
    private Rect crosshairRect;

    private float reloadTime = 2f;
    private bool isReloading = false;
    private int maxAmmo = 12;
    private int currentAmmo;

    private int score = 0;
    private int shotsFired = 0;
    private int shotsHit = 0;

    private AudioSource audioSource;

    void Start()
    {
        int colorIndex = PlayerPrefs.GetInt("CrosshairColorIndex", 0);
        crosshairColor = colorIndex switch
        {
            0 => Color.red,
            1 => Color.green,
            2 => Color.blue,
            _ => Color.red
        };

        shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ShootKey", KeyCode.Mouse0.ToString()));

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
            crosshairTexture.Apply();
        }

        currentAmmo = maxAmmo;

        Cursor.visible = false;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        UpdateUI();
    }

    void Update()
    {
        if (Cursor.visible)
        {
            Cursor.visible = false;
        }

        Vector2 mousePosition = Input.mousePosition;
        crosshairRect = new Rect(mousePosition.x - crosshairTexture.width / 2f, Screen.height - mousePosition.y - crosshairTexture.height / 2f, crosshairTexture.width, crosshairTexture.height);

        if (Input.GetKeyDown(shootKey) && CanShoot())
        {
            Shoot();
        }
        else if (Input.GetKeyDown(KeyCode.R) || (currentAmmo == 0 && Input.GetKeyDown(shootKey)))
        {
            Reload();
        }

        if (audioSource != null)
        {
            audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        UpdateUI();
    }

    void OnGUI()
    {
        if (!CanShoot()) return;

        GUI.color = crosshairColor;
        GUI.DrawTexture(crosshairRect, crosshairTexture);
        GUI.color = Color.white;
    }

    void Shoot()
    {
        shotsFired++;
        currentAmmo--;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Target"))
            {
                shotsHit++;
                score += 10;
                Destroy(hit.collider.gameObject);
            }
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        if (currentAmmo == 0)
        {
            Reload();
        }
    }

    void Reload()
    {
        if (isReloading) return;

        isReloading = true;
        Invoke(nameof(FinishReload), reloadTime);

        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
    }

    void FinishReload()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    void OnDestroy()
    {
        if (crosshairTexture != null && crosshairTexture.name == "")
        {
            Destroy(crosshairTexture);
        }

        Cursor.visible = true;
    }

    bool CanShoot()
    {
        var coverManager = Object.FindFirstObjectByType<CoverTransitionManagerMain>();
        var coverController = Object.FindFirstObjectByType<CoverControllerMain>();

        bool inCombat = coverManager != null && coverManager.IsInCombat;
        bool notInCover = coverController != null && !coverController.IsInCover();
        bool canShootAmmo = currentAmmo > 0 && !isReloading;

        return inCombat && notInCover && canShootAmmo;
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
        if (ammoText != null)
        {
            ammoText.text = "Ammo: " + currentAmmo + "/" + maxAmmo;
        }
    }

    public int GetScore()
    {
        return score;
    }

    public float GetAccuracy()
    {
        return shotsFired > 0 ? (float)shotsHit / shotsFired * 100f : 0f;
    }

    public void ResetStats()
    {
        score = 0;
        shotsFired = 0;
        shotsHit = 0;
        currentAmmo = maxAmmo;
        if (isReloading)
        {
            CancelInvoke(nameof(FinishReload));
            isReloading = false;
        }
        UpdateUI();
    }
}