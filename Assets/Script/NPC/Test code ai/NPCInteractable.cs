using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class NPCInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public NPCOrder1 npcOrder;
    [SerializeField] private NPCMovement npcMovement;

    [Header("Chat Bubble")]
    [SerializeField] private GameObject chatBubblePrefab;
    [SerializeField] private Transform chatBubbleSpawnPoint;
    [SerializeField] private float autoClearAfter = 2f;

    // Keep the dialogue lists HERE (NPCOrder1 no longer owns them)
    [Header("Dialog Lines (must match NPCOrder1.requestedItems)")]
    private List<string> requestLines = new List<string>();
    private List<string> thankLines = new List<string>();
    private List<string> wrongItemLines = new List<string>();

    [Header("Fallback Texts")]
    [SerializeField] private string defaultRequestFormat = "I’d like {0}, please.";
    [SerializeField] private string defaultThankFormat = "Thank you!";
    [SerializeField] private string defaultWrongFormat = "That’s not what I ordered. I asked for {0}.";
    [SerializeField] private string timeoutLine = "I’ll come back later.";

    [Header("Timers")]
    public float interactCooldown = 1.0f;
    public float initialWaitTime = 10f;
    public float extendedWaitTime = 10f;
    public float thankYouDelay = 5f;

    private bool playerInRange;
    private Transform currentInteractor;
    private bool waitingForInteraction;
    private float nextAllowedTime;
    private float interactionTimer;

    [SerializeField] private GameObject timerUIPrefab;
    private NPCTimerUI activeTimerUI;
    [SerializeField] private Canvas npcTimerCanvas;

    public bool IsTalking => waitingForInteraction || npcOrder?.HasActiveOrder == true;

    // Flag to track if NPC is resting
    private bool isResting = false;  // New variable to track resting state

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        npcOrder ??= GetComponent<NPCOrder1>();
        npcMovement ??= GetComponent<NPCMovement>();

        if (npcOrder != null)
            npcOrder.OnOrderFulfilled += HandleOrderFulfilled;

        npcTimerCanvas = GameObject.FindWithTag("NPCTimerCanvas").GetComponent<Canvas>();
    }

    void OnDestroy()
    {
        if (npcOrder != null)
            npcOrder.OnOrderFulfilled -= HandleOrderFulfilled;
    }

    void Update()
    {
        if (!waitingForInteraction) return;

        interactionTimer -= Time.deltaTime;
        if (interactionTimer <= 0f)
        {
            waitingForInteraction = false;
            HandleTimeout();
        }
    }

    // Called by NPCMovement when NPC reaches sit point
    public void StartWaitingForPlayer()
    {
        isResting = true;  // Set NPC to resting state when waiting for player
        waitingForInteraction = true;
        interactionTimer = initialWaitTime;
        SpawnTimerUI();
    }

    // Called by NPCMovement when NPC starts moving
    public void StartMoving()
    {
        isResting = false;  // NPC is no longer resting when moving
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        // Check if NPC is resting before allowing interaction
        if (!ctx.performed || !playerInRange || currentInteractor == null || !isResting)
            return;

        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + interactCooldown;

        var playerInventory = currentInteractor.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;

        bool acted = npcOrder && npcOrder.StartOrTryFulfill(playerInventory);

        if (!acted)
        {
            // Wrong item (active order, but mismatch)
            if (npcOrder && npcOrder.HasActiveOrder)
                ShowChat(BuildWrongLine());
            return;
        }

        // If we now have an active order, show the request and enter ordering state
        if (npcOrder.HasActiveOrder)
        {
            waitingForInteraction = true;
            interactionTimer = extendedWaitTime;

            ShowChat(BuildRequestLine());
            npcMovement?.BeginOrdering();
        }
    }

    private void HandleOrderFulfilled()
    {
        waitingForInteraction = false;
        ShowChat(BuildThankLine());

        if (npcOrder?.DeliveredDish != null)
        {
            var dishRef = npcOrder.DeliveredDish.GetComponent<DishReference>();
            if (dishRef != null)
                LevelManager.Instance.AddStars(dishRef.starsEarned);

            npcOrder.DeliveredDish = null;
        }

        StartCoroutine(ThankAndLeave());
        RemoveTimerUI();
    }

    private IEnumerator ThankAndLeave()
    {
        yield return new WaitForSeconds(thankYouDelay);
        npcMovement?.BeginThanking();
    }

    private void HandleTimeout()
    {
        ShowChat(timeoutLine);
        npcMovement?.StartLeaving();
        RemoveTimerUI();
    }

    private bool IsPlayerTag(Collider other)
    {
        return other.CompareTag("Player") || other.CompareTag("Player2");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerTag(other))
        {
            playerInRange = true;
            currentInteractor = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerTag(other) && other.transform == currentInteractor)
        {
            playerInRange = false;
            currentInteractor = null;
        }
    }

    // ---------- Dialogue builders ----------
    private string BuildRequestLine()
    {
        return FormatByIndex(
            requestLines,
            npcOrder?.CurrentRequestIndex ?? -1,
            defaultRequestFormat,
            npcOrder?.CurrentRequestName ?? ""
        );
    }

    private string BuildWrongLine()
    {
        return FormatByIndex(
            wrongItemLines,
            npcOrder?.CurrentRequestIndex ?? -1,
            defaultWrongFormat,
            npcOrder?.CurrentRequestName ?? ""
        );
    }

    private string BuildThankLine()
    {
        return FormatByIndex(
            thankLines,
            npcOrder?.CurrentRequestIndex ?? -1,
            defaultThankFormat,
            npcOrder?.CurrentRequestName ?? ""
        );
    }

    private static string FormatByIndex(List<string> list, int idx, string fallbackFmt, string itemName)
    {
        string line = null;
        if (list != null && idx >= 0 && idx < list.Count)
            line = list[idx];
        if (string.IsNullOrWhiteSpace(line))
            line = fallbackFmt;

        return line.Contains("{0}") ? string.Format(line, itemName) : line;
    }

    // timer
    public float GetRemainingTime()
    {
        return interactionTimer;
    }

    public float GetMaxTime()
    {
        return waitingForInteraction ? extendedWaitTime : initialWaitTime;
    }

    private void SpawnTimerUI()
    {
        if (activeTimerUI != null) return;

        Debug.Log("Spawning timer!");

        GameObject obj = Instantiate(timerUIPrefab, npcTimerCanvas.transform);
        activeTimerUI = obj.GetComponent<NPCTimerUI>();
        activeTimerUI.npc = this;
        activeTimerUI.followTarget = chatBubbleSpawnPoint;
    }

    private void RemoveTimerUI()
    {
        if (activeTimerUI != null)
        {
            Destroy(activeTimerUI.gameObject);
            activeTimerUI = null;
        }
    }

    // ---------- Chat bubble helper ----------
    private void ShowChat(string text)
    {
        if (!chatBubblePrefab || !chatBubbleSpawnPoint || string.IsNullOrWhiteSpace(text)) return;

        ChatBubble.Create(
            chatBubbleSpawnPoint,
            Vector3.zero,
            ChatBubble.IconType.Dish,
            text,
            chatBubblePrefab,
            autoClearAfter
        );
    }
}
