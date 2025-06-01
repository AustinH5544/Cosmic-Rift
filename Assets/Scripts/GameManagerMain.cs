using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerMain : MonoBehaviour
{
    public GUISkin guiSkin;
    public CrosshairControllerMain crosshairController;

    private Rect windowRect = new Rect(Screen.width / 2 - 500, Screen.height / 2 - 500, 1000, 1000);
    private enum MenuState { None, GameOver, Pause, Options, SoundSettings, ControlsSettings, Leaderboard }
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

    private int[] highScores = new int[5];
    private float[] highAccuracies = new float[5];
    private string[] playerNames = new string[5];
    private string currentPlayerName = "Player";
    private bool nameSubmitted = false;

    private CoverTransitionManagerMain coverTransitionManager;

    void Start()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("ShootKey", KeyCode.Mouse0.ToString()));
        coverKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("CoverKey", KeyCode.Space.ToString()));
        pauseKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("PauseKey", KeyCode.Escape.ToString()));

        for (int i = 0; i < 5; i++)
        {
            highScores[i] = PlayerPrefs.GetInt("HighScore" + i, 0);
            highAccuracies[i] = PlayerPrefs.GetFloat("HighAccuracy" + i, 0f);
            playerNames[i] = PlayerPrefs.GetString("PlayerName" + i, "-");
        }

        coverTransitionManager = FindObjectOfType<CoverTransitionManagerMain>();
    }

    void Update()
    {
        if (currentState == MenuState.None && Input.GetKeyDown(pauseKey))
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

        if (currentState == MenuState.None && coverTransitionManager != null)
        {
            if (coverTransitionManager.CurrentIndex >= coverTransitionManager.SplineStops.Count)
            {
                ShowGameOverScreen();
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
        GUILayout.BeginArea(new Rect(40, 50, 920, 900));

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

            case MenuState.Leaderboard:
                DrawLeaderboard();
                break;
        }

        GUILayout.EndArea();
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
            currentState = MenuState.None;
        }

        if (GUILayout.Button("Main Menu", buttonStyle, GUILayout.Height(90)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        if (GUILayout.Button("Restart", buttonStyle, GUILayout.Height(90)))
        {
            if (crosshairController != null)
            {
                crosshairController.ResetStats();
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainLevel");
        }

        if (GUILayout.Button("Options", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.Options;
        }
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
                SceneManager.LoadScene("MainLevel");
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

    void DrawOptionsMenu()
    {
        GUIStyle backButtonStyle = new GUIStyle(GUI.skin.button);
        backButtonStyle.fontSize = 36;
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", backButtonStyle, GUILayout.Width(300), GUILayout.Height(90)))
        {
            currentState = MenuState.Pause;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

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

    void DrawLeaderboard()
    {
        GUIStyle backButtonStyle = new GUIStyle(GUI.skin.button);
        backButtonStyle.fontSize = 36;
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", backButtonStyle, GUILayout.Width(300), GUILayout.Height(90)))
        {
            currentState = MenuState.GameOver;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(40);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 64;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Leaderboard", titleStyle, GUILayout.Height(100));

        GUIStyle entryStyle = new GUIStyle(GUI.skin.label);
        entryStyle.fontSize = 36;
        entryStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.Space(40);

        for (int i = 0; i < 5; i++)
        {
            string rank = (i + 1) + ". ";
            string nameEntry = playerNames[i] != "-" ? playerNames[i] : "-";
            string scoreEntry = highScores[i] > 0 ? highScores[i].ToString() : "-";
            string accuracyEntry = highAccuracies[i] > 0 ? highAccuracies[i].ToString("F1") + "%" : "-";
            GUILayout.Label($"{rank} {nameEntry} | Score: {scoreEntry} | Accuracy: {accuracyEntry}", entryStyle, GUILayout.Height(70));
        }
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

    public void ShowGameOverScreen()
    {
        if (crosshairController != null)
        {
            crosshairController.ResetStats();
        }
        Time.timeScale = 0f;
        currentState = MenuState.GameOver;
    }

    public void ShowPauseMenu()
    {
        Time.timeScale = 0f;
        currentState = MenuState.Pause;
    }

    public void GameOver()
    {
        Debug.Log("GameManagerMain: Game Over triggered due to player death.");
        ShowGameOverScreen();
    }

    // Added methods to check menu state
    public bool IsPaused()
    {
        return currentState == MenuState.Pause;
    }

    public bool IsGameOver()
    {
        return currentState == MenuState.GameOver;
    }
}