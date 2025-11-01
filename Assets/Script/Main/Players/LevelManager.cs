using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using TMPro;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Level Settings")]
    public string levelID = "Day1";   // Example: "Day1", "Day2"
    public int requiredStars = 3;     //  number of stars needed to unlock the next level
    public float levelTime = 120f;    // Time limit

    [Header("Runtime Data")]
    public int totalStars = 0;
    private float remainingTime;
    private bool levelActive = false;

    [Header("UI References (TMP)")]
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI requirementText;   // was quotaText
    public TextMeshProUGUI starsText;

    [Header("Buttons")]
    public Button returnButton;
    public Button retryButton;
    public Button nextButton;

    public Action OnStarsReached;
    public Action OnLevelFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        LoadStars();
        StartLevel();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (returnButton != null) returnButton.onClick.AddListener(ReturnToMenu);
        if (retryButton != null) retryButton.onClick.AddListener(RestartLevel);
        if (nextButton != null) nextButton.onClick.AddListener(GoToNextLevel);
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

        if (!levelActive) yield break;

        // When time runs out, check if enough stars reached
        bool hasEnoughStars = totalStars >= requiredStars;

        if (hasEnoughStars)
        {
            Debug.Log($" Level Complete! Earned {totalStars} (Requirement: {requiredStars})");
            ShowResultPanel(true);
            OnStarsReached?.Invoke();
        }
        else
        {
            Debug.Log($"Time's up! You only got {totalStars} out of {requiredStars} required.");
            ShowResultPanel(false);
            OnLevelFailed?.Invoke();
        }

        levelActive = false;
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = $"{minutes}:{seconds:00}";
    }

    public void AddStars(int amount)
    {
        totalStars += amount;
        Debug.Log($"Earned {amount} | Total: {totalStars}/{requiredStars}");
        SaveStars();
    }

    private void ShowResultPanel(bool isComplete)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);
        resultTitleText.text = isComplete ? "LEVEL COMPLETE" : "LEVEL FAILED";
        levelNameText.text = levelID;

        // display the requirement and total stars
        requirementText.text = $"REQUIRED STARS = {requiredStars}";
        starsText.text = $"STAR TOTAL = {totalStars}";

        // show Next button only if enough stars
        if (nextButton != null)
            nextButton.gameObject.SetActive(isComplete);
    }

    // Buttons
    private void ReturnToMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToNextLevel()
    {
        SceneManager.LoadScene("NextLevel");
    }

    // Save and Load
    public void SaveStars()
    {
        PlayerPrefs.SetInt($"{levelID}_TotalStars", totalStars);
        PlayerPrefs.Save();
    }

    public void LoadStars()
    {
        totalStars = PlayerPrefs.GetInt($"{levelID}_TotalStars", 0);
    }

    public void ResetStars()
    {
        totalStars = 0;
        SaveStars();
    }
}
