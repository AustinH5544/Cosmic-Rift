using UnityEngine;
using TMPro;

public class CrosshairController : MonoBehaviour
{
    public Texture2D crosshairTexture;
    public TMP_Text scoreText;
    public TMP_Text ammoText;
    public TargetSpawner targetSpawner;
    public GameObject hitEffectPrefab;
    public AudioSource gunshotSound;
    public AudioSource reloadSound;

    private Color crosshairColor;
    private Color originalCrosshairColor;
    private KeyCode shootKey;
    private KeyCode reloadKey;
    private Rect crosshairRect;
    private int score = 0;
    private bool canShoot = true;
    private float flashTimer = 0f;
    private float flashDuration = 0.2f;
    private int totalShots = 0;
    private int totalHits = 0;

    private int maxAmmo = 16;
    private int currentAmmo;
    private bool isReloading = false;
    private float reloadDuration = 1.5f;
    private float reloadTimer = 0f;

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
        originalCrosshairColor = crosshairColor;

        shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ShootKey", KeyCode.Mouse0.ToString()));
        reloadKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ReloadKey", KeyCode.R.ToString())); // Load from PlayerPrefs

        if (crosshairTexture == null)
        {
            crosshairTexture = new Texture2D(10, 10);
            for (int y = 0; y < crosshairTexture.height; y++)
            {
                for (int x = 0; x < crosshairTexture.width; x++)
                {
                    crosshairTexture.SetPixel(x, y, Color.white);
                }
            }
            crosshairTexture.Apply();
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: 0";
        }

        currentAmmo = maxAmmo;
        UpdateAmmoDisplay();

        AudioSource[] audioSources = GetComponents<AudioSource>();
        if (audioSources.Length >= 1)
        {
            gunshotSound = audioSources[0];
        }
        if (audioSources.Length >= 2)
        {
            reloadSound = audioSources[1];
        }

        Cursor.visible = false;
    }

    void Update()
    {
        if (Cursor.visible)
        {
            Cursor.visible = false;
        }

        Vector2 mousePosition = Input.mousePosition;
        crosshairRect = new Rect(mousePosition.x - crosshairTexture.width / 2f, Screen.height - mousePosition.y - crosshairTexture.height / 2f, crosshairTexture.width, crosshairTexture.height);

        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                crosshairColor = originalCrosshairColor;
            }
        }

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0)
            {
                isReloading = false;
                currentAmmo = maxAmmo;
                UpdateAmmoDisplay();
            }
        }

        if (canShoot && !isReloading)
        {
            if (Input.GetKeyDown(shootKey) && currentAmmo > 0)
            {
                Shoot();
            }
            else if (Input.GetKeyDown(reloadKey) || (currentAmmo == 0 && Input.GetKeyDown(shootKey)))
            {
                Reload();
            }
        }
    }

    void OnGUI()
    {
        GUI.color = crosshairColor;
        GUI.DrawTexture(crosshairRect, crosshairTexture);
        GUI.color = Color.white;
    }

    void Shoot()
    {
        totalShots++;
        currentAmmo--;
        UpdateAmmoDisplay();

        if (gunshotSound != null)
        {
            gunshotSound.Play();
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Target"))
            {
                totalHits++;
                score += 10;
                UpdateScoreDisplay();
                if (targetSpawner != null)
                {
                    targetSpawner.OnTargetDestroyed();
                }
                if (hitEffectPrefab != null)
                {
                    GameObject effect = Instantiate(hitEffectPrefab, hit.transform.position, Quaternion.identity);
                    Destroy(effect, 1f);
                }
                crosshairColor = Color.yellow;
                flashTimer = flashDuration;
                originalCrosshairColor = crosshairColor;
                Destroy(hit.collider.gameObject);
            }
        }
    }

    void Reload()
    {
        if (currentAmmo < maxAmmo && !isReloading)
        {
            isReloading = true;
            reloadTimer = reloadDuration;
            UpdateAmmoDisplay();
            if (reloadSound != null)
            {
                reloadSound.Play();
            }
        }
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    void UpdateAmmoDisplay()
    {
        if (ammoText != null)
        {
            if (isReloading)
            {
                ammoText.text = "Reloading...";
            }
            else
            {
                ammoText.text = currentAmmo + "/" + maxAmmo;
            }
        }
    }

    void OnDestroy()
    {
        if (crosshairTexture != null && crosshairTexture.name == "")
        {
            Destroy(crosshairTexture);
        }

        Cursor.visible = true;
    }

    public void SetCanShoot(bool value)
    {
        canShoot = value;
    }

    public float GetAccuracy()
    {
        if (totalShots == 0) return 0f;
        return (float)totalHits / totalShots * 100f;
    }

    public int GetScore()
    {
        return score;
    }

    public void ResetStats()
    {
        score = 0;
        totalShots = 0;
        totalHits = 0;
        currentAmmo = maxAmmo;
        isReloading = false;
        reloadTimer = 0f;
        UpdateScoreDisplay();
        UpdateAmmoDisplay();
    }

    public void SetCrosshairColor(Color color)
    {
        crosshairColor = color;
        originalCrosshairColor = color;
    }

    public void SetReloadKey(KeyCode newKey)
    {
        reloadKey = newKey;
    }
}