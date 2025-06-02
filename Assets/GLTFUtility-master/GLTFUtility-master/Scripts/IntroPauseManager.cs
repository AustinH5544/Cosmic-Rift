using UnityEngine;
using UnityEngine.UI;

public class IntroPauseManager : MonoBehaviour
{
	public GameObject introUI;
	public Button startButton;

	private bool hasStarted = false;

	void Start()
	{
		Time.timeScale = 0f;
		introUI.SetActive(true);
		startButton.onClick.AddListener(ResumeGame);
	}

	void Update()
	{
		if (!hasStarted && Input.GetKeyDown(KeyCode.Escape))
		{
			ResumeGame(); // Treat Esc like a "skip"
		}
	}

	void ResumeGame()
	{
		if (hasStarted) return;

		hasStarted = true;
		introUI.SetActive(false);
		Time.timeScale = 1f;
	}
}