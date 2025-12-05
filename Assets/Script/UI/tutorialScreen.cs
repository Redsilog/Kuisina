using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class TutorialScreen : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private GameObject tutorialScreenLevel1;   // Parent panel for tutorial
    [SerializeField] private Button playLevel1;                 // Button to start the level
    [SerializeField] private Image displayImage;                // Image component to show pages
    [SerializeField] private Sprite[] tutorialPages;            // Tutorial pages as sprites
    [SerializeField] private Button nextButton;                 // Next page button
    [SerializeField] private Button prevButton;                 // Previous page button
    [SerializeField] GameObject timer, starCount;

    [Header("Audio Setup")]
    [SerializeField] private AudioClip pageTurnSound;
    [SerializeField, Range(0f, 1f)] private float pageVolume = 0.5f;

    [Header("Keyboard Input (Optional)")]
    [SerializeField] private Key nextKey = Key.RightArrow;
    [SerializeField] private Key prevKey = Key.LeftArrow;
    [SerializeField] private Key toggleTutorialKey = Key.T;

    private int currentPage = 0;
    private bool isTutorialOpen = false;
    public GameObject start;

    private void Start()
    {
        // Ensure play button is hidden at start
        if (playLevel1 != null)
            playLevel1.gameObject.SetActive(false);

        // Show tutorial at start and PAUSE the game
        if (tutorialScreenLevel1 != null)
        {
            tutorialScreenLevel1.SetActive(true);
            isTutorialOpen = true;
            Time.timeScale = 0f;   // <- NPCs & gameplay paused here

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Hide gameplay UI at start
        if (timer != null) timer.gameObject.SetActive(false);
        if (starCount != null) starCount.gameObject.SetActive(false);

        // Hook up play button to start the level
        if (playLevel1 != null)
            playLevel1.onClick.AddListener(StartLevel);

        // Hook up page buttons
        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);
        if (prevButton != null)
            prevButton.onClick.AddListener(PreviousPage);

        ShowPage(currentPage);
    }

    private void Update()
    {
        if (Keyboard.current[toggleTutorialKey].wasPressedThisFrame)
        {
            ToggleTutorial();
        }

        if (isTutorialOpen)
        {
            if (timer != null) timer.gameObject.SetActive(false);
            if (starCount != null) starCount.gameObject.SetActive(false);

            if (Keyboard.current[nextKey].wasPressedThisFrame)
                NextPage();

            if (Keyboard.current[prevKey].wasPressedThisFrame)
                PreviousPage();

            if (currentPage == tutorialPages.Length - 1)
            {
                if (start != null) start.SetActive(true);
            }
            else
            {
                if (start != null) start.SetActive(false);
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void ToggleTutorial()
    {
        isTutorialOpen = !isTutorialOpen;

        if (tutorialScreenLevel1 != null)
            tutorialScreenLevel1.SetActive(isTutorialOpen);

        if (isTutorialOpen)
        {
            // Open tutorial  pause game & hide HUD
            Time.timeScale = 0f;
            if (timer != null) timer.gameObject.SetActive(false);
            if (starCount != null) starCount.gameObject.SetActive(false);
        }
        else
        {
            // Close tutorial  resume game & show HUD
            Time.timeScale = 1f;   // <- NPCs resume here
            if (timer != null) timer.gameObject.SetActive(true);
            if (starCount != null) starCount.gameObject.SetActive(true);
        }
    }

    public void StartLevel()
    {
        // Close tutorial
        if (tutorialScreenLevel1 != null)
            tutorialScreenLevel1.SetActive(false);

        if (playLevel1 != null)
            playLevel1.gameObject.SetActive(true);

        isTutorialOpen = false;

        // Show HUD and RESUME the game
        if (timer != null) timer.gameObject.SetActive(true);
        if (starCount != null) starCount.gameObject.SetActive(true);

        Time.timeScale = 1f;       //  NPCs start moving again
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void NextPage()
    {
        if (tutorialPages.Length == 0) return;

        currentPage++;
        if (currentPage >= tutorialPages.Length)
            currentPage = tutorialPages.Length - 1;

        PlayPageSound();
        ShowPage(currentPage);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void PreviousPage()
    {
        if (tutorialPages.Length == 0) return;

        currentPage--;
        if (currentPage < 0)
            currentPage = 0;

        PlayPageSound();
        ShowPage(currentPage);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void PlayPageSound()
    {
        if (pageTurnSound != null && SoundFXManager.instance != null)
            SoundFXManager.instance.PlaySoundFXClip(pageTurnSound, transform, pageVolume);
    }

    private void ShowPage(int index)
    {
        if (displayImage != null && tutorialPages.Length > 0)
        {
            displayImage.sprite = tutorialPages[index];
            if (nextButton != null) nextButton.interactable = index < tutorialPages.Length - 1;
            if (prevButton != null) prevButton.interactable = index > 0;
        }
    }
}
