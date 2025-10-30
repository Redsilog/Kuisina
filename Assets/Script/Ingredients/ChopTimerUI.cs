using UnityEngine;
using UnityEngine.UI;

public class ChopTimerUI : MonoBehaviour
{
    [Header("References")]
    public ChoppingBoard choppingBoard;
    public Image fillImage;
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (choppingBoard == null || fillImage == null) return;

        transform.position = choppingBoard.transform.position + offset;

        if (mainCam != null)
            transform.LookAt(mainCam.transform);

        fillImage.fillAmount = choppingBoard.ChopProgress01;

        bool show = choppingBoard != null && choppingBoard.IsChoppingActive();
        fillImage.enabled = show;
    }
}