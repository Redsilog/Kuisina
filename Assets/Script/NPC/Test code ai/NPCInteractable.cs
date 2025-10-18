using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Order (logic)")]
    [SerializeField] private NPCOrder1 npcOrder;  // Reference to NPCOrder1

    [Header("Head Look (optional)")]
    [SerializeField] private NPCHeadLookAt headLookAt;

    [Header("Movement Ref")]
    [SerializeField] private NPCMovement npcMovement;

    [Header("Chat Bubble")]
    [SerializeField] private GameObject chatBubblePrefab;
    [SerializeField] private Transform chatBubbleSpawnPoint;
    [SerializeField] private float autoClearAfter = 2f;

    [Header("Dialog (index MUST match NPCOrder1.requestedItems)")]
    public List<string> requestLines = new List<string>();    // shown when order starts
    public List<string> thankLines = new List<string>();    // shown when fulfilled
    public List<string> wrongItemLines = new List<string>();  // shown when wrong item given

    [Header("Fallback Lines")]
    [SerializeField] private string defaultRequestFormat = "I’d like {0}, please.";
    [SerializeField] private string defaultThankFormat = "Thank you!";
    [SerializeField] private string defaultWrongFormat = "That’s not what I ordered. I asked for {0}.";
    [SerializeField] private string timeoutLine = "I’ll come back later.";

    [Header("Timing")]
    public float interactCooldown = 1.0f;
    public float initialWaitTime = 10f; // SIT phase
    public float extendedWaitTime = 10f; // ORDER phase after interact
    public float thankYouDelay = 5f;  // dwell after successful delivery

    // runtime
    private bool playerInRange;
    private Transform currentInteractor;
    private float nextAllowedTime = 0f;

    private bool waitingForInteraction = false;
    private float interactionTimer = 0f;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!npcMovement) npcMovement = GetComponent<NPCMovement>();
        if (!npcOrder) npcOrder = GetComponent<NPCOrder1>();

        if (npcOrder != null)
            npcOrder.OnOrderFulfilled += HandleOrderFulfilled;
    }

    void OnDestroy()
    {
        if (npcOrder != null)
            npcOrder.OnOrderFulfilled -= HandleOrderFulfilled;
    }

    // Called by NPCMovement when it reaches the sit/wait spot
    public void StartWaitingForPlayer()
    {
        waitingForInteraction = true;
        interactionTimer = initialWaitTime; // SIT phase (no order yet)
    }

    void Update()
    {
        if (!waitingForInteraction) return;

        interactionTimer -= Time.deltaTime;
        if (interactionTimer <= 0f)
        {
            waitingForInteraction = false;

            // timeout: leave
            if (!string.IsNullOrEmpty(timeoutLine))
                ShowChat(timeoutLine);

            if (npcMovement) npcMovement.StartLeaving();
            if (headLookAt) headLookAt.StopLooking();
        }
    }

    // Proximity
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            currentInteractor = other.transform;
            Debug.Log("[NPCInteractable] Player entered interaction range.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if ((other.CompareTag("Player") || other.CompareTag("Player2")) && other.transform == currentInteractor)
        {
            playerInRange = false;
            currentInteractor = null;
            Debug.Log("[NPCInteractable] Player exited interaction range.");
        }
    }

    // INTERACT: start order if none, or try fulfill if active
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !playerInRange || currentInteractor == null) return;
        if (Time.time < nextAllowedTime) return;  // Cooldown check
        nextAllowedTime = Time.time + interactCooldown;

        Debug.Log("[NPCInteractable] Interact triggered by player.");

        var inv = currentInteractor.GetComponent<PlayerInventory>();
        bool acted = npcOrder ? npcOrder.StartOrTryFulfill(inv) : false;

        if (!acted)
        {
            // Wrong item, show wrong item message
            if (npcOrder && npcOrder.HasActiveOrder)
                ShowChat(npcOrder.GetWrongLine());
            return;
        }

        if (npcOrder.HasActiveOrder)
        {
            waitingForInteraction = true;
            interactionTimer = extendedWaitTime;

            // Show the request dialog for the item
            ShowChat(npcOrder.GetRequestLine());

            // Make the NPC look at the player
            if (headLookAt != null)
            {
                headLookAt.SetPlayerTransform(currentInteractor);
                headLookAt.LookAtTransform(currentInteractor, 1.6f);
                headLookAt.EnableFollowing(true);
            }
        }
    }

    // Fired by NPCOrder1 when the correct item was delivered
    void HandleOrderFulfilled()
    {
        waitingForInteraction = false;

        ShowChat(npcOrder.GetThankLine());
        //instantiate food here 
        StartCoroutine(ThankAndLeave());
    }

    IEnumerator ThankAndLeave()
    {
        yield return new WaitForSeconds(thankYouDelay);
        //destroy instantiated food
        if (npcMovement) npcMovement.StartLeaving();
        if (headLookAt) headLookAt.StopLooking();
    }

    // ------- Helpers -------
    string FormatByIndex(List<string> list, int idx, string fallbackFmt, string itemName)
    {
        string line = null;
        if (list != null && idx >= 0 && idx < list.Count) line = list[idx];
        if (string.IsNullOrWhiteSpace(line)) line = fallbackFmt;
        return line.Contains("{0}") ? string.Format(line, itemName) : line;
    }

    void ShowChat(string text)
    {
        Debug.Log($"[NPCInteractable] Showing chat bubble: {text}");

        if (!chatBubblePrefab || !chatBubbleSpawnPoint || string.IsNullOrWhiteSpace(text)) return;

        ChatBubble.Create(
            chatBubbleSpawnPoint,
            Vector3.zero,
            ChatBubble.IconType.Dish, // adjust icon type if you want
            text,
            chatBubblePrefab,
            autoClearAfter
        );
    }
}
