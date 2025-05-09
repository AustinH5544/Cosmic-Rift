using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GUISkin guiSkin;
    private string[] crosshairColors = { "Red", "Green", "Blue" };
    private int selectedColorIndex = 0;
    private Rect windowRect = new Rect(Screen.width / 2 - 400, Screen.height / 2 - 350, 800, 700);
    private enum MenuState { MainMenu, StageSelection, Options }
    private MenuState currentState = MenuState.MainMenu;

    void Start()
    {
        selectedColorIndex = PlayerPrefs.GetInt("CrosshairColorIndex", 0);
    }

    void OnGUI()
    {
        GUI.skin = guiSkin;
        windowRect = GUI.Window(0, windowRect, DrawMenuWindow, "");
    }

    void DrawMenuWindow(int windowID)
    {
        GUILayout.BeginArea(new Rect(40, 50, 720, 600));

        switch (currentState)
        {
            case MenuState.MainMenu:
                DrawMainMenu();
                break;
            case MenuState.StageSelection:
                DrawStageSelection();
                break;
            case MenuState.Options:
                DrawOptions();
                break;
        }

        GUILayout.EndArea();
    }

    void DrawMainMenu()
    {
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Main Menu", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        if (GUILayout.Button("Play", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.StageSelection;
        }

        if (GUILayout.Button("Options", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.Options;
        }

        if (GUILayout.Button("Quit", buttonStyle, GUILayout.Height(90)))
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        GUILayout.Space(40);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 42;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Crosshair Color:", labelStyle, GUILayout.Height(50));

        GUILayout.Space(20);

        if (GUILayout.Button("Color: " + crosshairColors[selectedColorIndex], buttonStyle, GUILayout.Height(60)))
        {
            selectedColorIndex = (selectedColorIndex + 1) % crosshairColors.Length;
            PlayerPrefs.SetInt("CrosshairColorIndex", selectedColorIndex);
            PlayerPrefs.Save();
        }
    }

    void DrawStageSelection()
    {
        GUIStyle backButtonStyle = new GUIStyle(GUI.skin.button);
        backButtonStyle.fontSize = 24;
        if (GUI.Button(new Rect(10, 10, 100, 40), "Back", backButtonStyle))
        {
            currentState = MenuState.MainMenu;
        }

        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Stage Selection", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        if (GUILayout.Button("Shootout", buttonStyle, GUILayout.Height(90)))
        {
            PlayerPrefs.SetString("SelectedStage", "Shootout");
            PlayerPrefs.Save();
            SceneManager.LoadScene("Game");
        }

        if (GUILayout.Button("Story", buttonStyle, GUILayout.Height(90)))
        {
            PlayerPrefs.SetString("SelectedStage", "Story");
            PlayerPrefs.Save();
            SceneManager.LoadScene("Game");
        }

        if (GUILayout.Button("Infinite", buttonStyle, GUILayout.Height(90)))
        {
            PlayerPrefs.SetString("SelectedStage", "Infinite");
            PlayerPrefs.Save();
            SceneManager.LoadScene("Game");
        }

        if (GUILayout.Button("Add Modifiers", buttonStyle, GUILayout.Height(90)))
        {
            Debug.Log("Add Modifiers: Slow/Speed Time, One Shot, Perm Guns (TBD)");
        }
    }

    void DrawOptions()
    {
        GUIStyle backButtonStyle = new GUIStyle(GUI.skin.button);
        backButtonStyle.fontSize = 24;
        if (GUI.Button(new Rect(10, 10, 100, 40), "Back", backButtonStyle))
        {
            currentState = MenuState.MainMenu;
        }

        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Options", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        if (GUILayout.Button("Sound", buttonStyle, GUILayout.Height(90)))
        {
            Debug.Log("Sound Settings (TBD)");
        }

        if (GUILayout.Button("Controls", buttonStyle, GUILayout.Height(90)))
        {
            Debug.Log("Controls Settings (TBD)");
        }

        if (GUILayout.Button("Resolution", buttonStyle, GUILayout.Height(90)))
        {
            Debug.Log("Resolution Settings (TBD)");
        }
    }
}
