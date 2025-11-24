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

        // Show tutorial at start
        if (tutorialScreenLevel1 != null)
        {
            tutorialScreenLevel1.SetActive(true);
            isTutorialOpen = true;
        }

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
            timer.gameObject.SetActive(false);
            starCount.gameObject.SetActive(false);
            if (Keyboard.current[nextKey].wasPressedThisFrame)
                NextPage();

            if (Keyboard.current[prevKey].wasPressedThisFrame)
                PreviousPage();
            if (currentPage == tutorialPages.Length - 1)
            {
                start.SetActive(true);
            }
            else
            {
                start.SetActive(false);
            }

        }
    }

    private void ToggleTutorial()
    {
        isTutorialOpen = !isTutorialOpen;
        if (tutorialScreenLevel1 != null)
            tutorialScreenLevel1.SetActive(isTutorialOpen);
    }

    public void StartLevel()
    {
        tutorialScreenLevel1.SetActive(false);
        playLevel1.gameObject.SetActive(true);
        isTutorialOpen = false;
        timer.gameObject.SetActive(true);
        starCount.gameObject.SetActive(true);
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
