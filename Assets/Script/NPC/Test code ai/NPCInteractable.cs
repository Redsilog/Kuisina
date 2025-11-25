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
        [TextArea] public List<string> midComboLines = new List<string>(); // lines after partial combo delivery
    }

    [Header("Per-Dish Custom Dialogues")]
    [SerializeField] private List<NPCDialogueSet> customDialogues = new List<NPCDialogueSet>();

    [Header("Fallback Texts")]
    // Fallbacks removed for request & wrong; only thank + timeout kept
    [SerializeField] private string thankSingleFormat = "Salamat!";
    [SerializeField] private string thankComboFormat = "Salamat sa {0} at {1}!";
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
            npcOrder.OnItemAccepted += HandleItemAccepted; // per-item handler
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
        {
            // Build and store the request line so we can reuse it later
            selectedRequestLine = BuildRequestLine();
            ShowChat(selectedRequestLine);
        }
    }

    public void StartMoving()
    {
        isResting = false;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !playerInRange || currentInteractor == null || !isResting)
            return;

        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + interactCooldown;

        var playerInventory = currentInteractor.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;

        bool acted = npcOrder && npcOrder.StartOrTryFulfill(playerInventory);

        // If no state change happened (no item delivered / no new order)
        if (!acted)
        {
            if (npcOrder && npcOrder.HasActiveOrder)
            {
                if (IsWrongPrefab(playerInventory))
                {
                    ShowChat(BuildWrongLine());
                }
                else
                {
                    // make sure we have a request line to repeat
                    if (string.IsNullOrWhiteSpace(selectedRequestLine))
                    {
                        Debug.Log($"[{name}] OnInteract: selectedRequestLine was empty, rebuilding.");
                        selectedRequestLine = BuildRequestLine();
                    }

                    ShowChat(selectedRequestLine);
                }
            }
            return;
        }

        // Only show the request line when a brand new order was created
        if (npcOrder && npcOrder.LastInteractionResult == OrderInteractionResult.OrderCreated)
        {
            waitingForInteraction = true;
            inExtendedPhase = true;
            interactionTimer = extendedWaitTime;

            if (string.IsNullOrWhiteSpace(selectedRequestLine))
            {
                Debug.Log($"[{name}] OrderCreated: selectedRequestLine was empty, rebuilding.");
                selectedRequestLine = BuildRequestLine();
            }

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

    private string CleanName(string name) =>
        string.IsNullOrEmpty(name) ? "" : name.Replace("(Clone)", "").Trim();

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

    private bool IsPlayerTag(Collider other) =>
        other.CompareTag("Player") || other.CompareTag("Player2");

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

    // ---------------- Dialogue Builders ----------------
    private string BuildRequestLine()
    {
        Debug.Log($"[{name}] BuildRequestLine: CurrentRequestName = '{npcOrder?.CurrentRequestName}'");

        var custom = GetDialogueSet();

        if (custom == null)
        {
            Debug.LogWarning($"[{name}] BuildRequestLine: NO dialogue set found.");
            return null;
        }

        Debug.Log($"[{name}] BuildRequestLine: using set '{custom.dishName}', reqCount={custom.requestLines?.Count}");

        if (custom.requestLines != null && custom.requestLines.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, custom.requestLines.Count);
            string line = custom.requestLines[index];
            Debug.Log($"[{name}] Picked request line [{index}]: {line}");
            return line;
        }

        Debug.LogWarning($"[{name}] BuildRequestLine: dialogue set has 0 request lines.");
        return null;
    }

    private string BuildWrongLine()
    {
        var custom = GetDialogueSet();
        if (custom != null && custom.wrongLines?.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, custom.wrongLines.Count);
            string line = custom.wrongLines[index];
            Debug.Log($"[{name}] BuildWrongLine picked [{index}]: {line}");
            return line;
        }

        // No custom wrong-line → no bubble
        Debug.LogWarning($"[{name}] BuildWrongLine: no wrong lines or no set.");
        return null;
    }

    private string BuildThankLine()
    {
        var custom = GetDialogueSet();
        if (custom != null && custom.thankLines?.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, custom.thankLines.Count);
            string line = custom.thankLines[index];
            Debug.Log($"[{name}] BuildThankLine picked [{index}]: {line}");
            return line;
        }

        // Fallback: still keep THANK YOU using formats
        var names = SplitNames(npcOrder?.CurrentRequestName);
        string fallback = names.Count <= 1
            ? thankSingleFormat
            : FormatMulti(thankComboFormat, names);

        Debug.Log($"[{name}] BuildThankLine using fallback: {fallback}");
        return fallback;
    }

    private NPCDialogueSet GetDialogueSet()
    {
        if (npcOrder == null || string.IsNullOrWhiteSpace(npcOrder.CurrentRequestName))
        {
            Debug.LogWarning($"[{name}] GetDialogueSet: npcOrder or CurrentRequestName is null/empty.");
            return null;
        }

        string nameToMatch = npcOrder.CurrentRequestName;
        Debug.Log($"[{name}] GetDialogueSet: trying to match '{nameToMatch}'");

        var set = customDialogues.FirstOrDefault(d =>
            nameToMatch.IndexOf(d.dishName, StringComparison.OrdinalIgnoreCase) >= 0);

        if (set == null)
            Debug.LogWarning($"[{name}] GetDialogueSet: no match found.");
        else
            Debug.Log($"[{name}] GetDialogueSet: matched '{set.dishName}'");

        return set;
    }

    // Get dialogue set based on a specific delivered dish name
    private NPCDialogueSet GetDialogueSetForName(string dishName)
    {
        if (string.IsNullOrWhiteSpace(dishName))
        {
            Debug.LogWarning($"[{name}] GetDialogueSetForName: dishName is null/empty.");
            return null;
        }

        Debug.Log($"[{name}] GetDialogueSetForName: trying to match '{dishName}'");

        var set = customDialogues.FirstOrDefault(d =>
            dishName.IndexOf(d.dishName, StringComparison.OrdinalIgnoreCase) >= 0);

        if (set == null)
            Debug.LogWarning($"[{name}] GetDialogueSetForName: no match found.");
        else
            Debug.Log($"[{name}] GetDialogueSetForName: matched '{set.dishName}'");

        return set;
    }

    private static List<string> SplitNames(string joined)
    {
        if (string.IsNullOrWhiteSpace(joined)) return new List<string>();

        return joined
            .Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    private static string FormatMulti(string template, List<string> names)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Join(" + ", names);

        int maxIndex = MaxPlaceholderIndex(template);
        if (maxIndex >= 0)
        {
            object[] args = new object[maxIndex + 1];
            for (int i = 0; i < args.Length; i++)
                args[i] = (i < names.Count) ? names[i] : "";

            string baseText = string.Format(template, args);

            if (names.Count > args.Length)
            {
                var extras = names.Skip(args.Length);
                baseText += " " + string.Join(" + ", extras.Select(n => $"+ {n}"));
            }

            return baseText;
        }

        if (names.Count == 0) return template;

        return template + " " + string.Join(" + ", names);
    }

    private static int MaxPlaceholderIndex(string template)
    {
        int max = -1;
        for (int i = 0; i < template.Length; i++)
        {
            if (template[i] == '{')
            {
                int j = i + 1, val = 0;
                bool any = false;
                while (j < template.Length && char.IsDigit(template[j]))
                {
                    any = true;
                    val = (val * 10) + (template[j] - '0');
                    j++;
                }
                if (any && j < template.Length && template[j] == '}')
                    if (val > max) max = val;
            }
        }
        return max;
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
        Debug.Log($"[{name}] ShowChat called with: '{text}'");

        if (!chatBubblePrefab || !chatBubbleSpawnPoint || string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning($"[{name}] ShowChat: ABORT (prefab/spawn missing or text empty).");
            return;
        }

        ChatBubble.Create(
            chatBubbleSpawnPoint,
            Vector3.zero,
            ChatBubble.IconType.Dish,
            text,
            chatBubblePrefab,
            autoClearAfter
        );
    }

    // called every time a correct item is delivered
    private void HandleItemAccepted(string deliveredName, bool isComplete)
    {
        // If order is already complete, let HandleOrderFulfilled handle the thank-you flow
        if (isComplete) return;

        if (npcOrder == null || npcOrder.OriginalOrderCount <= 1)
            return; // Only care about combos

        // Make sure we have a base request line to fall back to (custom only)
        if (string.IsNullOrEmpty(selectedRequestLine))
        {
            Debug.Log($"[{name}] HandleItemAccepted: selectedRequestLine was empty, rebuilding.");
            selectedRequestLine = BuildRequestLine();
        }

        var set = GetDialogueSetForName(deliveredName);

        // If this dish has special mid-combo lines (e.g. main dish like Adobong Puti)
        if (set != null && set.midComboLines != null && set.midComboLines.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, set.midComboLines.Count);
            string line = set.midComboLines[index];
            Debug.Log($"[{name}] HandleItemAccepted mid-combo line [{index}]: {line}");
            ShowChat(line);
        }
        else
        {
            // Side dish or no special mid-combo text → repeat the original order
            ShowChat(selectedRequestLine);
        }
    }
}
