using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;          // optional
    [SerializeField] private NPCHeadLookAt headLookAt;   // required
    [SerializeField] private TextMeshProUGUI uiText;     // world-space or screen-space TMP text

    [Header("Speech")]
    [TextArea] public string lineOnInteract = "Hello there!";
    public float autoClearAfter = 2f;                    // seconds; 0 = don't auto-clear
    public float lookAtYOffset = 1.6f;                   // eye level

    // runtime
    private Transform currentInteractor;
    private bool playerInRange;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!headLookAt) headLookAt = GetComponent<NPCHeadLookAt>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnDisable()
    {
        headLookAt?.StopLooking();
        CancelInvoke(nameof(ClearText));
    }

    // ---- proximity ---------------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            currentInteractor = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if ((other.CompareTag("Player") || other.CompareTag("Player2")) && other.transform == currentInteractor)
        {
            playerInRange = false;
            currentInteractor = null;
            headLookAt?.StopLooking();
        }
    }

    // ---- interaction (forwarded from PlayerController.OnInteract) ----------
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (!playerInRange || currentInteractor == null) return;

        // 1) look at the player
        headLookAt?.LookAtTransform(currentInteractor, lookAtYOffset);

        // 2) show UI text instead of a chat bubble
        if (uiText) uiText.text = lineOnInteract;
        if (autoClearAfter > 0f) { CancelInvoke(nameof(ClearText)); Invoke(nameof(ClearText), autoClearAfter); }

        // 3) optional talk animation
        if (animator) animator.SetTrigger("Talk");
    }

    void ClearText()
    {
        if (uiText) uiText.text = "";
    }
}
