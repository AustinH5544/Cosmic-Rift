using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviour
{
    public GUISkin guiSkin;
    public AudioMixer audioMixer;
    public AudioSource backgroundMusic;
    private string[] crosshairColors = { "Red", "Green", "Blue" };
    private int selectedColorIndex = 0;
    private Rect windowRect = new Rect(Screen.width / 2 - 400, Screen.height / 2 - 350, 800, 700);
    private enum MenuState { MainMenu, StageSelection, Options, SoundSettings, ControlsSettings }
    private MenuState currentState = MenuState.MainMenu;

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private KeyCode shootKey = KeyCode.Mouse0;
    private KeyCode coverKey = KeyCode.Space;
    private KeyCode pauseKey = KeyCode.Escape;

    private bool isRebindingShoot = false;
    private bool isRebindingCover = false;
    private bool isRebindingPause = false;
    private bool isRebindingLocked = false;
    private float rebindingCooldown = 0f;

    public Texture2D crosshairTexture;
    private Color crosshairColor;
    private Rect crosshairRect;

    void Start()
    {
        selectedColorIndex = PlayerPrefs.GetInt("CrosshairColorIndex", 0);
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(masterVolume) * 20);
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20);
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20);
        }

        shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ShootKey", KeyCode.Mouse0.ToString()));
        coverKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("CoverKey", KeyCode.Space.ToString()));
        pauseKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("PauseKey", KeyCode.Escape.ToString()));

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

        UpdateCrosshairColor();

        // Play background music
        if (backgroundMusic != null)
        {
            backgroundMusic.loop = true; // Loop the music
            backgroundMusic.Play();
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

        if (rebindingCooldown > 0)
        {
            rebindingCooldown -= Time.deltaTime;
            if (rebindingCooldown <= 0)
            {
                isRebindingLocked = false;
            }
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

                        isRebindingLocked = true;
                        rebindingCooldown = 0.2f;
                    }
                    else if (keyCode == KeyCode.Escape)
                    {
                        isRebindingShoot = false;
                        isRebindingCover = false;
                        isRebindingPause = false;

                        isRebindingLocked = true;
                        rebindingCooldown = 0.2f;
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

                isRebindingLocked = true;
                rebindingCooldown = 0.2f;
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

                isRebindingLocked = true;
                rebindingCooldown = 0.2f;
            }
        }
    }

    void OnGUI()
    {
        GUI.skin = guiSkin;
        windowRect = GUI.Window(0, windowRect, DrawMenuWindow, "");

        GUI.color = crosshairColor;
        GUI.DrawTexture(crosshairRect, crosshairTexture);
        GUI.color = Color.white;
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
            case MenuState.SoundSettings:
                DrawSoundSettings();
                break;
            case MenuState.ControlsSettings:
                DrawControlsSettings();
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
            UpdateCrosshairColor();
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
            SceneManager.LoadScene("ShootingRange");
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
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(masterVolume) * 20);
        }

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
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20);
        }

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
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20);
        }

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
        if (!isRebindingLocked && GUILayout.Button(shootLabel, buttonStyle, GUILayout.Height(50)))
        {
            isRebindingShoot = true;
            isRebindingCover = false;
            isRebindingPause = false;
        }

        GUILayout.Space(20);

        string coverLabel = isRebindingCover ? "Press a key..." : $"Cover: {coverKey}";
        if (!isRebindingLocked && GUILayout.Button(coverLabel, buttonStyle, GUILayout.Height(50)))
        {
            isRebindingShoot = false;
            isRebindingCover = true;
            isRebindingPause = false;
        }

        GUILayout.Space(20);

        string pauseLabel = isRebindingPause ? "Press a key..." : $"Pause: {pauseKey}";
        if (!isRebindingLocked && GUILayout.Button(pauseLabel, buttonStyle, GUILayout.Height(50)))
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

    void UpdateCrosshairColor()
    {
        crosshairColor = selectedColorIndex switch
        {
            0 => Color.red,
            1 => Color.green,
            2 => Color.blue,
            _ => Color.red
        };
    }
}