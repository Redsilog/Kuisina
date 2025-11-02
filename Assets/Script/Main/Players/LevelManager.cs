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

    [Header("Result Sprite Images")]
    public Image resultImage;           // Image for Level Complete or Failed
    public Sprite levelCompleteSprite;  // Sprite for "LEVEL COMPLETE"
    public Sprite levelFailedSprite;    // Sprite for "LEVEL FAILED"

    [Header("Star Images")]
    public Image star1Image; // Image for Star 1
    public Image star2Image; // Image for Star 2
    public Image star3Image; // Image for Star 3
    public Image star4Image; // Image for Star 4
    public Image star5Image; // Image for Star 5

    public Sprite filledStarSprite; // Sprite for filled star
    public Sprite emptyStarSprite;  // Sprite for empty star

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

        // Change sprite based on result
        if (resultImage != null)
        {
            resultImage.sprite = isComplete ? levelCompleteSprite : levelFailedSprite;
        }

        levelNameText.text = levelID;
        requirementText.text = $"REQUIRED STARS = {requiredStars}";

        // Update the stars (fill or empty) based on the totalStars
        UpdateStarImages();

        // Only show "Next" button when player meets the requirement
        if (nextButton != null)
            nextButton.gameObject.SetActive(isComplete);
    }

    // Update the star images based on the totalStars
    private void UpdateStarImages()
    {
        // Get the filled and empty star sprites based on totalStars
        Image[] stars = { star1Image, star2Image, star3Image, star4Image, star5Image };

        for (int i = 0; i < stars.Length; i++)
        {
            // If totalStars is greater than i, show filled star, else show empty star
            stars[i].sprite = (i < totalStars) ? filledStarSprite : emptyStarSprite;
        }
    }

    //  Button actions
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

    //  Save and Load
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
