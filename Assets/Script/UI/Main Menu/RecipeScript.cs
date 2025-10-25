using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RecipeScript : MonoBehaviour
{
    [Header("UI Setup")]
    public Image displayImage;            // The UI Image component to show pages
    public Sprite[] pages;                // List of page images
    public Button nextButton;             // Optional button to go forward
    public Button prevButton;             // Optional button to go back

    [Header("Audio Setup")]
    public AudioSource audioSource;        // Audio source to play sound
    public AudioClip pageTurnSound;        // Page flip sound effect

    [Header("Optional Keyboard Input")]
    public Key nextKey = Key.RightArrow;
    public Key prevKey = Key.LeftArrow;

    private int currentPage = 0;

    void Start()
    {
        if (displayImage == null)
        {
            Debug.LogError("❌ No display Image assigned!");
            return;
        }

        if (pages.Length == 0)
        {
            Debug.LogWarning("⚠️ No pages assigned.");
            return;
        }

        // Hook up buttons if they exist
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousPage);

        ShowPage(currentPage);
    }

    void Update()
    {
        // Optional keyboard controls
        if (Keyboard.current[nextKey].wasPressedThisFrame)
            NextPage();

        if (Keyboard.current[prevKey].wasPressedThisFrame)
            PreviousPage();
    }

    public void NextPage()
    {
        if (pages.Length == 0) return;

        currentPage++;
        if (currentPage >= pages.Length)
            currentPage = pages.Length - 1; // stay at last page

        PlayPageSound();

        ShowPage(currentPage);
    }

    public void PreviousPage()
    {
        if (pages.Length == 0) return;

        currentPage--;
        if (currentPage < 0)
            currentPage = 0; // stay at first page

        PlayPageSound();
        ShowPage(currentPage);
    }

    private void PlayPageSound()
    {
        if (audioSource != null && pageTurnSound != null)
            audioSource.PlayOneShot(pageTurnSound);
    }

    private void ShowPage(int index)
    {
        displayImage.sprite = pages[index];
        Debug.Log($"📖 Showing page {index + 1}/{pages.Length}: {pages[index].name}");

        // Optionally disable buttons at edges
        if (nextButton != null) nextButton.interactable = index < pages.Length - 1;
        if (prevButton != null) prevButton.interactable = index > 0;
    }
}