using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

    [System.Serializable]
    public class NPCDialogueSet
    {
        public string dishName;
        [Header("Random Lines")]
        [TextArea] public List<string> requestLines = new List<string>();
        [TextArea] public List<string> wrongLines = new List<string>();
        [TextArea] public List<string> thankLines = new List<string>();
        [TextArea] public List<string> midComboLines = new List<string>(); // NEW: lines after partial combo delivery
    }

    [Header("Per-Dish Custom Dialogues")]
    [SerializeField] private List<NPCDialogueSet> customDialogues = new List<NPCDialogueSet>();

    [Header("Fallback Texts (Timeout Only)")]
    [SerializeField] private string timeoutLine = "Bagal naman dito.";

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
    private bool inExtendedPhase = false;
    private bool isResting = false;
    private bool isLeaving = false; // prevents interaction during leave state

    private string selectedRequestLine;
    public bool IsTalking => waitingForInteraction || npcOrder?.HasActiveOrder == true;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        npcOrder ??= GetComponent<NPCOrder1>();
        npcMovement ??= GetComponent<NPCMovement>();

        if (npcOrder != null)
        {
            npcOrder.OnOrderFulfilled += HandleOrderFulfilled;
            npcOrder.OnItemAccepted += HandleItemAccepted; // NEW: per-item handler
        }

        var canvasObj = GameObject.FindWithTag("NPCTimerCanvas");
        if (canvasObj != null) npcTimerCanvas = canvasObj.GetComponent<Canvas>();
    }

    void OnDestroy()
    {
        if (npcOrder != null)
        {
            npcOrder.OnOrderFulfilled -= HandleOrderFulfilled;
            npcOrder.OnItemAccepted -= HandleItemAccepted;
        }
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

    public void StartWaitingForPlayer()
    {
        isResting = true;
        isLeaving = false; // reset leave flag
        waitingForInteraction = true;
        inExtendedPhase = false;
        interactionTimer = initialWaitTime;
        SpawnTimerUI();

        if (npcOrder != null && npcOrder.HasActiveOrder)
            ShowChat(BuildRequestLine());
    }

    public void StartMoving()
    {
        isResting = false;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !playerInRange || currentInteractor == null || !isResting || isLeaving)
            return;

        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + interactCooldown;

        var playerInventory = currentInteractor.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;

        bool acted = npcOrder && npcOrder.StartOrTryFulfill(playerInventory);

        if (!acted)
        {
            if (npcOrder && npcOrder.HasActiveOrder)
            {
                if (IsWrongPrefab(playerInventory))
                    ShowChat(BuildWrongLine());
                else
                    ShowChat(selectedRequestLine);
            }
            return;
        }

        // Only show the request line when a brand new order was created
        if (npcOrder && npcOrder.LastInteractionResult == OrderInteractionResult.OrderCreated)
        {
            waitingForInteraction = true;
            inExtendedPhase = true;
            interactionTimer = extendedWaitTime;

            if (string.IsNullOrEmpty(selectedRequestLine))
                selectedRequestLine = BuildRequestLine();

            ShowChat(selectedRequestLine);
            npcMovement?.BeginOrdering();
        }
    }

    private bool IsWrongPrefab(PlayerInventory playerInventory)
    {
        if (playerInventory.heldVisual == null) return false;

        string heldItemName = CleanName(playerInventory.heldVisual.name);

        if (npcOrder != null && npcOrder.requestedItems.Count > 0)
        {
            foreach (var requestedItem in npcOrder.requestedItems)
                if (heldItemName.Equals(CleanName(requestedItem.name), StringComparison.OrdinalIgnoreCase))
                    return false;

            foreach (var combo in npcOrder.prefabCombos)
                foreach (var comboItem in combo.items)
                    if (heldItemName.Equals(CleanName(comboItem.name), StringComparison.OrdinalIgnoreCase))
                        return false;
        }

        return true;
    }

    private string CleanName(string name) => string.IsNullOrEmpty(name) ? "" : name.Replace("(Clone)", "").Trim();

    private void HandleOrderFulfilled()
    {
        if (npcOrder?.DeliveredDish != null)
        {
            var dishRef = npcOrder.DeliveredDish.GetComponent<DishReference>();
            if (dishRef != null)
                LevelManager.Instance.AddStars(dishRef.starsEarned);
        }

        waitingForInteraction = false;
        ShowChat(BuildThankLine());
        npcOrder.DeliveredDish = null;
        StartCoroutine(ThankAndLeave());
        RemoveTimerUI();
    }

    private IEnumerator ThankAndLeave()
    {
        yield return new WaitForSeconds(thankYouDelay);
        isLeaving = true; // Mark NPC as leaving — disable interaction
        npcMovement?.BeginThanking();
    }

    private void HandleTimeout()
    {
        ShowChat(timeoutLine);
        isLeaving = true; // Prevent interaction once NPC decides to leave
        npcMovement?.StartLeaving();
        RemoveTimerUI();
    }

    private bool IsPlayerTag(Collider other) => other.CompareTag("Player") || other.CompareTag("Player2");

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

    // ---------------- Dialogue Builders (NO FALLBACKS) ----------------
    private string BuildRequestLine()
    {
        var custom = GetDialogueSet();
        if (custom != null && custom.requestLines != null && custom.requestLines.Count > 0)
        {
            return custom.requestLines[UnityEngine.Random.Range(0, custom.requestLines.Count)];
        }

        // No custom line → no text (no chat bubble)
        return null;
    }

    private string BuildWrongLine()
    {
        var custom = GetDialogueSet();
        if (custom != null && custom.wrongLines != null && custom.wrongLines.Count > 0)
        {
            return custom.wrongLines[UnityEngine.Random.Range(0, custom.wrongLines.Count)];
        }

        // No custom line → no text
        return null;
    }

    private string BuildThankLine()
    {
        var custom = GetDialogueSet();
        if (custom != null && custom.thankLines != null && custom.thankLines.Count > 0)
        {
            return custom.thankLines[UnityEngine.Random.Range(0, custom.thankLines.Count)];
        }

        // No custom line → no text
        return null;
    }

    private NPCDialogueSet GetDialogueSet()
    {
        if (npcOrder == null || string.IsNullOrWhiteSpace(npcOrder.CurrentRequestName)) return null;
        return customDialogues.FirstOrDefault(d =>
            npcOrder.CurrentRequestName.IndexOf(d.dishName, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // NEW: Get dialogue set based on a specific delivered dish name
    private NPCDialogueSet GetDialogueSetForName(string dishName)
    {
        if (string.IsNullOrWhiteSpace(dishName)) return null;
        return customDialogues.FirstOrDefault(d =>
            dishName.IndexOf(d.dishName, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // ---------------- Timer UI ----------------
    public float GetRemainingTime() => interactionTimer;
    public float GetMaxTime() => inExtendedPhase ? extendedWaitTime : initialWaitTime;

    private void SpawnTimerUI()
    {
        if (activeTimerUI != null || timerUIPrefab == null || npcTimerCanvas == null) return;

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

    // NEW: called every time a correct item is delivered
    private void HandleItemAccepted(string deliveredName, bool isComplete)
    {
        // If order is already complete, let HandleOrderFulfilled handle the thank-you flow
        if (isComplete) return;

        if (npcOrder == null || npcOrder.OriginalOrderCount <= 1)
            return; // Only care about combos

        // Make sure we have a base request line to fall back to (custom only)
        if (string.IsNullOrEmpty(selectedRequestLine))
            selectedRequestLine = BuildRequestLine();

        var set = GetDialogueSetForName(deliveredName);

        // If this dish has special mid-combo lines (e.g. main dish like Adobong Puti)
        if (set != null && set.midComboLines != null && set.midComboLines.Count > 0)
        {
            string line = set.midComboLines[UnityEngine.Random.Range(0, set.midComboLines.Count)];
            ShowChat(line);
        }
        else
        {
            // Side dish or no special mid-combo text → repeat the original order (if any)
            ShowChat(selectedRequestLine);
        }
    }
}
