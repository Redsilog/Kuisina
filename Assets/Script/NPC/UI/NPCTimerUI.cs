using UnityEngine;
using UnityEngine.UI;

public class NPCTimerUI : MonoBehaviour
{
    public NPCInteractable npc;
    public Transform followTarget;
    public Slider slider;
    public Vector3 offset;

    //sprites
    public Image emotionIcon;
    public Sprite happySprite;
    public Sprite neutralSprite;
    public Sprite sadSprite;


    Canvas canvas;
    RectTransform rectTransform;
    private Camera cam;
    private Vector3 lastScreenPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }
    void Start()
    {
        cam = Camera.main;
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }
    
    void LateUpdate()
    {
        if (followTarget == null || cam == null)
            return;

        // Convert world position (NPC's head) to screen position
        Vector3 screenPos = cam.WorldToScreenPoint(followTarget.position + offset);

        // Set the UI element’s position directly in screen space
        transform.position = screenPos;
    }

    void Update()
    {
        if (npc == null || followTarget == null || rectTransform == null || canvas == null) return;

        float remaining = npc.GetRemainingTime();
        float max = npc.GetMaxTime();

        // Hide or deactivate when timer ends
        if (remaining <= 0f || max <= 0f)
        {
            slider.value = 0f;
            emotionIcon.enabled = false;
            return;
        }

        slider.value = Mathf.Clamp01(remaining / max);

        UpdateEmotionIcon();

        Vector3 worldPos = followTarget.position + offset;

        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
            ? canvas.worldCamera
            : Camera.main;

        if (cam == null) return;

        Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
        RectTransform canvasRect = canvas.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out Vector2 localPoint))
            rectTransform.anchoredPosition = localPoint;
    }
    void UpdateEmotionIcon()
    {
        if (emotionIcon == null) return;

        float ratio = npc.GetRemainingTime() / npc.GetMaxTime();

        if (ratio > 0.7f)
            emotionIcon.sprite = happySprite;
        else if (ratio > 0.3f)
            emotionIcon.sprite = neutralSprite;
        else
            emotionIcon.sprite = sadSprite;

        emotionIcon.enabled = true;
    }
}
