using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using TMPro;  // Import TMP
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Level Settings")]
    public string levelID = "Day1";   // Example: "Day1", "Day2"
    public int requiredStars = 3;     // Stars needed to unlock next level
    public float levelTime = 120f;    // Time limit

    [Header("Runtime Data")]
    public int totalStars = 0;
    private float remainingTime;
    private bool levelActive = false;

    [Header("UI References (TMP)")]
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI levelCompleteText;  // Added TMP text for result (Level Complete / Failed)
    public TextMeshProUGUI starText;  // Added TMP text for integer stars (earned / required)

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

        bool hasEnoughStars = totalStars >= requiredStars;

        if (hasEnoughStars)
        {
            Debug.Log($"Level Complete! Earned {totalStars} (Requirement: {requiredStars})");
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
        // Each dish completed is worth 5 points
        totalStars += amount * 5;
        Debug.Log($"Earned {amount * 5} points | Total: {totalStars}/{requiredStars}");
        SaveStars();
    }

    private void ShowResultPanel(bool isComplete)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);

        // Show level result text (Level Complete or Level Failed)
        if (levelCompleteText != null)
        {
            levelCompleteText.text = isComplete ? "1" : "0";  // 1 for "Level Complete", 0 for "Level Failed"
        }

        // Only show numeric stars (earned / required)
        UpdateStarText();

        // Only show "Next" button when player meets the requirement
        if (nextButton != null)
            nextButton.gameObject.SetActive(isComplete);
    }

    // Update the star text as an integer value based on totalStars
    private void UpdateStarText()
    {
        // Show the stars as numbers, e.g., "3 / 5"
        starText.text = $"{totalStars} / {requiredStars}";
    }

    // Button actions
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
