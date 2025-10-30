using UnityEngine;
using UnityEngine.UI;

public class NPCTimerUI : MonoBehaviour
{
    public NPCInteractable npc;
    public Transform followTarget;
    public Slider slider;
    public Vector3 offset;

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
        slider.value = (max <= 0f) ? 0f : Mathf.Clamp01(remaining / max);

        Vector3 worldPos = followTarget.position + offset;

        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
            ? canvas.worldCamera
            : Camera.main;

        if (cam == null) return;

        Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, (canvas.renderMode == RenderMode.ScreenSpaceCamera) ? cam : null, out localPoint);
        rectTransform.anchoredPosition = localPoint;
    }
}
