using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseMenu : MonoBehaviour
{

    [SerializeField] public GameObject pauseMenu;
    [SerializeField] public GameObject optionsMenu;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("pressing esc");
            if (optionsMenu.activeSelf)
            {
                optionsMenu.SetActive(false);
                pauseMenu.SetActive(true);
            }
            else if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void home()
    {
        if (SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PauseAllSounds();
        }

        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene("Main Menu");
    }
    
    public void Pause()
    {
        pauseMenu.SetActive(true);
        optionsMenu.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;
        
        if (SoundFXManager.instance != null)
            SoundFXManager.instance.PauseAllSounds();
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        if (SoundFXManager.instance != null)
            SoundFXManager.instance.ResumeAllSounds();
    }

    public void Options()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void Exit()
    {
        //quit
    }
}
