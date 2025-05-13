using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    public Texture2D crosshairTexture; // Assign this in the Inspector with your Asset Store crosshair

    private Color crosshairColor;
    private KeyCode shootKey;
    private Rect crosshairRect;

    void Start()
    {
        // Load the crosshair color from PlayerPrefs
        int colorIndex = PlayerPrefs.GetInt("CrosshairColorIndex", 0); // Default to 0 (Red)
        crosshairColor = colorIndex switch
        {
            0 => Color.red,
            1 => Color.green,
            2 => Color.blue,
            _ => Color.red
        };

        // Load the shoot key from PlayerPrefs
        shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ShootKey", KeyCode.Mouse0.ToString()));

        // Ensure the crosshair texture is assigned
        if (crosshairTexture == null)
        {
            Debug.LogError("Crosshair texture is not assigned in the Inspector!");
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

        // Hide the cursor (we'll reinforce this in Update)
        Cursor.visible = false;
    }

    void Update()
    {
        // Reinforce hiding the cursor in case something else shows it
        if (Cursor.visible)
        {
            Cursor.visible = false;
            Debug.Log("Cursor visibility reset to false in Update.");
        }

        // Update the crosshair position to match the mouse position
        Vector2 mousePosition = Input.mousePosition;
        crosshairRect = new Rect(mousePosition.x - crosshairTexture.width / 2f, Screen.height - mousePosition.y - crosshairTexture.height / 2f, crosshairTexture.width, crosshairTexture.height);

        // Debug log to confirm crosshair position
        Debug.Log($"Crosshair Position (Game) - X: {mousePosition.x}, Y: {Screen.height - mousePosition.y}");

        // Check for shooting input
        if (Input.GetKeyDown(shootKey))
        {
            Shoot();
        }
    }

    void OnGUI()
    {
        // Draw the crosshair at its current position with the selected color tint
        GUI.color = crosshairColor;
        GUI.DrawTexture(crosshairRect, crosshairTexture);
        GUI.color = Color.white;
    }

    void Shoot()
    {
        // Raycast from the current position of the crosshair
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Target"))
            {
                Debug.Log("Hit target: " + hit.collider.gameObject.name);
                Destroy(hit.collider.gameObject);
            }
        }
    }

    void OnDestroy()
    {
        // Clean up the texture if it was created as a fallback
        if (crosshairTexture != null && crosshairTexture.name == "")
        {
            Destroy(crosshairTexture);
        }

        // Restore the cursor when leaving the game scene
        Cursor.visible = true;
    }
}