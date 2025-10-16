using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Dialog")]
    [TextArea] public string[] npcDialogLines;       // List of dialog lines
    [SerializeField] private TextMeshProUGUI uiText; // TextMeshPro component for displaying dialog

    
    [Header("Chat Bubble")]
    [SerializeField] private GameObject chatBubblePrefab;
    [SerializeField] private Transform chatBubbleSpawnPoint;

    [Header("Order Interaction")]
    [SerializeField] private NPCOrder1 npcOrder; // Reference to NPCOrder script

    [Header("Timing")]
    public float autoClearAfter = 2f; // clear UI/chat bubble
    public float interactCooldown = 1.0f; // prevent spam interaction

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
        }
    }

    // ---- Interaction Handling ------------------------------------------------
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !playerInRange || currentInteractor == null) return;

        // Cooldown gate
        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + interactCooldown;

        string randomLine = GetRandomDialog();
        if (!string.IsNullOrEmpty(randomLine))
        {
            ShowChat(randomLine);
            npcOrder.RandomizeOrder();
            npcOrder.OnInteract(ctx);
        }

    }

    private string GetRandomDialog()
    {
        if (npcDialogLines == null || npcDialogLines.Length == 0)
            return null;

        int randomIndex = Random.Range(0, npcDialogLines.Length);
        return npcDialogLines[randomIndex];
    }

    private void ShowChat(string text)
    {
        if (chatBubblePrefab && chatBubbleSpawnPoint)
        {
            ChatBubble.Create(
                chatBubbleSpawnPoint,
                Vector3.zero,
                ChatBubble.IconType.Dish, // change icon type if needed
                text,
                chatBubblePrefab,
                autoClearAfter
            );
        }
    }
}
