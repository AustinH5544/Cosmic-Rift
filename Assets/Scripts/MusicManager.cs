using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    public AudioSource mainMenuMusic;
    public AudioSource shootingRangeMusic;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mainMenuMusic != null)
        {
            mainMenuMusic.Stop();
        }
        if (shootingRangeMusic != null)
        {
            shootingRangeMusic.Stop();
        }

        if (scene.name == "MainMenu" && mainMenuMusic != null)
        {
            mainMenuMusic.Play();
        }
        else if (scene.name == "ShootingRange" && shootingRangeMusic != null)
        {
            shootingRangeMusic.Play();
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}