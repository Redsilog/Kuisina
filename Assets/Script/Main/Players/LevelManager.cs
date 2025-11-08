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
    public int requiredStars = 0;     // Stars needed to unlock next level
    public float levelTime = 120f;    // Time limit

    [Header("Runtime Data")]
    public int totalStars = 0;
    private float remainingTime;
    private bool levelActive = false;

    [Header("UI References (TMP)")]
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI levelCompleteText;  // Added TMP text for result (Level Complete / Failed)
    public TextMeshProUGUI earnedStarText;  // Added TMP text for earned stars (e.g., "3")
    public TextMeshProUGUI requiredStarText;  // Added TMP text for required stars (e.g., "10")

    [Header("In-Game UI")]
    public TextMeshProUGUI starCounterText;
    
    [Header("Buttons")]
    public Button returnButton;
    public Button retryButton;
    public Button nextButton;

    public Action OnStarsReached;
    public Action OnLevelFailed;

    [Header("Debug Tools")]
    public bool overrideStars = false;   // enables manual control
    public int debugStars = 0;           // manually set this in Inspector

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "Main Level 1")
        {
            return;
        }

        else
        {
            startLevel();
        }
        
    }
    
    void startLevel()
    {
        string key = $"{levelID}_TotalStars";

        // Always reset stars at the start
        totalStars = 0;
        PlayerPrefs.SetInt(key, totalStars);
        PlayerPrefs.Save();
        Debug.Log($"[LevelManager] Reset {key} = 0");

        // Update UI
        UpdateStarCounterUI();
        StartLevel();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (returnButton != null) returnButton.onClick.AddListener(ReturnToMenu);
        if (retryButton != null) retryButton.onClick.AddListener(RestartLevel);
        if (nextButton != null) nextButton.onClick.AddListener(GoToNextLevel);
    }


    void Update()
    {
        if (overrideStars)
        {
            debugStars = Mathf.Max(0, debugStars); // prevent negatives

            if (totalStars != debugStars)
            {
                totalStars = debugStars;
                SaveStars();
                UpdateStarCounterUI();
                Debug.Log($"[DEBUG] Forced star count to {totalStars}");
            }
        }
    }
    
    public void StartLevel()
    {
        remainingTime = levelTime;
        levelActive = true;
        UpdateTimerUI();
        UpdateStarCounterUI();
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
        
        totalStars += amount; // ✅ remove *5
        Debug.Log($"Earned {amount} stars | Total: {totalStars}/{requiredStars}");
        SaveStars();

        UpdateStarCounterUI();
    }

    private void ShowResultPanel(bool isComplete)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);

        // Show level result text (Level Complete or Level Failed)
        if (levelCompleteText != null)
        {
            levelCompleteText.text = isComplete ? "Level Complete" : "Level Failed";  // 1 for "Level Complete", 0 for "Level Failed"
            AddTextShadow(levelCompleteText, new Vector2(6f, -6f), new Color(0, 0, 0, 0.5f));
        }

        // Update earned stars
        if (earnedStarText != null)
        {
            earnedStarText.text = $"{totalStars}";  // Show only the earned stars number
        }

        // Update required stars
        if (requiredStarText != null)
        {
            requiredStarText.text = $"{requiredStars}";  // Show the required stars number
        }

        if (nextButton != null)
        {
            var nextImage = nextButton.GetComponent<Image>();

            if (isComplete)
            {
                nextButton.interactable = true;

                if (nextImage != null)
                {
                    Color c = nextImage.color;
                    c.a = 1f;
                    nextImage.color = c;
                }
            }
            else
            {
                nextButton.interactable = false;

                if (nextImage != null)
                {
                    Color c = nextImage.color;
                    c.a = 0.9f;
                    nextImage.color = c;
                }
            }
        }
    }

    // Button actions
    public void ReturnToMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextLevel()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        // Check if next index exists within the build settings
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            string nextSceneName = System.IO.Path.GetFileNameWithoutExtension(
                SceneUtility.GetScenePathByBuildIndex(nextIndex)
            );

            PlayerPrefs.SetString("LastLevel", nextSceneName);
            PlayerPrefs.Save();

            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("No further levels found — returning to main menu.");
            SceneManager.LoadScene("Main Menu");
        }
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
        UpdateStarCounterUI();
    }


    public void ResetStars()
    {
        totalStars = 0;
        SaveStars();
    }
    private void AddTextShadow(TextMeshProUGUI originalText, Vector2 offset, Color shadowColor)
    {
        if (originalText == null) return;

        // Create a duplicate GameObject
        GameObject shadowObj = Instantiate(originalText.gameObject, originalText.transform.parent);

        // Get the TextMeshProUGUI component
        TextMeshProUGUI shadowText = shadowObj.GetComponent<TextMeshProUGUI>();

        // Adjust its appearance
        shadowText.text = originalText.text;
        shadowText.color = shadowColor;
        shadowText.raycastTarget = false;  // so it doesn't block clicks

        // Move it slightly behind
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchoredPosition += offset;

        // Make sure it's rendered *behind* the main text
        shadowObj.transform.SetSiblingIndex(originalText.transform.GetSiblingIndex());

        // Optional: give it a name for clarity
        shadowObj.name = originalText.name + "_Shadow";
    }
    private void UpdateStarCounterUI()
    {
        if (starCounterText != null)
        {
            starCounterText.text = $"{totalStars} / {requiredStars}";
        }
    }
}
