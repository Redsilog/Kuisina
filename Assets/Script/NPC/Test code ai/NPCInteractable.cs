using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Dialog")]
    [TextArea] public string[] npcDialogLines;       // List of dialog lines

    [Header("Chat Bubble")]
    [SerializeField] private GameObject chatBubblePrefab;
    [SerializeField] private Transform chatBubbleSpawnPoint;

    [Header("Order")]
    [SerializeField] private NPCOrder1 npcOrder;

    [Header("Head Look")]
    [SerializeField] private NPCHeadLookAt headLookAt;

    [Header("Movement Ref")]
    [SerializeField] private NPCMovement npcMovement;

    [Header("Timing")]
    public float autoClearAfter = 2f;
    public float interactCooldown = 1.0f;
    public float initialWaitTime = 5f;     // waiting before any interaction
    public float extendedWaitTime = 20f;   // after taking the order
    public float thankYouDelay = 5f;       // dwell after delivery

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

        if (npcOrder != null)
            npcOrder.OnOrderFulfilled += HandleOrderFulfilled;
    }

    void OnDestroy()
    {
        if (npcOrder != null)
            npcOrder.OnOrderFulfilled -= HandleOrderFulfilled;
    }

    // Proximity
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
            if (headLookAt) headLookAt.StopLooking();
        }
    }

    // Called by NPCMovement when it reaches its waiting spot
    public void StartWaitingForPlayer()
    {
        interactionTimer = initialWaitTime;
        waitingForInteraction = true;
        // Randomize the order as soon as the NPC is ready to serve
        npcOrder.RandomizeOrder();
    }

    void Update()
    {
        if (!waitingForInteraction) return;

        interactionTimer -= Time.deltaTime;
        if (interactionTimer <= 0f)
        {
            waitingForInteraction = false;
            // no interaction → leave
            if (npcMovement) npcMovement.StartLeaving();
            if (headLookAt) headLookAt.StopLooking();
        }
    }

    // Interact from PlayerController
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !playerInRange || currentInteractor == null) return;
        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + interactCooldown;

        var inv = currentInteractor.GetComponent<PlayerInventory>();

        // try to start or fulfill order
        bool acted = npcOrder ? npcOrder.StartOrTryFulfill(inv) : false;

        if (acted)
        {
            if (npcOrder.HasActiveOrder)
            {
                // order started → extend wait time and show request line
                interactionTimer = extendedWaitTime;
                waitingForInteraction = true;

                string want = npcOrder.CurrentRequestName;
                if (!string.IsNullOrEmpty(want))
                    ShowChat($"I’d like {want}, please.");

                // NPC looks at the player
                if (headLookAt != null)
                {
                    headLookAt.SetPlayerTransform(currentInteractor);
                    headLookAt.LookAtTransform(currentInteractor, 1.6f);
                    headLookAt.EnableFollowing(true);
                }
            }
            else
            {
                // Optional: Flavor line if order was fulfilled already
                string line = GetRandomDialog();
                if (!string.IsNullOrEmpty(line)) ShowChat(line);
            }
        }
    }

    void HandleOrderFulfilled()
    {
        ShowChat("Thank you!");
        StartCoroutine(ThankAndLeave());
    }

    IEnumerator ThankAndLeave()
    {
        waitingForInteraction = false;
        yield return new WaitForSeconds(thankYouDelay);
        if (npcMovement) npcMovement.StartLeaving();
        if (headLookAt) headLookAt.StopLooking();
    }

    // UI helpers
    private string GetRandomDialog()
    {
        if (npcDialogLines == null || npcDialogLines.Length == 0) return null;
        int idx = Random.Range(0, npcDialogLines.Length);
        return npcDialogLines[idx];
    }

    private void ShowChat(string text)
    {
        if (chatBubblePrefab && chatBubbleSpawnPoint)
        {
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
}
