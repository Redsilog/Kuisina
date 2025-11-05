using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class UIButtonToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea] public string tooltipMessage;                     // Text to display when hovering
    [SerializeField] private TextMeshProUGUI tooltipText;        // Assign this in Inspector
    [SerializeField] private Vector3 offset = new Vector3(0, -40, 0); // Adjust Y for placement under button

    private RectTransform tooltipRect;
    private RectTransform buttonRect;

    void Awake()
    {
        // Get references
        if (tooltipText == null)
        {
            var go = GameObject.Find("TooltipText");
            if (go != null)
                tooltipText = go.GetComponent<TextMeshProUGUI>();
        }

        if (tooltipText == null)
        {
            Debug.LogError("[UIButtonToolTip] No TooltipText assigned or found in scene.", this);
            return;
        }

        tooltipRect = tooltipText.GetComponent<RectTransform>();
        buttonRect = GetComponent<RectTransform>();
        tooltipText.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipText == null) return;

        tooltipText.text = tooltipMessage;
        tooltipText.gameObject.SetActive(true);

        // Position tooltip under the button
        tooltipRect.position = buttonRect.position + offset;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipText == null) return;

        tooltipText.gameObject.SetActive(false);
        tooltipText.text = "";
    }
}
