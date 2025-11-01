using UnityEngine;
using System.Collections;
using System;
using TMPro;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public int starsToNextLevel = 5;
    public int currentStars = 0;

    public float levelTime = 0f;

    public Action OnStarsReached;
    public Action OnLevelFailed;

    private float remainingTime;
    private bool levelActive = false;

    public TextMeshProUGUI timerText;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        StartLevel();
    }

    public void StartLevel()
    {
        remainingTime = levelTime;
        levelActive = true;
        
        UpdateTimerUI();
        StartCoroutine(LevelTimer());
    }

    private IEnumerator LevelTimer()
    {
        while (remainingTime > 0 && levelActive)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerUI(); 
            yield return null;
        }

        if (levelActive && currentStars < starsToNextLevel)
        {
            Debug.Log("Time's up! You failed the level.");
            OnLevelFailed?.Invoke();
            levelActive = false;
        }
    }
    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = $"{minutes}:{seconds:00}";
    }

    public void AddStars(int amount)
    {
        currentStars += amount;
        Debug.Log($"Earned {amount} stars. Total: {currentStars}/{starsToNextLevel}");

        if (currentStars >= starsToNextLevel)
            Debug.Log("Finished Level");
    }

    public void ResetStars()
    {
        currentStars = 0;
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }
}
