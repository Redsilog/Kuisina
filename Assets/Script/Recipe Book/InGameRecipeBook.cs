using UnityEngine;
using UnityEngine.InputSystem;

public class InGameRecipeBook : MonoBehaviour
{
    [Header("Pages (Assign Manually)")]
    public GameObject Page1;
    public GameObject Page2;
    public GameObject Page3;
    public GameObject Page4;
    public GameObject Page5;
    public GameObject Page6;
    public GameObject Page7;
    public GameObject Page8;
    public GameObject Page9;
    public GameObject Page10;
    public GameObject Page11;
    public GameObject Page12;
    public GameObject Page13;

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
    private const int MIN_PAGE = 1;
    private const int MAX_PAGE = 13;

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
            ShowOnly(currentPage);
            Debug.Log("📘 Book opened (Page 1)");
            TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.OpenRecipeBook);

        }
        else
        {
            HideAllPages();
            Debug.Log("📕 Book closed");
        }
    }


    private void NextPage()
    {
        currentPage = Mathf.Clamp(currentPage + 1, MIN_PAGE, MAX_PAGE);
        ShowOnly(currentPage);
        Debug.Log($"➡️ Page: {currentPage}");
    }

    private void PreviousPage()
    {
        currentPage = Mathf.Clamp(currentPage - 1, MIN_PAGE, MAX_PAGE);
        ShowOnly(currentPage);
        Debug.Log($"⬅️ Page: {currentPage}");
    }

    // Shows one page, hides the rest
    private void ShowOnly(int page)
    {
        HideAllPages();

        switch (page)
        {
            case 1: if (Page1) Page1.SetActive(true); break;
            case 2: if (Page2) Page2.SetActive(true); break;
            case 3: if (Page3) Page3.SetActive(true); break;
            case 4: if (Page4) Page4.SetActive(true); break;
            case 5: if (Page5) Page5.SetActive(true); break;
            case 6: if (Page6) Page6.SetActive(true); break;
            case 7: if (Page7) Page7.SetActive(true); break;
            case 8: if (Page8) Page8.SetActive(true); break;
            case 9: if (Page9) Page9.SetActive(true); break;
            case 10: if (Page10) Page10.SetActive(true); break;
            case 11: if (Page11) Page11.SetActive(true); break;
            case 12: if (Page12) Page12.SetActive(true); break;
            case 13: if (Page13) Page13.SetActive(true); break;
        }
    }

    private void HideAllPages()
    {
        if (Page1) Page1.SetActive(false);
        if (Page2) Page2.SetActive(false);
        if (Page3) Page3.SetActive(false);
        if (Page4) Page4.SetActive(false);
        if (Page5) Page5.SetActive(false);
        if (Page6) Page6.SetActive(false);
        if (Page7) Page7.SetActive(false);
        if (Page8) Page8.SetActive(false);
        if (Page9) Page9.SetActive(false);
        if (Page10) Page10.SetActive(false);
        if (Page11) Page11.SetActive(false);
        if (Page12) Page12.SetActive(false);
        if (Page13) Page13.SetActive(false);
    }
    public void ToggleBookExternally()
    {
        ToggleBook();
    }
}
