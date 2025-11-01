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
    public string levelID = "Day1";       // Example: "Day1", "Day2"
    public int starsToNextLevel = 15;     // Quota
    public float levelTime = 120f;        // Level duration in seconds

    [Header("Runtime Data")]
    public int totalStars = 0;
    private float remainingTime;
    private bool levelActive = false;

    [Header("UI References (TMP)")]
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;              // Your Level Complete panel
    public TextMeshProUGUI resultTitleText;     // "LEVEL COMPLETE" / "LEVEL FAILED"
    public TextMeshProUGUI levelNameText;       // e.g. "LEVEL 1"
    public TextMeshProUGUI quotaText;           // "QUOTA TOTAL ="
    public TextMeshProUGUI starsText;           // "STARS TOTAL ="

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
            resultPanel.SetActive(false); // Hide panel at start

        // Assign button listeners
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

        if (totalStars >= starsToNextLevel)
        {
            Debug.Log($" Level Complete! You earned {totalStars} out of {starsToNextLevel}.");
            ShowResultPanel(true);
            OnStarsReached?.Invoke();
        }
        else
        {
            Debug.Log($"Time's up! You failed the level with {totalStars}out of {starsToNextLevel}.");
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

    //  Add stars
    public void AddStars(int amount)
    {
        totalStars += amount;
        Debug.Log($"Earned {amount} | Total: {totalStars}/{starsToNextLevel}");
        SaveStars();
    }

    //  Show the result panel and fill in TMP texts
    private void ShowResultPanel(bool isComplete)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);
        resultTitleText.text = isComplete ? "LEVEL COMPLETE" : "LEVEL FAILED";
        levelNameText.text = levelID;
        quotaText.text = $"QUOTA TOTAL = {starsToNextLevel}";
        starsText.text = $"STARS TOTAL = {totalStars}";
    }

    //  Button actions
    private void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");  // Change scene name as needed
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToNextLevel()
    {
        SceneManager.LoadScene("NextLevel");  // Change this later when you know next scene name
    }

    //  Save / Load
    public void SaveStars()
    {
        PlayerPrefs.SetInt($"{levelID}_TotalStars", totalStars);
        PlayerPrefs.Save();
        Debug.Log($"[SAVE] {levelID} total stars = {totalStars}");
    }

    public void LoadStars()
    {
        totalStars = PlayerPrefs.GetInt($"{levelID}_TotalStars", 0);
        Debug.Log($"[LOAD] {levelID} total stars = {totalStars}");
    }

    public void ResetStars()
    {
        totalStars = 0;
        SaveStars();
    }
}
