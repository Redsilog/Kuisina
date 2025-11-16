using UnityEngine;
using UnityEngine.InputSystem;

public class InGameRecipeBook : MonoBehaviour
{
    [Header("Pages (Assign Manually)")]
    public GameObject[] pages;

    [Header("Keys")]
    private Key openCloseKey = Key.Space;
    private Key prevKey = Key.Q;
    private Key nextKey = Key.E;

    private Key openCloseKey2 = Key.RightShift;
    private Key prevKey2 = Key.Comma;
    private Key nextKey2 = Key.Period;

    private bool player1InRange = false;
    private bool player2InRange = false;

    [Header("Highlight Settings")]
    public OutlineHighlighter highlighter;
    public string player1Tag = "Player";
    public string player2Tag = "Player2";

    private bool isBookOpen = false;
    private int currentPage = 1;

    void Start()
    {
        HideAllPages();
        if (highlighter == null)
            highlighter = GetComponent<OutlineHighlighter>();
    }

    void Update()
    {
        // --- PLAYER 1 controls ---
        if (player1InRange && Keyboard.current[openCloseKey].wasPressedThisFrame)
            ToggleBook();

        if (player1InRange && isBookOpen)
        {
            if (Keyboard.current[nextKey].wasPressedThisFrame)
                NextPage();

            if (Keyboard.current[prevKey].wasPressedThisFrame)
                PreviousPage();
        }

        // --- PLAYER 2 controls ---
        if (player2InRange && Keyboard.current[openCloseKey2].wasPressedThisFrame)
            ToggleBook();

        if (player2InRange && isBookOpen)
        {
            if (Keyboard.current[nextKey2].wasPressedThisFrame)
                NextPage();

            if (Keyboard.current[prevKey2].wasPressedThisFrame)
                PreviousPage();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(player1Tag))
            player1InRange = true;

        if (other.CompareTag(player2Tag))
            player2InRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(player1Tag))
            player1InRange = false;

        if (other.CompareTag(player2Tag))
            player2InRange = false;

        // Auto-close book if walking away
        if (isBookOpen)
            ToggleBook();
    }

    private void ToggleBook()
    {
        isBookOpen = !isBookOpen;

        if (isBookOpen)
        {
            ShowOnly(currentPage); // Show the last page
            Debug.Log($" Book opened (Page {currentPage})");
            TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.OpenRecipeBook);
        }
        else
        {
            HideAllPages();
            Debug.Log(" Book closed");
        }
    }

    private void NextPage()
    {
        currentPage = Mathf.Clamp(currentPage + 1, 1, pages.Length);
        ShowOnly(currentPage);
        Debug.Log($" Page: {currentPage}");
    }

    private void PreviousPage()
    {
        currentPage = Mathf.Clamp(currentPage - 1, 1, pages.Length);
        ShowOnly(currentPage);
        Debug.Log($" Page: {currentPage}");
    }

    private void ShowOnly(int page)
    {
        HideAllPages();

        if (pages.Length >= page && page > 0)
            pages[page - 1].SetActive(true);
    }

    private void HideAllPages()
    {
        foreach (GameObject page in pages)
            page.SetActive(false);
    }

    public void ToggleBookExternally()
    {
        ToggleBook();
    }
}
