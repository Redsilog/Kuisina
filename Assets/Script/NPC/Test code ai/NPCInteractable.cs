using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    // Dialogue lists kept here; index-based lines are still supported.
    [Header("Dialog Lines (must match NPCOrder1.requestedItems)")]
    private List<string> requestLines = new List<string>();
    private List<string> thankLines = new List<string>();
    private List<string> wrongItemLines = new List<string>();

    [Header("Fallback Texts")]
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
    private bool isResting = false; // NPC is available only when resting

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

    // Called by NPCMovement when NPC reaches sit point
    public void StartWaitingForPlayer()
    {
        isResting = true;
        waitingForInteraction = true;
        inExtendedPhase = false; // initial wait window
        interactionTimer = initialWaitTime;
        SpawnTimerUI();

        // Announce request when seated, if any
        if (npcOrder != null && npcOrder.HasActiveOrder)
            ShowChat(BuildRequestLine());
    }

    // Called by NPCMovement when NPC starts moving
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

        if (!acted)
        {
            // Wrong item (active order but mismatch)
            if (npcOrder && npcOrder.HasActiveOrder)
                ShowChat(BuildWrongLine());
            return;
        }

        // If we now have an active order, show the request and enter ordering state
        if (npcOrder.HasActiveOrder)
        {
            waitingForInteraction = true;
            inExtendedPhase = true; // extended wait window
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

            npcOrder.DeliveredDish = null; // allowed: property has public setter
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

    // ---------- Dialogue builders (multi-placeholder aware) ----------

    private string BuildRequestLine()
    {
        var names = SplitNames(npcOrder?.CurrentRequestName);

        if (names.Count <= 1)
            return string.Format(requestSingleFormat, names.Count > 0 ? names[0] : "");
        else
            return FormatMulti(requestComboFormat, names);
    }

    private string BuildWrongLine()
    {
        var names = SplitNames(npcOrder?.CurrentRequestName);

        if (names.Count <= 1)
            return string.Format(wrongSingleFormat, names.Count > 0 ? names[0] : "");
        else
            return FormatMulti(wrongComboFormat, names);
    }

    private string BuildThankLine()
    {
        var names = SplitNames(npcOrder?.CurrentRequestName);

        if (names.Count <= 1)
            return thankSingleFormat;
        else
            return FormatMulti(thankComboFormat, names);
    }


    private static List<string> SplitNames(string joined)
    {
        if (string.IsNullOrWhiteSpace(joined)) return new List<string>();
        // CurrentRequestName uses " + " as joiner; split back to parts:
        return joined.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim())
                     .ToList();
    }

    /// <summary>
    /// If the template contains placeholders ({0},{1},...), we fill as many as exist.
    /// If there are more names than placeholders, append " + name" to the end.
    /// If there are no placeholders, we fall back to: "templatePrefix + joined names".
    /// </summary>
    private static string FormatMulti(string template, List<string> names)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Join(" + ", names);

        // Detect placeholders {0}..{N}
        int maxIndex = MaxPlaceholderIndex(template);
        if (maxIndex >= 0)
        {
            // Build args array sized to the number of placeholders we actually saw
            object[] args = new object[maxIndex + 1];
            for (int i = 0; i < args.Length; i++)
                args[i] = (i < names.Count) ? names[i] : "";

            string baseText = string.Format(template, args);

            // If more names than placeholders, append extras as " + name"
            if (names.Count > args.Length)
            {
                var extras = names.Skip(args.Length);
                baseText += " " + string.Join(" + ", extras.Select(n => $"+ {n}"));
            }
            return baseText;
        }

        // No placeholders at all → simple fallback: append the joined list
        if (names.Count == 0) return template;
        return template + " " + string.Join(" + ", names);
    }

    private static int MaxPlaceholderIndex(string template)
    {
        int max = -1;
        // tiny parser for {0},{1}...
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
                {
                    if (val > max) max = val;
                }
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
