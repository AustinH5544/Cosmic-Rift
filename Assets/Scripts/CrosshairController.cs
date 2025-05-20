using UnityEngine;
using TMPro;

public class CrosshairController : MonoBehaviour
{
    public Texture2D crosshairTexture;
    public TMP_Text scoreText;
    public TargetSpawner targetSpawner;

    private Color crosshairColor;
    private KeyCode shootKey;
    private Rect crosshairRect;
    private int score = 0;
    private bool canShoot = true;

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

        if (scoreText != null)
        {
            scoreText.text = "Score: 0";
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

        if (canShoot && Input.GetKeyDown(shootKey))
        {
            Shoot();
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Target"))
            {
                score += 10;
                UpdateScoreDisplay();
                if (targetSpawner != null)
                {
                    targetSpawner.OnTargetDestroyed();
                }
                Destroy(hit.collider.gameObject);
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
}