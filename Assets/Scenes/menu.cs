using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject optionsPage;
    public GameObject recipePage;
    public GameObject playerSelectPage;
    public Button startButton;
    public Button mainsettingsButton, mainrecipeButton;
    public Button backFromSettingsButton, backFromRecipesButton;
    public Button levelsBackButton;
    public Button exitButton;
    public Button onePlayerButton, twoPlayersButton;


    void Start()
    {
        startButton.onClick.AddListener(StartButton);
        playerSelectPage.SetActive(false);
        mainsettingsButton.onClick.AddListener(mainOptions);
        mainrecipeButton.onClick.AddListener(Recipes);
        backFromSettingsButton.onClick.AddListener(BackFromOptions);
        backFromRecipesButton.onClick.AddListener(BackFromRecipes);
        levelsBackButton.onClick.AddListener(levelsBack);
        exitButton.onClick.AddListener(ExitGame);
        onePlayerButton.onClick.AddListener(OnePlayer);
        twoPlayersButton.onClick.AddListener(TwoPlayers);
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
    public void BackFromOptions()
    {
        optionsPage.SetActive(false);
        mainMenu.SetActive(true);
    }
    public void BackFromRecipes()
    {
        recipePage.SetActive(false);
        mainMenu.SetActive(true);
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
