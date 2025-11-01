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
    public int requiredStars = 3;     // Stars needed to unlock next level
    public float levelTime = 120f;    // Time limit

    [Header("Runtime Data")]
    public int totalStars = 0;
    private float remainingTime;
    private bool levelActive = false;

    [Header("UI References (TMP)")]
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI requirementText;
    public TextMeshProUGUI starsText;

    [Header("Result Title Images")]
    public GameObject levelCompleteImage;   //  Assign the "Level Complete" banner
    public GameObject levelFailedImage;     //  Assign the "Level Failed" banner

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

        if (levelCompleteImage != null) levelCompleteImage.SetActive(false);
        if (levelFailedImage != null) levelFailedImage.SetActive(false);

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
        Debug.Log($"Earned {amount}  | Total: {totalStars}/{requiredStars}");
        SaveStars();
    }

    private void ShowResultPanel(bool isComplete)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);

        //  Show the correct image
        if (levelCompleteImage != null)
            levelCompleteImage.SetActive(isComplete);

        if (levelFailedImage != null)
            levelFailedImage.SetActive(!isComplete);

        //  Update texts 
        levelNameText.text = levelID;
        requirementText.text = $"REQUIRED STARS = {requiredStars}";
        starsText.text = $"STAR TOTAL = {totalStars}";

        //  Only show "Next" button when complete
        if (nextButton != null)
            nextButton.gameObject.SetActive(isComplete);
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
