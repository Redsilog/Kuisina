using UnityEngine;
using UnityEngine.UI;

public class ChopTimerUI : MonoBehaviour
{
    [Header("References")]
    public ChoppingBoard choppingBoard;
    public Image fillImage;

    void Update()
    {
        fillImage.fillAmount = choppingBoard.ChopProgress01;

        bool show = choppingBoard.IsChoppingActive();
        fillImage.enabled = show;
    }
}