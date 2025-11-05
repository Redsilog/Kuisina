using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class UIButtonToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea] public string tooltipMessage; 
    [SerializeField] private TextMeshProUGUI tooltipText;  
    [SerializeField] private Vector3 offset = new Vector3(0, -40, 0);

    private RectTransform tooltipRect;
    private RectTransform buttonRect;
    private static TextMeshProUGUI staticTooltipText;

    void Awake()
    {
        tooltipRect = tooltipText.GetComponent<RectTransform>();
        buttonRect = GetComponent<RectTransform>();
        tooltipText.gameObject.SetActive(false);

        staticTooltipText = tooltipText;
    }
    

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipText == null) return;

        tooltipText.text = tooltipMessage;
        tooltipText.gameObject.SetActive(true);

        tooltipRect.position = buttonRect.position + offset;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    public static void HideTooltip()
    {
        if (staticTooltipText == null) return;
        staticTooltipText.gameObject.SetActive(false);
        staticTooltipText.text = "";
    }
}
