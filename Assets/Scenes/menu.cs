using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Main Menu 2");
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
