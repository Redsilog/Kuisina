using UnityEngine;

public class MainMenuScript : MonoBehaviour
{

    public GameObject mainMenu;
    public GameObject optionsMenu;
    public GameObject recipeMenu;
    public GameObject playerSelect;

    public void Options()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }   
    public void Recipes()
    {
        mainMenu.SetActive(false);
        recipeMenu.SetActive(true);
    }
    public void BackFromOptions()
    {
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
    public void BackFromRecipes()
    {
        recipeMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void StartButton()
    {
        playerSelect.SetActive(true);
        mainMenu.SetActive(false);
    }
}