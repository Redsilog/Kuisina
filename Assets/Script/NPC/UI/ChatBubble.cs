using UnityEngine;
using TMPro;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class ChatBubble : MonoBehaviour
{
    public static GameObject chatBubblePrefab;

    public static void Create(Transform parent, Vector3 localPosition, IconType iconType, string text, GameObject prefab, float lifetime = 2f)
    {
        if (prefab == null)
        {
            Debug.LogError("❌ ChatBubble prefab not assigned to NPC!");
            return;
        }

        GameObject chatBubbleGO = Instantiate(prefab, parent);
        chatBubbleGO.transform.localPosition = localPosition;

        ChatBubble bubble = chatBubbleGO.GetComponent<ChatBubble>();
        bubble.Setup(iconType, text);

        if (lifetime > 0f)
            GameObject.Destroy(chatBubbleGO, lifetime);
    }

    public enum IconType
    {
        Dish
    }

    [SerializeField] private Sprite dishIconSprite;
    [SerializeField] private IconType iconType;

    private SpriteRenderer backgroundSpriteRenderer;
    private SpriteRenderer iconSpriteRenderer;
    private TextMeshPro textMeshPro;

    private void Awake()
    {
        backgroundSpriteRenderer = transform.Find("Background").GetComponent<SpriteRenderer>();
        iconSpriteRenderer = transform.Find("Icon").GetComponent<SpriteRenderer>();
        textMeshPro = transform.Find("Text").GetComponent<TextMeshPro>();
    }

    //billboarding
    private void LateUpdate()
    {
        if (Camera.main == null) return;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camUp = Camera.main.transform.up;

        transform.LookAt(transform.position + camForward, camUp);
    }

    private void Setup(IconType iconType, string text)
    {
        if (textMeshPro == null || backgroundSpriteRenderer == null)
        {
            Debug.LogError($"{name}: Setup failed because required components are missing.");
            return;
        }

        textMeshPro.SetText(text ?? "");
        textMeshPro.ForceMeshUpdate();

        Vector2 textSize = textMeshPro.GetRenderedValues(false);
        float textWidth = Mathf.Max(0.01f, textSize.x); 
        float textHeight = Mathf.Max(0.2f, textSize.y);
        
        Sprite iconSprite = GetIconSprite(iconType);
        bool hasIcon = iconSprite != null && iconSpriteRenderer != null;
        float iconWidth = 0f;

        if (hasIcon)
        {
            iconSpriteRenderer.sprite = iconSprite;
            Vector3 iconLossyScale = iconSpriteRenderer.transform.lossyScale;
            iconWidth = iconSprite.bounds.size.x * iconLossyScale.x;
        }
        else if (iconSpriteRenderer != null)
        {
            iconSpriteRenderer.sprite = null;
        }

        // spacing & padding
        float spacing = hasIcon ? 0.12f : 0f;
        Vector2 padding = new Vector2(0.2f, 0.2f);

        float totalWidth = padding.x * 2f + iconWidth + spacing + textWidth;
        float totalHeight = padding.y * 2f + textHeight;
        totalWidth = Mathf.Max(totalWidth, 0.3f); 
        totalHeight = Mathf.Max(totalHeight, 0.25f);

        backgroundSpriteRenderer.size = new Vector2(totalWidth, totalHeight);

        float contentWidth = hasIcon ? (iconWidth + spacing + textWidth) : textWidth;

        float contentStart = -contentWidth / 2f;

        float xIconCenter = hasIcon ? contentStart + iconWidth / 2f : 0f;
        float xTextCenter = hasIcon
            ? (contentStart + iconWidth + spacing + textWidth / 2f)
            : (contentStart + textWidth / 2f);

        if (iconSpriteRenderer != null)
        {
            iconSpriteRenderer.transform.localPosition = new Vector3(xIconCenter, 0f, 0f);
            iconSpriteRenderer.enabled = hasIcon;
        }

        textMeshPro.transform.localPosition = new Vector3(xTextCenter, 0f, 0f);

        textMeshPro.alignment = TextAlignmentOptions.Center;

    }

    private Sprite GetIconSprite(IconType iconType)
    {
        switch (iconType)
        {
            default:
            case IconType.Dish: return dishIconSprite;
        }
    }
}
