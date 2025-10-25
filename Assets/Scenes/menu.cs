using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Main Level 1");
    }

    public void ExitGame()
    {
        Debug.Log("Game exited");
        Application.Quit();
    }
}
