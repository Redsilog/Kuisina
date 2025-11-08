using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TutorialScript : MonoBehaviour
{
    [Header("UI Setup")]
    public GameObject tutorialPanel;   // Parent UI panel for the tutorial
    public Image displayImage;         // The UI Image component to show pages
    public Sprite[] tutorialPages;     // List of tutorial page images (7 pages)
    public Button nextButton;          // Button to go forward
    public Button prevButton;          // Button to go back

    [Header("Audio Setup")]
    public AudioClip pageTurnSound;    // Optional page turn sound
    public float pageVolume = 0.5f;

    [Header("Optional Keyboard Input")]
    public Key nextKey = Key.RightArrow;
    public Key prevKey = Key.LeftArrow;
    public Key toggleTutorialKey = Key.T; // Press T to open/close the tutorial

    private int currentPage = 0;
    private bool isTutorialOpen = false;

    void Start()
    {
        if (displayImage == null)
        {
            Debug.LogError("❌ No display Image assigned!");
            return;
        }

        if (tutorialPages.Length == 0)
        {
            Debug.LogWarning("⚠️ No tutorial pages assigned.");
            return;
        }


        // Hook up button listeners
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousPage);

        ShowPage(currentPage);

        // Hide tutorial at start
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    void Update()
    {
        // Toggle tutorial visibility
        if (Keyboard.current[toggleTutorialKey].wasPressedThisFrame)
        {
            ToggleTutorial();
        }

        // Only allow navigation if tutorial is open
        if (isTutorialOpen)
        {
            if (Keyboard.current[nextKey].wasPressedThisFrame)
                NextPage();

            if (Keyboard.current[prevKey].wasPressedThisFrame)
                PreviousPage();
        }
    }

    private void ToggleTutorial()
    {
        isTutorialOpen = !isTutorialOpen;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(isTutorialOpen);

        Debug.Log(isTutorialOpen ? "📘 Tutorial opened." : "📕 Tutorial closed.");
    }

    public void NextPage()
    {
        if (tutorialPages.Length == 0) return;

        currentPage++;
        if (currentPage >= tutorialPages.Length)
            currentPage = tutorialPages.Length - 1; // stay at last page

        PlayPageSound();
        ShowPage(currentPage);

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void PreviousPage()
    {
        if (tutorialPages.Length == 0) return;

        currentPage--;
        if (currentPage < 0)
            currentPage = 0; // stay at first page

        PlayPageSound();
        ShowPage(currentPage);

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void PlayPageSound()
    {
        if (pageTurnSound != null && SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundFXClip(pageTurnSound, transform, pageVolume);
        }
    }

    private void ShowPage(int index)
    {
        displayImage.sprite = tutorialPages[index];
        Debug.Log($"📖 Showing tutorial page {index + 1}/{tutorialPages.Length}: {tutorialPages[index].name}");

        // Disable buttons at the start or end
        if (nextButton != null) nextButton.interactable = index < tutorialPages.Length - 1;
        if (prevButton != null) prevButton.interactable = index > 0;
    }
}
