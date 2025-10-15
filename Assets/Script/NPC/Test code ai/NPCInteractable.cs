using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Dialog")]
    [TextArea] public string[] npcDialogLines;       // List of dialog lines
    [SerializeField] private TextMeshProUGUI uiText; // TextMeshPro component for displaying dialog

    [Header("Order Interaction")]
    [SerializeField] private NPCOrder1 npcOrder; // Reference to NPCOrder script

    [Tooltip("Cooldown for NPC interaction")]
    public float interactCooldown = 1.0f;

    // runtime variables
    private bool playerInRange;
    private Transform currentInteractor;
    private float nextAllowedTime = 0f;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    // ---- Proximity Handling ------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            currentInteractor = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.transform == currentInteractor)
        {
            playerInRange = false;
            currentInteractor = null;
        }
    }

    // ---- Interaction Handling ------------------------------------------------
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !playerInRange || currentInteractor == null) return;

        // Cooldown gate
        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + interactCooldown;

        // 1) Show random dialog line
        ShowRandomDialog();

        // 2) Forward to NPCOrder for item checking and feedback
        if (npcOrder != null)
        {
            npcOrder.OnInteract(ctx);
        }
    }

    // ---- Handle dialog (pick a random one) --------------------------------
    void ShowRandomDialog()
    {
        if (npcDialogLines.Length > 0)
        {
            int randomIndex = Random.Range(0, npcDialogLines.Length);
            string randomLine = npcDialogLines[randomIndex];

            if (uiText)
                uiText.text = randomLine;

            // Auto-clear after a short period
            Invoke(nameof(ClearDialog), 2f); // clear after 2 seconds
        }
    }

    void ClearDialog()
    {
        if (uiText)
            uiText.text = "";
    }
}
