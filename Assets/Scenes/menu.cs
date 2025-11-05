using TMPro;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject optionsPage;
    public GameObject recipePage;
    public GameObject playerSelectPage;
    public GameObject helpPage;
    public Button startButton;
    public Button mainSettingsButton, mainRecipeButton;
    public Button mainHelpButton, backFromHelpButton;
    public Button backFromSettingsButton, backFromRecipesButton;
    public Button levelsBackButton;
    public Button exitButton;
    public Button onePlayerButton, twoPlayersButton;
    public TextMeshProUGUI optionsText, helpText, recipesText;


    void Start()
    {
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

        optionsText.gameObject.SetActive(false);
        helpText.gameObject.SetActive(false);
        recipesText.gameObject.SetActive(false);

        AddHover(mainSettingsButton, optionsText);
        AddHover(mainRecipeButton, recipesText);
        AddHover(mainHelpButton, helpText);

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

    public void ExitGame()
    {
        Debug.Log("Game exited");
        Application.Quit();
    }
    public void OnePlayer()
    {
        GameMode.Instance.SetPlayers(1);
        SceneManager.LoadScene("Main Level 1");
    }

    public void TwoPlayers()
    {
        GameMode.Instance.SetPlayers(2);
        SceneManager.LoadScene("Main Level 1");
    }
}
