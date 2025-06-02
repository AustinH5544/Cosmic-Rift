using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GUISkin guiSkin;
    public CrosshairController crosshairController;
    public Timer timer;
    public TargetSpawner targetSpawner;

    private Rect windowRect = new Rect(Screen.width / 2 - 500, Screen.height / 2 - 500, 1000, 1000);
    private enum MenuState { Welcome, None, GameOver, Pause, Options, SoundSettings, ControlsSettings, Leaderboard }
    private MenuState currentState = MenuState.Welcome;

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private KeyCode shootKey = KeyCode.Mouse0;
    private KeyCode coverKey = KeyCode.Space;
    private KeyCode pauseKey = KeyCode.Escape;
    private KeyCode reloadKey = KeyCode.R;

    private bool isRebindingShoot = false;
    private bool isRebindingCover = false;
    private bool isRebindingPause = false;
    private bool isRebindingReload = false;
    private bool isRebindingLocked = false;
    private float rebindingCooldown = 0f;
    private bool justCancelledRebinding = false; // New flag to track cancellation frame

    private int[] highScores = new int[5];
    private float[] highAccuracies = new float[5];
    private string[] playerNames = new string[5];
    private string currentPlayerName = "Player";
    private bool nameSubmitted = false;

    private string[] crosshairColors = { "Red", "Green", "Blue" };
    private int selectedColorIndex;

    void Start()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ShootKey", KeyCode.Mouse0.ToString()));
        coverKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("CoverKey", KeyCode.Space.ToString()));
        pauseKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("PauseKey", KeyCode.Escape.ToString()));
        reloadKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ReloadKey", KeyCode.R.ToString()));

        for (int i = 0; i < 5; i++)
        {
            highScores[i] = PlayerPrefs.GetInt("HighScore" + i, 0);
            highAccuracies[i] = PlayerPrefs.GetFloat("HighAccuracy" + i, 0f);
            playerNames[i] = PlayerPrefs.GetString("PlayerName" + i, "-");
        }

        selectedColorIndex = PlayerPrefs.GetInt("CrosshairColorIndex", 0);
        UpdateCrosshairColor();

        if (crosshairController != null)
        {
            crosshairController.SetCanShoot(false);
            crosshairController.SetReloadKey(reloadKey);
        }
        else
        {
            Debug.LogWarning("CrosshairController reference is not assigned in GameManager!");
        }

        if (timer != null)
        {
            timer.enabled = false;
        }
        else
        {
            Debug.LogWarning("Timer reference is not assigned in GameManager!");
        }

        if (targetSpawner != null)
        {
            targetSpawner.enabled = false;
        }
        else
        {
            Debug.LogWarning("TargetSpawner reference is not assigned in GameManager!");
        }
    }

    void Update()
    {
        // Prevent Pause menu from opening during rebinding or on the frame rebinding is cancelled
        if (currentState == MenuState.None && Input.GetKeyDown(pauseKey) && !IsRebinding() && !justCancelledRebinding)
        {
            Debug.Log("Showing Pause menu");
            ShowPauseMenu();
        }

        if (rebindingCooldown > 0)
        {
            rebindingCooldown -= Time.deltaTime;
            if (rebindingCooldown <= 0)
            {
                isRebindingLocked = false;
                justCancelledRebinding = false; // Reset cancellation flag after cooldown
            }
        }

        if (IsRebinding())
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
                            Debug.Log($"Shoot key rebound to: {shootKey}");
                        }
                        else if (isRebindingCover)
                        {
                            coverKey = keyCode;
                            isRebindingCover = false;
                            Debug.Log($"Cover key rebound to: {coverKey}");
                        }
                        else if (isRebindingPause)
                        {
                            pauseKey = keyCode;
                            isRebindingPause = false;
                            Debug.Log($"Pause key rebound to: {pauseKey}");
                        }
                        else if (isRebindingReload)
                        {
                            reloadKey = keyCode;
                            isRebindingReload = false;
                            if (crosshairController != null)
                            {
                                crosshairController.SetReloadKey(reloadKey);
                                Debug.Log($"Reload key rebound to: {reloadKey}");
                            }
                            else
                            {
                                Debug.LogWarning("CrosshairController reference is null during reload key rebinding!");
                            }
                        }

                        PlayerPrefs.SetString("ShootKey", shootKey.ToString());
                        PlayerPrefs.SetString("CoverKey", coverKey.ToString());
                        PlayerPrefs.SetString("PauseKey", pauseKey.ToString());
                        PlayerPrefs.SetString("ReloadKey", reloadKey.ToString());
                        PlayerPrefs.Save();

                        isRebindingLocked = true;
                        rebindingCooldown = 0.2f;
                    }
                    else if (keyCode == KeyCode.Escape)
                    {
                        isRebindingShoot = false;
                        isRebindingCover = false;
                        isRebindingPause = false;
                        isRebindingReload = false;

                        isRebindingLocked = true;
                        rebindingCooldown = 0.2f;
                        justCancelledRebinding = true; // Set flag to prevent Pause menu on this frame
                        Debug.Log("Rebinding cancelled with Escape key.");
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (isRebindingShoot)
                {
                    shootKey = KeyCode.Mouse0;
                    isRebindingShoot = false;
                    Debug.Log($"Shoot key rebound to: {shootKey}");
                }
                else if (isRebindingCover)
                {
                    coverKey = KeyCode.Mouse0;
                    isRebindingCover = false;
                    Debug.Log($"Cover key rebound to: {coverKey}");
                }
                else if (isRebindingPause)
                {
                    pauseKey = KeyCode.Mouse0;
                    isRebindingPause = false;
                    Debug.Log($"Pause key rebound to: {pauseKey}");
                }
                else if (isRebindingReload)
                {
                    reloadKey = KeyCode.Mouse0;
                    isRebindingReload = false;
                    if (crosshairController != null)
                    {
                        crosshairController.SetReloadKey(reloadKey);
                        Debug.Log($"Reload key rebound to: {reloadKey}");
                    }
                    else
                    {
                        Debug.LogWarning("CrosshairController reference is null during reload key rebinding!");
                    }
                }

                PlayerPrefs.SetString("ShootKey", shootKey.ToString());
                PlayerPrefs.SetString("CoverKey", coverKey.ToString());
                PlayerPrefs.SetString("PauseKey", pauseKey.ToString());
                PlayerPrefs.SetString("ReloadKey", reloadKey.ToString());
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
                    Debug.Log($"Shoot key rebound to: {shootKey}");
                }
                else if (isRebindingCover)
                {
                    coverKey = KeyCode.Mouse1;
                    isRebindingCover = false;
                    Debug.Log($"Cover key rebound to: {coverKey}");
                }
                else if (isRebindingPause)
                {
                    pauseKey = KeyCode.Mouse1;
                    isRebindingPause = false;
                    Debug.Log($"Pause key rebound to: {pauseKey}");
                }
                else if (isRebindingReload)
                {
                    reloadKey = KeyCode.Mouse1;
                    isRebindingReload = false;
                    if (crosshairController != null)
                    {
                        crosshairController.SetReloadKey(reloadKey);
                        Debug.Log($"Reload key rebound to: {reloadKey}");
                    }
                    else
                    {
                        Debug.LogWarning("CrosshairController reference is null during reload key rebinding!");
                    }
                }

                PlayerPrefs.SetString("ShootKey", shootKey.ToString());
                PlayerPrefs.SetString("CoverKey", coverKey.ToString());
                PlayerPrefs.SetString("PauseKey", pauseKey.ToString());
                PlayerPrefs.SetString("ReloadKey", reloadKey.ToString());
                PlayerPrefs.Save();

                isRebindingLocked = true;
                rebindingCooldown = 0.2f;
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
        // Adjusted BeginArea to fit content with even padding
        GUILayout.BeginArea(new Rect(50, 50, 900, 900));

        switch (currentState)
        {
            case MenuState.Welcome:
                DrawWelcomeScreen();
                break;
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
            case MenuState.Leaderboard:
                DrawLeaderboard();
                break;
        }

        GUILayout.EndArea();
    }

    void DrawWelcomeScreen()
    {
        GUILayout.Space(30);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.wordWrap = true;
        GUILayout.Label("Welcome to the Shooting Range", titleStyle, GUILayout.Height(140));

        GUIStyle messageStyle = new GUIStyle(GUI.skin.label);
        messageStyle.fontSize = 36;
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.wordWrap = true;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 36;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 36;

        GUILayout.Space(50);

        GUILayout.Label("Shoot as many targets as possible in 60 seconds", messageStyle, GUILayout.Height(120));

        GUILayout.Space(30);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Crosshair Color:", labelStyle, GUILayout.Height(50));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Color: " + crosshairColors[selectedColorIndex], buttonStyle, GUILayout.Width(600), GUILayout.Height(100)))
        {
            selectedColorIndex = (selectedColorIndex + 1) % crosshairColors.Length;
            PlayerPrefs.SetInt("CrosshairColorIndex", selectedColorIndex);
            PlayerPrefs.Save();
            UpdateCrosshairColor();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(50);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Start", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            currentState = MenuState.None;
            if (crosshairController != null)
            {
                crosshairController.SetCanShoot(true);
            }
            if (timer != null)
            {
                timer.enabled = true;
            }
            if (targetSpawner != null)
            {
                targetSpawner.enabled = true;
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            SceneManager.LoadScene("MainMenu");
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void DrawGameOverMenu()
    {
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Game Over", titleStyle, GUILayout.Height(80));

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 48;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.fontSize = 48;
        textFieldStyle.alignment = TextAnchor.MiddleCenter;
        textFieldStyle.padding = new RectOffset(10, 10, 10, 10);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 36;

        if (!nameSubmitted)
        {
            GUILayout.Space(60);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Enter Your Name:", labelStyle, GUILayout.Height(70));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            currentPlayerName = GUILayout.TextField(currentPlayerName, 10, textFieldStyle, GUILayout.Height(90), GUILayout.Width(400));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(60);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Submit", buttonStyle, GUILayout.Height(90), GUILayout.Width(300)))
            {
                nameSubmitted = true;
                UpdateLeaderboard();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Main Menu", buttonStyle, GUILayout.Height(90)))
            {
                nameSubmitted = false;
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }

            if (GUILayout.Button("Restart", buttonStyle, GUILayout.Height(90)))
            {
                if (crosshairController != null)
                {
                    crosshairController.ResetStats();
                }
                nameSubmitted = false;
                Time.timeScale = 1f;
                SceneManager.LoadScene("ShootingRange");
            }

            if (GUILayout.Button("Options", buttonStyle, GUILayout.Height(90)))
            {
                currentState = MenuState.Options;
            }

            if (GUILayout.Button("Leaderboard", buttonStyle, GUILayout.Height(90)))
            {
                currentState = MenuState.Leaderboard;
            }
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
        buttonStyle.fontSize = 36;

        GUILayout.Space(50);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Resume", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            Time.timeScale = 1f;
            if (crosshairController != null)
            {
                crosshairController.SetCanShoot(true);
            }
            currentState = MenuState.None;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Main Menu", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Restart", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            if (crosshairController != null)
            {
                crosshairController.ResetStats();
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene("ShootingRange");
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Options", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            currentState = MenuState.Options;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            Time.timeScale = 1f;
            if (crosshairController != null)
            {
                crosshairController.SetCanShoot(false);
            }
            if (timer != null)
            {
                timer.enabled = false;
            }
            if (targetSpawner != null)
            {
                targetSpawner.enabled = false;
            }
            currentState = MenuState.Welcome;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void DrawOptionsMenu()
    {
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Options", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 36;

        GUILayout.Space(50);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Sound", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            currentState = MenuState.SoundSettings;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Controls", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            currentState = MenuState.ControlsSettings;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Resolution", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            currentState = (Time.timeScale == 0) ? MenuState.Pause : MenuState.GameOver;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void DrawSoundSettings()
    {
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
        buttonStyle.fontSize = 36;

        GUILayout.Space(50);

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

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Save", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            currentState = MenuState.Options;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void DrawControlsSettings()
    {
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Controls", titleStyle, GUILayout.Height(80));

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 36;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 36;
        buttonStyle.padding = new RectOffset(20, 20, 15, 15);

        GUILayout.Space(40);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        string shootLabel = isRebindingShoot ? "Press a key..." : $"Shoot: {shootKey}";
        if (!isRebindingLocked && GUILayout.Button(shootLabel, buttonStyle, GUILayout.Width(600), GUILayout.Height(100)))
        {
            isRebindingShoot = true;
            isRebindingCover = false;
            isRebindingPause = false;
            isRebindingReload = false;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        string coverLabel = isRebindingCover ? "Press a key..." : $"Cover: {coverKey}";
        if (!isRebindingLocked && GUILayout.Button(coverLabel, buttonStyle, GUILayout.Width(600), GUILayout.Height(100)))
        {
            isRebindingShoot = false;
            isRebindingCover = true;
            isRebindingPause = false;
            isRebindingReload = false;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        string pauseLabel = isRebindingPause ? "Press a key..." : $"Pause: {pauseKey}";
        if (!isRebindingLocked && GUILayout.Button(pauseLabel, buttonStyle, GUILayout.Width(600), GUILayout.Height(100)))
        {
            isRebindingShoot = false;
            isRebindingCover = false;
            isRebindingPause = true;
            isRebindingReload = false;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        string reloadLabel = isRebindingReload ? "Press a key..." : $"Reload: {reloadKey}";
        if (!isRebindingLocked && GUILayout.Button(reloadLabel, buttonStyle, GUILayout.Width(600), GUILayout.Height(100)))
        {
            isRebindingShoot = false;
            isRebindingCover = false;
            isRebindingPause = false;
            isRebindingReload = true;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(40);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Reset to Default", buttonStyle, GUILayout.Width(600), GUILayout.Height(100)))
        {
            shootKey = KeyCode.Mouse0;
            coverKey = KeyCode.Space;
            pauseKey = KeyCode.Escape;
            reloadKey = KeyCode.R;
            if (crosshairController != null)
            {
                crosshairController.SetReloadKey(reloadKey);
            }
            PlayerPrefs.SetString("ShootKey", shootKey.ToString());
            PlayerPrefs.SetString("CoverKey", coverKey.ToString());
            PlayerPrefs.SetString("PauseKey", pauseKey.ToString());
            PlayerPrefs.SetString("ReloadKey", reloadKey.ToString());
            PlayerPrefs.Save();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", buttonStyle, GUILayout.Width(600), GUILayout.Height(100)))
        {
            currentState = MenuState.Options;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void DrawLeaderboard()
    {
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 64;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Leaderboard", titleStyle, GUILayout.Height(100));

        GUIStyle entryStyle = new GUIStyle(GUI.skin.label);
        entryStyle.fontSize = 36;
        entryStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 36;

        GUILayout.Space(50);

        for (int i = 0; i < 5; i++)
        {
            string rank = (i + 1) + ". ";
            string nameEntry = playerNames[i] != "-" ? playerNames[i] : "-";
            string scoreEntry = highScores[i] > 0 ? highScores[i].ToString() : "-";
            string accuracyEntry = highAccuracies[i] > 0 ? highAccuracies[i].ToString("F1") + "%" : "-";
            GUILayout.Label($"{rank} {nameEntry} | Score: {scoreEntry} | Accuracy: {accuracyEntry}", entryStyle, GUILayout.Height(70));
        }

        GUILayout.Space(40);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", buttonStyle, GUILayout.Width(600), GUILayout.Height(130)))
        {
            currentState = MenuState.GameOver;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void UpdateLeaderboard()
    {
        if (crosshairController != null)
        {
            int currentScore = crosshairController.GetScore();
            float currentAccuracy = crosshairController.GetAccuracy();

            for (int i = 0; i < 5; i++)
            {
                if (currentScore > highScores[i])
                {
                    for (int j = 4; j > i; j--)
                    {
                        highScores[j] = highScores[j - 1];
                        highAccuracies[j] = highAccuracies[j - 1];
                        playerNames[j] = playerNames[j - 1];
                    }
                    highScores[i] = currentScore;
                    highAccuracies[i] = currentAccuracy;
                    playerNames[i] = currentPlayerName;
                    break;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                PlayerPrefs.SetInt("HighScore" + i, highScores[i]);
                PlayerPrefs.SetFloat("HighAccuracy" + i, highAccuracies[i]);
                PlayerPrefs.SetString("PlayerName" + i, playerNames[i]);
            }
            PlayerPrefs.Save();
        }
    }

    void UpdateCrosshairColor()
    {
        if (crosshairController != null)
        {
            Color newColor = selectedColorIndex switch
            {
                0 => Color.red,
                1 => Color.green,
                2 => Color.blue,
                _ => Color.red
            };
            crosshairController.SetCrosshairColor(newColor);
        }
        else
        {
            Debug.LogWarning("CrosshairController reference is not assigned in GameManager!");
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

    private bool IsRebinding()
    {
        return isRebindingShoot || isRebindingCover || isRebindingPause || isRebindingReload;
    }
}