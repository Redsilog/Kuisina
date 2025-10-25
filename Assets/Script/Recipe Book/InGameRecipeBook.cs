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
    public GameObject Page14;

    [Header("Keys")]
    public Key openCloseKey = Key.Space;
    public Key prevKey = Key.X;
    public Key nextKey = Key.C;
    public Key openCloseKey2 = Key.RightShift;
    public Key prevKey2 = Key.LeftBracket;
    public Key nextKey2 = Key.RightBracket;


    private bool isBookOpen = false;
    private int currentPage = 1;

    void Start()
    {
        HideAllPages();
    }

    void Update()
    {
        // --- PLAYER 1 controls ---
        if (Keyboard.current[openCloseKey].wasPressedThisFrame)
        {
            ToggleBook();
        }

        if (isBookOpen)
        {
            if (Keyboard.current[nextKey].wasPressedThisFrame)
                NextPage();

            if (Keyboard.current[prevKey].wasPressedThisFrame)
                PreviousPage();
        }

        // --- PLAYER 2 controls ---
        if (Keyboard.current[openCloseKey2].wasPressedThisFrame)
        {
            ToggleBook();
        }

        if (isBookOpen)
        {
            if (Keyboard.current[nextKey2].wasPressedThisFrame)
                NextPage();

            if (Keyboard.current[prevKey2].wasPressedThisFrame)
                PreviousPage();
        }
    }

    private void ToggleBook()
    {
        isBookOpen = !isBookOpen;

        if (isBookOpen)
        {
            currentPage = 1;
            ShowOnly(currentPage);
            Debug.Log("📘 Book opened (Page 1)");
        }
        else
        {
            HideAllPages();
            Debug.Log("📕 Book closed");
        }
    }

    private void NextPage()
    {
        if (currentPage >= 14) return;

        currentPage++;
        ShowOnly(currentPage);
        Debug.Log($"➡️ Next Page: {currentPage}");
    }

    private void PreviousPage()
    {
        if (currentPage <= 1) return;

        currentPage--;
        ShowOnly(currentPage);
        Debug.Log($"⬅️ Previous Page: {currentPage}");
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
            case 14: if (Page14) Page14.SetActive(true); break;
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
        if (Page14) Page14.SetActive(false);
    }
}
