using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMainLevel()
    {
        SceneManager.LoadScene("MainLevel"); // Make sure MainLevel is added in Build Settings
    }
}