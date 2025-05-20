using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GUISkin guiSkin;
    public CrosshairController crosshairController;

    private Rect windowRect = new Rect(Screen.width / 2 - 400, Screen.height / 2 - 350, 800, 700);
    private enum MenuState { None, GameOver, Pause, Options, SoundSettings, ControlsSettings }
    private MenuState currentState = MenuState.None;

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private KeyCode shootKey = KeyCode.Mouse0;
    private KeyCode coverKey = KeyCode.Space;
    private KeyCode pauseKey = KeyCode.Escape;

    private bool isRebindingShoot = false;
    private bool isRebindingCover = false;
    private bool isRebindingPause = false;

    void Start()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ShootKey", KeyCode.Mouse0.ToString()));
        coverKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("CoverKey", KeyCode.Space.ToString()));
        pauseKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("PauseKey", KeyCode.Escape.ToString()));
    }

    void Update()
    {
        if (currentState == MenuState.None && Input.GetKeyDown(KeyCode.Escape))
        {
            ShowPauseMenu();
        }

        if (isRebindingShoot || isRebindingCover || isRebindingPause)
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode != KeyCode.None && keyCode != KeyCode.Escape)
                    {
                        if (isRebindingShoot)
                        {
                            shootKey = keyCode;
                            isRebindingShoot = false;
                        }
                        else if (isRebindingCover)
                        {
                            coverKey = keyCode;
                            isRebindingCover = false;
                        }
                        else if (isRebindingPause)
                        {
                            pauseKey = keyCode;
                            isRebindingPause = false;
                        }

                        PlayerPrefs.SetString("ShootKey", shootKey.ToString());
                        PlayerPrefs.SetString("CoverKey", coverKey.ToString());
                        PlayerPrefs.SetString("PauseKey", pauseKey.ToString());
                        PlayerPrefs.Save();
                    }
                    else if (keyCode == KeyCode.Escape)
                    {
                        isRebindingShoot = false;
                        isRebindingCover = false;
                        isRebindingPause = false;
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (isRebindingShoot)
                {
                    shootKey = KeyCode.Mouse0;
                    isRebindingShoot = false;
                }
                else if (isRebindingCover)
                {
                    coverKey = KeyCode.Mouse0;
                    isRebindingCover = false;
                }
                else if (isRebindingPause)
                {
                    pauseKey = KeyCode.Mouse0;
                    isRebindingPause = false;
                }

                PlayerPrefs.SetString("ShootKey", shootKey.ToString());
                PlayerPrefs.SetString("CoverKey", coverKey.ToString());
                PlayerPrefs.SetString("PauseKey", pauseKey.ToString());
                PlayerPrefs.Save();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (isRebindingShoot)
                {
                    shootKey = KeyCode.Mouse1;
                    isRebindingShoot = false;
                }
                else if (isRebindingCover)
                {
                    coverKey = KeyCode.Mouse1;
                    isRebindingCover = false;
                }
                else if (isRebindingPause)
                {
                    pauseKey = KeyCode.Mouse1;
                    isRebindingPause = false;
                }

                PlayerPrefs.SetString("ShootKey", shootKey.ToString());
                PlayerPrefs.SetString("CoverKey", coverKey.ToString());
                PlayerPrefs.SetString("PauseKey", pauseKey.ToString());
                PlayerPrefs.Save();
            }
        }
    }

    void OnGUI()
    {
        if (currentState != MenuState.None)
        {
            GUI.skin = guiSkin;
            windowRect = GUI.Window(0, windowRect, DrawMenuWindow, "");
        }
    }

    void DrawMenuWindow(int windowID)
    {
        GUILayout.BeginArea(new Rect(40, 50, 720, 600));

        switch (currentState)
        {
            case MenuState.GameOver:
                DrawGameOverMenu();
                break;
            case MenuState.Pause:
                DrawPauseMenu();
                break;
            case MenuState.Options:
                DrawOptionsMenu();
                break;
            case MenuState.SoundSettings:
                DrawSoundSettings();
                break;
            case MenuState.ControlsSettings:
                DrawControlsSettings();
                break;
        }

        GUILayout.EndArea();
    }

    void DrawGameOverMenu()
    {
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Game Over", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        if (GUILayout.Button("Main Menu", buttonStyle, GUILayout.Height(90)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        if (GUILayout.Button("Restart", buttonStyle, GUILayout.Height(90)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("ShootingRange");
        }

        if (GUILayout.Button("Options", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.Options;
        }
    }

    void DrawPauseMenu()
    {
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Paused", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        if (GUILayout.Button("Resume", buttonStyle, GUILayout.Height(90)))
        {
            Time.timeScale = 1f;
            if (crosshairController != null)
            {
                crosshairController.SetCanShoot(true);
            }
            currentState = MenuState.None;
        }

        if (GUILayout.Button("Main Menu", buttonStyle, GUILayout.Height(90)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        if (GUILayout.Button("Restart", buttonStyle, GUILayout.Height(90)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("ShootingRange");
        }

        if (GUILayout.Button("Options", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.Options;
        }
    }

    void DrawOptionsMenu()
    {
        GUIStyle backButtonStyle = new GUIStyle(GUI.skin.button);
        backButtonStyle.fontSize = 24;
        if (GUI.Button(new Rect(10, 10, 100, 40), "Back", backButtonStyle))
        {
            currentState = (Time.timeScale == 0) ? MenuState.Pause : MenuState.GameOver;
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
            currentState = MenuState.SoundSettings;
        }

        if (GUILayout.Button("Controls", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.ControlsSettings;
        }

        if (GUILayout.Button("Resolution", buttonStyle, GUILayout.Height(90)))
        {
        }
    }

    void DrawSoundSettings()
    {
        GUIStyle backButtonStyle = new GUIStyle(GUI.skin.button);
        backButtonStyle.fontSize = 24;
        if (GUI.Button(new Rect(10, 10, 100, 40), "Back", backButtonStyle))
        {
            currentState = MenuState.Options;
        }

        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Sound Settings", titleStyle, GUILayout.Height(80));

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 36;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle percentageStyle = new GUIStyle(GUI.skin.label);
        percentageStyle.fontSize = 28;
        percentageStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        GUILayout.Space(20);

        GUILayout.Label("Master Volume", labelStyle, GUILayout.Height(50));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        masterVolume = GUILayout.HorizontalSlider(masterVolume, 0f, 1f, GUILayout.Width(500), GUILayout.Height(30));
        GUILayout.Label(((int)(masterVolume * 100)).ToString() + "%", percentageStyle, GUILayout.Width(100), GUILayout.Height(30));
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.Label("Music Volume", labelStyle, GUILayout.Height(50));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        musicVolume = GUILayout.HorizontalSlider(musicVolume, 0f, 1f, GUILayout.Width(500), GUILayout.Height(30));
        GUILayout.Label(((int)(musicVolume * 100)).ToString() + "%", percentageStyle, GUILayout.Width(100), GUILayout.Height(30));
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.Label("SFX Volume", labelStyle, GUILayout.Height(50));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        sfxVolume = GUILayout.HorizontalSlider(sfxVolume, 0f, 1f, GUILayout.Width(500), GUILayout.Height(30));
        GUILayout.Label(((int)(sfxVolume * 100)).ToString() + "%", percentageStyle, GUILayout.Width(100), GUILayout.Height(30));
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(40);

        if (GUILayout.Button("Save", buttonStyle, GUILayout.Height(60)))
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }
    }

    void DrawControlsSettings()
    {
        GUIStyle backButtonStyle = new GUIStyle(GUI.skin.button);
        backButtonStyle.fontSize = 24;
        if (GUI.Button(new Rect(10, 10, 100, 40), "Back", backButtonStyle))
        {
            currentState = MenuState.Options;
        }

        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Controls", titleStyle, GUILayout.Height(80));

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 36;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        GUILayout.Space(20);

        string shootLabel = isRebindingShoot ? "Press a key..." : $"Shoot: {shootKey}";
        if (GUILayout.Button(shootLabel, buttonStyle, GUILayout.Height(50)))
        {
            isRebindingShoot = true;
            isRebindingCover = false;
            isRebindingPause = false;
        }

        GUILayout.Space(20);

        string coverLabel = isRebindingCover ? "Press a key..." : $"Cover: {coverKey}";
        if (GUILayout.Button(coverLabel, buttonStyle, GUILayout.Height(50)))
        {
            isRebindingShoot = false;
            isRebindingCover = true;
            isRebindingPause = false;
        }

        GUILayout.Space(20);

        string pauseLabel = isRebindingPause ? "Press a key..." : $"Pause: {pauseKey}";
        if (GUILayout.Button(pauseLabel, buttonStyle, GUILayout.Height(50)))
        {
            isRebindingShoot = false;
            isRebindingCover = false;
            isRebindingPause = true;
        }

        GUILayout.Space(40);

        if (GUILayout.Button("Reset to Default", buttonStyle, GUILayout.Height(60)))
        {
            shootKey = KeyCode.Mouse0;
            coverKey = KeyCode.Space;
            pauseKey = KeyCode.Escape;
            PlayerPrefs.SetString("ShootKey", shootKey.ToString());
            PlayerPrefs.SetString("CoverKey", coverKey.ToString());
            PlayerPrefs.SetString("PauseKey", pauseKey.ToString());
            PlayerPrefs.Save();
        }
    }

    public void ShowGameOverScreen()
    {
        Time.timeScale = 0f;
        if (crosshairController != null)
        {
            crosshairController.SetCanShoot(false);
        }
        currentState = MenuState.GameOver;
    }

    public void ShowPauseMenu()
    {
        Time.timeScale = 0f;
        if (crosshairController != null)
        {
            crosshairController.SetCanShoot(false);
        }
        currentState = MenuState.Pause;
    }
}