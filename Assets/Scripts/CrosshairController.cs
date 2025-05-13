using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    public Texture2D crosshairTexture;

    private Color crosshairColor;
    private KeyCode shootKey;
    private Rect crosshairRect;

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

        if (Input.GetKeyDown(shootKey))
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
                Destroy(hit.collider.gameObject);
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
}