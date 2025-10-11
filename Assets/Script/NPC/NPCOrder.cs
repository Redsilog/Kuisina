using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem; // <-- New Input System

public class NPCOrder : MonoBehaviour
{
    [Header("Order Data")]
    public GameObject[] orderPrefabs;
    public string[] orderNames;

    [Header("Settings")]
    public InputActionReference interactAction; // Assign via Inspector
    public Transform orderDisplayPoint;

    [Header("Events")]
    public UnityEvent onOrderRequested;
    public UnityEvent onOrderComplete;

    NPCInventory npcInventory;
    PlayerInventory playerInventory;
    bool playerInRange;
    int currentOrder = -1;
    GameObject displayGhost;

    [SerializeField] public TextMeshProUGUI customerOrder1;

    void Awake()
    {
        if (name.Contains("Jason"))
            customerOrder1 = GameObject.Find("Dialogue_Jason").GetComponent<TextMeshProUGUI>();
        else if (name.Contains("Rafael"))
            customerOrder1 = GameObject.Find("Dialogue_Rafael").GetComponent<TextMeshProUGUI>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;
        npcInventory = GetComponent<NPCInventory>();
    }

    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = false;
            playerInventory = null;
        }
    }

    void Update()
    {
        if (!playerInRange || playerInventory == null || interactAction == null) return;

        if (interactAction.action.WasPressedThisFrame())
        {
            if (currentOrder < 0) AskForRandomOrder();
            else if (PlayerHasRequestedItem()) FulfillOrder();
            else Debug.Log("NPC: You already have an order. Bring it back!");
        }
    }

    bool PlayerHasRequestedItem()
    {
        if (playerInventory.heldVisual == null) return false;
        string held = playerInventory.heldVisual.name.Replace("(Clone)", "");
        string needed = orderNames[currentOrder];
        return held == needed;
    }

    public void AskForRandomOrder()
    {
        currentOrder = Random.Range(0, orderNames.Length);
        Debug.Log($"NPC requests: «{orderNames[currentOrder]}»");

        if (customerOrder1 != null)
            customerOrder1.text = orderNames[currentOrder];

        onOrderRequested?.Invoke();
    }

    void ClearText()
    {
        if (customerOrder1 != null)
            customerOrder1.text = "";
    }

    void FulfillOrder()
    {
        int fulfilledOrderIndex = currentOrder;

        if (playerInventory.heldVisual != null)
        {
            Destroy(playerInventory.heldVisual);
            playerInventory.heldVisual = null;
        }

        npcInventory.ReceiveItem(orderNames[fulfilledOrderIndex], null);

        Debug.Log("NPC: Thank you!");

        if (customerOrder1 != null)
        {
            customerOrder1.text = "Thank you!";
            Invoke(nameof(ClearText), 2f);
        }

        if (displayGhost) Destroy(displayGhost);

        if (orderDisplayPoint != null)
        {
            GameObject servedDish = Instantiate(
                orderPrefabs[fulfilledOrderIndex],
                orderDisplayPoint.position,
                orderDisplayPoint.rotation,
                orderDisplayPoint
            );

            Destroy(servedDish, 5f);
        }

        onOrderComplete?.Invoke();
        currentOrder = -1;
    }
}
