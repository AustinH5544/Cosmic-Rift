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
    private Rect windowRect = new Rect(Screen.width / 2 - 400, Screen.height / 2 - 450, 800, 900);
    private Rect storyLogsWindowRect = new Rect(Screen.width / 2 - 1000, Screen.height / 2 - 600, 2000, 1200);
    private enum MenuState { MainMenu, StageSelection, Options, SoundSettings, ControlsSettings, StoryLogs }
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

    // Story Logs
    private string[] storyLogs = new string[]
    {
        "Vol.1 (Year 2077)\n\n“Hello my name is Elle, today is August 20th 2077. This is my first log so bear with me. It is the 100 year anniversary of voyager 1. My father was an astronomer and we used to spend almost every night looking at the stars. He was really looking forward to this day. It’s been six months since his passing and my therapist thought it would be a good idea to talk out my feelings. I really hate doing this but I’m going to give it a try. I miss you dad .. I’m going to continue searching for you in the stars.",
        "Vol.2\n\n“Hi, Elle here. It’s Aug 27th 2077. I’m going to try to make these tapes weekly. I got inspired last week and broke out dad’s old equipment. I’m surprised it even still works. The roof in the shed was leaking but I was able to salvage his telescope and his old notebook. I’ve gotten it all set up. Dad’s notes are pretty messy and a little damp but I’ll try to decipher what I can. I wish we’d spent more time together. I really took these moments for granted but I’m here now dad.",
        "Vol.3\n\n“So I’ve been going through dads notes. There a lot of information here. He was so detailed. Everything is well dated and described. He even has entries from when I was a kid. ‘Elle and I went out to our favorite camping spot(41.70242° N, 103.66496° W). Clear skies and 75 degrees. We set up the tent and cooked dinner. I think we both chipped a tooth on the rehydrated biscuits but I wouldn’t trade it for the world. Tonight we set up facing southward. There is supposed to be a meteor shower tonight. Hopefully God gives us a show.’ I remember that day. I was such a brat back then… I wish I would have appreciated the time we had together.",
        "Vol.4\n\n“It’s Nov 14th now. I haven’t made a tape in a while. Reading dad’s old journal really got to me. I’ve decided to restart my look through his books and I noticed something weird. The beginning of notebook starts normally just our camping trips but there are some I don’t remember. Maybe he took some trips by himself. This isn’t the weirdest thing, towards the end he starts writing all over the margins. Weird symbols, math, drawings. Short quotes not cited to anything. I was talking to mom about this and she just wrote it off as he was a quirky guy. I know that he wasn’t crazy.",
        "Vol.5\n\n“Today is the day my whole life got turned upside down. I’ve been working on dad’s book for a few months now. Something just isn’t adding up. So the story is that he lost his mind out in the woods and hung himself. I don’t believe it. Dad wasn’t crazy. I talked to him the week prior and everything seemed fine. There are parts of his notebook pointing out new viewing sites and his plan to see them listed after his supposed suicide. While these are mixed between his ramblings I know that he didn’t do it. I’ve been going through his computer and I think I found something. I’ve been trying to match the drawing from his books. There is a file on the trajectory of the voyager. Transposing the symbols gives us a what I think is a planet. I’ve looked it up and I think I’ve figured out which one it is by combining both of these data points. I don’t know what he saw but this is much bigger than I originally thought.",
        "Vol.6\n\n“I took the data to some of his associates and started asking questions. It was weird. They acted like they didn’t know what I was talking about and hurried me away. On my way home the other day I noticed a car. Well a suv dark tinted windows no visible plates. It has been parked just down the street from my house all week. Maybe I’m losing it too and it’s just someone visiting a neighbor. (End of tape)",
        "Vol.7\n\n“Yesterday an anonymous note showed up inside my house… It said ‘Keep your nose out of things that aren’t your business. Just go back to your normal life. Love, Dad’ I’m seriously fucking freaked out. I always lock the doors …who the fuck came into my house. I had the police come and they thought it was some sort of practical joke. Fucking useless. I’m just going to get in my car and drive as far away as possible. This is proof and I’m going to find out what happened",
        "Vol.8\n\n(A female screaming and a struggle can be heard. This promptly stops followed by a muffled male voice.) ‘Put her in the trunk’ (dragging sounds followed by a loud metal clanking and fading footsteps on rocks followed by the sound of car doors closing a second voice male starts) … just … leave alone  ….  You have … what issues… caused. … found … chaos. (the next 10 minutes are muffled shallow breathing, car sounds and unintelligible radio) (end of tape)"
    };
    private int unlockedLogs = 0;
    private Vector2 scrollPosition = Vector2.zero;

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

        unlockedLogs = PlayerPrefs.GetInt("UnlockedStoryLogs", 0);

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

        if (backgroundMusic != null)
        {
            backgroundMusic.loop = true;
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

        // Debug log to verify window sizes
        Debug.Log($"Screen Resolution: {Screen.width}x{Screen.height}");
        Debug.Log($"Main Window Rect: {windowRect}");
        Debug.Log($"Story Logs Window Rect: {storyLogsWindowRect}");

        // Draw the main window for all menus except StoryLogs
        if (currentState != MenuState.StoryLogs)
        {
            windowRect = GUI.Window(0, windowRect, DrawMenuWindow, "");
        }
        // Draw the Story Logs window separately when in StoryLogs state
        else
        {
            storyLogsWindowRect = GUI.Window(1, storyLogsWindowRect, DrawStoryLogsWindow, "");
        }

        GUI.color = crosshairColor;
        GUI.DrawTexture(crosshairRect, crosshairTexture);
        GUI.color = Color.white;
    }

    void DrawMenuWindow(int windowID)
    {
        GUILayout.BeginArea(new Rect(40, 50, 720, 800));

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
        GUILayout.Label("Cosmic Rift", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        GUILayout.Space(50);

        if (GUILayout.Button("Play", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.StageSelection;
        }

        if (GUILayout.Button("Options", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.Options;
        }

        if (GUILayout.Button("Story Logs", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.StoryLogs;
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
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Stage Selection", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        GUILayout.Space(50);

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
            SceneManager.LoadScene("MainLevel");
        }

        if (GUILayout.Button("Back", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.MainMenu;
        }
    }

    void DrawOptions()
    {
        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Options", titleStyle, GUILayout.Height(80));

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        GUILayout.Space(50);

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

        if (GUILayout.Button("Back", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.MainMenu;
        }
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
        buttonStyle.fontSize = 28;

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

        if (GUILayout.Button("Back", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.Options;
        }
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
        buttonStyle.fontSize = 28;

        GUILayout.Space(50);

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

        if (GUILayout.Button("Back", buttonStyle, GUILayout.Height(90)))
        {
            currentState = MenuState.Options;
        }
    }

    void DrawStoryLogsWindow(int windowID)
    {
        GUILayout.BeginArea(new Rect(50, 50, 1900, 1100));

        GUILayout.Space(20);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 56;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Story Logs", titleStyle, GUILayout.Height(80));

        GUIStyle logStyle = new GUIStyle(GUI.skin.label);
        logStyle.fontSize = 32;
        logStyle.alignment = TextAnchor.UpperLeft;
        logStyle.wordWrap = true;
        logStyle.padding = new RectOffset(10, 10, 0, 0);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.Space(50);

        // Scrollable area for logs, including the button
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Height(860));

        // Display logs up to the number of unlocked logs, with reduced width and centered
        for (int i = 0; i < storyLogs.Length && i <= unlockedLogs; i++)
        {
            GUIContent content = new GUIContent(storyLogs[i]);
            float height = logStyle.CalcHeight(content, 1400); // Reduced width to 1400 for larger margins
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Adds space on the left
            GUILayout.Label(storyLogs[i], logStyle, GUILayout.Width(1400), GUILayout.Height(height));
            GUILayout.FlexibleSpace(); // Adds space on the right
            GUILayout.EndHorizontal();
            GUILayout.Space(40);
        }

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back", buttonStyle, GUILayout.Height(90), GUILayout.Width(300)))
        {
            currentState = MenuState.MainMenu;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.EndScrollView();

        GUILayout.EndArea();
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