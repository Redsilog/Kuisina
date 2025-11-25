using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] public GameObject pauseMenu;
    [SerializeField] public GameObject optionsMenu;
    [SerializeField] public GameObject helpMenu;
    [SerializeField] public GameObject firstPauseButton; // Assign in inspector

    private bool isPaused = false;

    void Start()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "Main Level 1")
        {
            return;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                return;

            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        pauseMenu.SetActive(true);
        optionsMenu.SetActive(false);
        helpMenu.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;

        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Select first button so UI works immediately
        if (EventSystem.current != null && firstPauseButton != null)
            EventSystem.current.SetSelectedGameObject(firstPauseButton);

        if (SoundFXManager.instance != null)
            SoundFXManager.instance.PauseAllSounds();
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
        helpMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // Hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Clear selection
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (SoundFXManager.instance != null)
            SoundFXManager.instance.ResumeAllSounds();
    }

    public void Options()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void Help()
    {
        pauseMenu.SetActive(false);
        helpMenu.SetActive(true);
    }

    public void home()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("Main Menu");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
