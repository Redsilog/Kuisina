using TMPro;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class MainMenu : MonoBehaviour
{
    [Header("Confirmation UI")]
    public GameObject confirmationPanel;
    public Button confirmYesButton;
    public Button confirmNoButton;
    public Button newGameButton;
    public Button continueButton;
    public GameObject mainMenu;
    public GameObject optionsPage;
    public GameObject recipePage;
    public GameObject playerSelectPage;
    public GameObject helpPage;
    public Button startButton;
    public Button mainSettingsButton, mainRecipeButton;
    public Button mainHelpButton, backFromHelpButton;
    public Button backFromSettingsButton, backFromRecipesButton, backFromConfirmation;
    public Button levelsBackButton;
    public Button exitButton;
    public Button onePlayerButton, twoPlayersButton;
    public TextMeshProUGUI optionsText, helpText, recipesText;

    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public TextMeshProUGUI loadingText;
    public float fadeDuration = 0.5f;


    void Start()
    {
        //continue button testing
        //PlayerPrefs.SetString("LastLevel", "Main Level 2");
        //PlayerPrefs.Save();
        //PlayerPrefs.DeleteAll();
        
        startButton.onClick.AddListener(StartButton);
        playerSelectPage.SetActive(false);
        mainSettingsButton.onClick.AddListener(mainOptions);
        mainRecipeButton.onClick.AddListener(Recipes);
        mainHelpButton.onClick.AddListener(Help);
        backFromHelpButton.onClick.AddListener(BackFromHelp);
        backFromSettingsButton.onClick.AddListener(BackFromOptions);
        backFromRecipesButton.onClick.AddListener(BackFromRecipes);
        levelsBackButton.onClick.AddListener(levelsBack);
        exitButton.onClick.AddListener(ExitGame);
        onePlayerButton.onClick.AddListener(OnePlayer);
        twoPlayersButton.onClick.AddListener(TwoPlayers);

        newGameButton.onClick.AddListener(NewGame);
        continueButton.onClick.AddListener(ContinueGame);

        confirmationPanel.SetActive(false);
        confirmYesButton.onClick.AddListener(ConfirmNewGame);
        confirmNoButton.onClick.AddListener(CancelNewGame);

        optionsText.gameObject.SetActive(false);
        helpText.gameObject.SetActive(false);
        recipesText.gameObject.SetActive(false);

        AddHover(mainSettingsButton, optionsText);
        AddHover(mainRecipeButton, recipesText);
        AddHover(mainHelpButton, helpText);

        UpdateContinueButtonState();

    }

    private void AddHover(Button button, TextMeshProUGUI text)
    {
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((_) => text.gameObject.SetActive(true));
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((_) => text.gameObject.SetActive(false));
        trigger.triggers.Add(entryExit);
    }


    public void mainOptions()
    {
        mainMenu.SetActive(false);
        optionsPage.SetActive(true);
    }
    public void Recipes()
    {
        mainMenu.SetActive(false);
        recipePage.SetActive(true);
    }
    public void Help()
    {
        mainMenu.SetActive(false);
        helpPage.SetActive(true);
    }
    public void BackFromHelp()
    {
        helpPage.SetActive(false);
        mainMenu.SetActive(true);
        optionsText.gameObject.SetActive(false);
        helpText.gameObject.SetActive(false);
        recipesText.gameObject.SetActive(false);
    }
    public void BackFromOptions()
    {
        optionsPage.SetActive(false);
        mainMenu.SetActive(true);
        optionsText.gameObject.SetActive(false);
        helpText.gameObject.SetActive(false);
        recipesText.gameObject.SetActive(false);
    }
    public void BackFromRecipes()
    {
        recipePage.SetActive(false);
        mainMenu.SetActive(true);
        optionsText.gameObject.SetActive(false);
        helpText.gameObject.SetActive(false);
        recipesText.gameObject.SetActive(false);
    }

    public void StartButton()
    {
        playerSelectPage.SetActive(true);
        mainMenu.SetActive(false);
    }
    public void levelsBack()
    {
        playerSelectPage.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void NewGame()
    {
        if (PlayerPrefs.HasKey("LastLevel"))
        {
            if (confirmationPanel != null)
                confirmationPanel.SetActive(true);
            mainMenu.SetActive(false);
        }
        else
        {
            StartFreshGame();
        }
    }

    private void ConfirmNewGame()
    {
        PlayerPrefs.DeleteKey("LastLevel");
        PlayerPrefs.DeleteKey("PlayerCount");
        PlayerPrefs.Save();
        Debug.Log(" Starting new game... Progress reset.");

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        playerSelectPage.SetActive(true);
        mainMenu.SetActive(false);
        UpdateContinueButtonState();
    }

    private void CancelNewGame()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
        mainMenu.SetActive(true);
    }
    private void StartFreshGame()
    {
        PlayerPrefs.DeleteKey("LastLevel");
        PlayerPrefs.DeleteKey("PlayerCount");
        PlayerPrefs.Save();

        Debug.Log(" No previous save — starting a new game fresh.");
        playerSelectPage.SetActive(true);
        mainMenu.SetActive(false);
        UpdateContinueButtonState();
    }

    public void ContinueGame()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (playerSelectPage != null) playerSelectPage.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(false);
        if (optionsPage != null) optionsPage.SetActive(false);
        if (recipePage != null) recipePage.SetActive(false);
        if (helpPage != null) helpPage.SetActive(false);

        if (PlayerPrefs.HasKey("LastLevel"))
        {
            string lastLevel = PlayerPrefs.GetString("LastLevel");
            int playerCount = PlayerPrefs.GetInt("PlayerCount", 1);

            GameMode.Instance.SetPlayers(playerCount);
            SceneManager.LoadScene(lastLevel);
        }
        else
        {
            Debug.Log("No saved game found — starting new game instead.");
            NewGame();
        }
    }
    public void ExitGame()
    {
        Debug.Log("Game exited");
        Application.Quit();
    }
    public void OnePlayer()
    {
        GameMode.Instance.SetPlayers(1);

        PlayerPrefs.SetInt("PlayerCount", 1);
        PlayerPrefs.Save();

        SceneToLoad.nextScene = "Player Level 1";
        SceneManager.LoadScene("Loading Screen");
    }

    public void TwoPlayers()
    {
        GameMode.Instance.SetPlayers(2);

        PlayerPrefs.SetInt("PlayerCount", 2);
        PlayerPrefs.Save();

        SceneToLoad.nextScene = "Main Level 1";
        SceneManager.LoadScene("Loading Screen");
    }
    private void UpdateContinueButtonState()
    {
        bool hasSave = PlayerPrefs.HasKey("LastLevel");

        continueButton.interactable = hasSave;

        CanvasGroup cg = continueButton.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = continueButton.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = hasSave ? 1f : 0.9f;
    }
}
