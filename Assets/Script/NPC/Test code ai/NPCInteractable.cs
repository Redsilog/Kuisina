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
    private bool isLeaving = false; // 🚫 NEW: prevents interaction during leave state

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

    // 🧩 INTERACTION — player presses E
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        // 🚫 Prevent interacting if NPC is about to leave
        if (isLeaving)
            return;

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
            if (npcOrder && npcOrder.HasActiveOrder)
            {
                if (IsWrongPrefab(playerInventory))
                    ShowChat(BuildWrongLine());
                else
                    ShowChat(selectedRequestLine);
            }
            return;
        }

        if (npcOrder.HasActiveOrder)
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
            {
                if (heldItemName.Equals(CleanName(requestedItem.name), StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            foreach (var combo in npcOrder.prefabCombos)
            {
                foreach (var comboItem in combo.items)
                {
                    if (heldItemName.Equals(CleanName(comboItem.name), StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }
        }
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
        isLeaving = true; // 🚫 Mark NPC as leaving — disable interaction
        npcMovement?.BeginThanking();
    }

    private void HandleTimeout()
    {
        ShowChat(timeoutLine);
        isLeaving = true; // 🚫 Prevent interaction once NPC decides to leave
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

        if (custom != null && custom.requestLines.Count > 0)
            return custom.requestLines[UnityEngine.Random.Range(0, custom.requestLines.Count)];

        var names = SplitNames(npcOrder?.CurrentRequestName);
        return names.Count <= 1
            ? string.Format(requestSingleFormat, names.Count > 0 ? names[0] : "")
            : FormatMulti(requestComboFormat, names);
    }

    private string BuildWrongLine()
    {
        var custom = GetDialogueSet();

        if (custom != null && custom.wrongLines.Count > 0)
            return custom.wrongLines[UnityEngine.Random.Range(0, custom.wrongLines.Count)];

        var names = SplitNames(npcOrder?.CurrentRequestName);
        return names.Count <= 1
            ? string.Format(wrongSingleFormat, names.Count > 0 ? names[0] : "")
            : FormatMulti(wrongComboFormat, names);
    }

    private string BuildThankLine()
    {
        var custom = GetDialogueSet();

        if (custom != null && custom.thankLines.Count > 0)
            return custom.thankLines[UnityEngine.Random.Range(0, custom.thankLines.Count)];

        var names = SplitNames(npcOrder?.CurrentRequestName);
        return names.Count <= 1 ? thankSingleFormat : FormatMulti(thankComboFormat, names);
    }

    private NPCDialogueSet GetDialogueSet()
    {
        if (npcOrder == null || string.IsNullOrWhiteSpace(npcOrder.CurrentRequestName))
            return null;

        return customDialogues.FirstOrDefault(d =>
            npcOrder.CurrentRequestName.IndexOf(d.dishName, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static List<string> SplitNames(string joined)
    {
        if (string.IsNullOrWhiteSpace(joined)) return new List<string>();
        return joined.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim()).ToList();
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
