using UnityEngine;
using TMPro;

public class TimerMain : MonoBehaviour
{
    public TMP_Text timerText;
    public float totalTime = 30f;
    public GameManagerMain gameManager;

    private float timeRemaining;
    private bool isGameOver = false;

    void Start()
    {
        timeRemaining = totalTime;
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (isGameOver) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                isGameOver = true;
                OnTimerEnd();
            }
        }
    }

    public void AddTime(float bonusTime)
    {
        if (isGameOver) return;

        timeRemaining += bonusTime;
        UpdateTimerDisplay();
        Debug.Log($"TimerMain: Added {bonusTime} seconds. Current Time: {timeRemaining}");
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void OnTimerEnd()
    {
        if (gameManager != null)
        {
            gameManager.ShowGameOverScreen();
            Debug.Log("TimerMain: Time ran out! Game Over.");
        }
    }
}