using UnityEngine;
using System.Collections;
using System;

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
        StartCoroutine(LevelTimer());
    }

    private IEnumerator LevelTimer()
    {
        while (remainingTime > 0 && levelActive)
        {
            remainingTime -= Time.deltaTime;
            Debug.Log($"Time remaining: {remainingTime:F2} seconds");
            yield return null;
        }

        if (levelActive && currentStars < starsToNextLevel)
        {
            Debug.Log("Time's up! You failed the level.");
            OnLevelFailed?.Invoke();
            levelActive = false;
        }
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
