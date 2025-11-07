using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Linq;

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

    // 🧩 Per-dish dialogue sets (each can have multiple random lines)
    [System.Serializable]
    public class NPCDialogueSet
    {
        public string dishName;

        [Header("Random Lines")]
        [TextArea] public List<string> requestLines = new List<string>();
        [TextArea] public List<string> wrongLines = new List<string>();
        [TextArea] public List<string> thankLines = new List<string>();
    }

    [Header("Per-Dish Custom Dialogues")]
    [SerializeField] private List<NPCDialogueSet> customDialogues = new List<NPCDialogueSet>();

    [Header("Fallback Texts (if no custom lines found)")]
    [SerializeField] private string requestSingleFormat = "Pwede isang order ng {0}?";
    [SerializeField] private string requestComboFormat = "Pwede isang order ng {0} at {1}?";
    [SerializeField] private string thankSingleFormat = "Salamat!";
    [SerializeField] private string thankComboFormat = "Salamat sa {0} at {1}!";
    [SerializeField] private string wrongSingleFormat = "Di naman yan order ko, sabi ko {0}!";
    [SerializeField] private string wrongComboFormat = "Di naman yan order ko, sabi ko {0} at {1}!";
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

    // New variable to store the selected request line
    private string selectedRequestLine;

    public bool IsTalking => waitingForInteraction || npcOrder?.HasActiveOrder == true;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        npcOrder ??= GetComponent<NPCOrder1>();
        npcMovement ??= GetComponent<NPCMovement>();

        if (npcOrder != null)
            npcOrder.OnOrderFulfilled += HandleOrderFulfilled;

        var canvasObj = GameObject.FindWithTag("NPCTimerCanvas");
        if (canvasObj != null) npcTimerCanvas = canvasObj.GetComponent<Canvas>();
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

    // Called when NPC sits and starts waiting
    public void StartWaitingForPlayer()
    {
        isResting = true;
        waitingForInteraction = true;
        inExtendedPhase = false;
        interactionTimer = initialWaitTime;
        SpawnTimerUI();

        if (npcOrder != null && npcOrder.HasActiveOrder)
            ShowChat(BuildRequestLine());
    }

    // Called when NPC stands/moves away
    public void StartMoving()
    {
        isResting = false;
    }

    // Called when player interacts (E button)
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.InteractCustomer);
        if (!ctx.performed || !playerInRange || currentInteractor == null || !isResting)
            return;

        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + interactCooldown;

        var playerInventory = currentInteractor.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;

        bool acted = npcOrder && npcOrder.StartOrTryFulfill(playerInventory);

        if (!acted)
        {
            // If the player gives the wrong prefab
            if (npcOrder && npcOrder.HasActiveOrder)
            {
                if (IsWrongPrefab(playerInventory)) // Check if the prefab is wrong
                {
                    // Show the wrong order line when the order is incorrect
                    ShowChat(BuildWrongLine());  // Wrong order line shown here
                }
                else
                {
                    // If no active order or the player is holding the correct prefab, repeat the request line
                    ShowChat(selectedRequestLine); // Repeat the previously selected request line
                }
            }
            return;
        }

        if (npcOrder.HasActiveOrder)
        {
            // Keep the dialogue going when there's an active order
            waitingForInteraction = true;
            inExtendedPhase = true;
            interactionTimer = extendedWaitTime;

            // On first interaction, randomize and store the request line
            if (string.IsNullOrEmpty(selectedRequestLine))
            {
                selectedRequestLine = BuildRequestLine(); // Randomize and store the line
            }
            ShowChat(selectedRequestLine); // Repeat the stored request line
            npcMovement?.BeginOrdering();
        }
    }

    // This method checks if the player is holding the wrong prefab
    private bool IsWrongPrefab(PlayerInventory playerInventory)
    {
        // Ensure the player is holding an item
        if (playerInventory.heldVisual == null)
        {
            return false; // If nothing is being held, return false (no wrong prefab)
        }

        // Check if the held item matches the NPC's current request (single item or combo)
        string heldItemName = CleanName(playerInventory.heldVisual.name);

        // Compare against requested items (singles or combos)
        if (npcOrder != null && npcOrder.requestedItems.Count > 0)
        {
            // For single requested items
            foreach (var requestedItem in npcOrder.requestedItems)
            {
                if (heldItemName.Equals(CleanName(requestedItem.name), StringComparison.OrdinalIgnoreCase))
                {
                    return false; // Correct item is being held
                }
            }

            // For combos
            foreach (var combo in npcOrder.prefabCombos)
            {
                foreach (var comboItem in combo.items)
                {
                    if (heldItemName.Equals(CleanName(comboItem.name), StringComparison.OrdinalIgnoreCase))
                    {
                        return false; // Correct item is part of the combo
                    }
                }
            }
        }

        // If no match is found, return true (wrong prefab)
        return true;
    }

    private string CleanName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        return name.Replace("(Clone)", "").Trim();
    }

    private void HandleOrderFulfilled()
    {
        if (npcOrder?.DeliveredDish != null)
        {
            var dishRef = npcOrder.DeliveredDish.GetComponent<DishReference>();
            if (dishRef != null)
                LevelManager.Instance.AddStars(dishRef.starsEarned);
        }

        waitingForInteraction = false;
        TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.ServeDish);
        ShowChat(BuildThankLine());

        npcOrder.DeliveredDish = null;
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

    // ---------- Dialogue Builders ----------

    private string BuildRequestLine()
    {
        var custom = GetDialogueSet();

        // Randomly select a request line on the first interaction
        if (custom != null && custom.requestLines != null && custom.requestLines.Count > 0)
            return custom.requestLines[UnityEngine.Random.Range(0, custom.requestLines.Count)];

        var names = SplitNames(npcOrder?.CurrentRequestName);
        if (names.Count <= 1)
            return string.Format(requestSingleFormat, names.Count > 0 ? names[0] : "");
        else
            return FormatMulti(requestComboFormat, names);
    }

    private string BuildWrongLine()
    {
        var custom = GetDialogueSet();

        // If custom wrong lines are available, pick one randomly
        if (custom != null && custom.wrongLines != null && custom.wrongLines.Count > 0)
            return custom.wrongLines[UnityEngine.Random.Range(0, custom.wrongLines.Count)];

        var names = SplitNames(npcOrder?.CurrentRequestName);
        if (names.Count <= 1)
            return string.Format(wrongSingleFormat, names.Count > 0 ? names[0] : "");
        else
            return FormatMulti(wrongComboFormat, names);
    }
    private string BuildThankLine()
    {
        var custom = GetDialogueSet();

        if (custom != null && custom.thankLines != null && custom.thankLines.Count > 0)
            return custom.thankLines[UnityEngine.Random.Range(0, custom.thankLines.Count)];

        var names = SplitNames(npcOrder?.CurrentRequestName);
        if (names.Count <= 1)
            return thankSingleFormat;
        else
            return FormatMulti(thankComboFormat, names);
    }

    private NPCDialogueSet GetDialogueSet()
    {
        if (npcOrder == null || string.IsNullOrWhiteSpace(npcOrder.CurrentRequestName))
            return null;

        // Match any dish text found in CurrentRequestName
        return customDialogues.FirstOrDefault(d =>
            npcOrder.CurrentRequestName.IndexOf(d.dishName, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static List<string> SplitNames(string joined)
    {
        if (string.IsNullOrWhiteSpace(joined)) return new List<string>();
        return joined.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
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
                int j = i + 1;
                int val = 0;
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

    // ---------- Timer UI ----------

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
}
