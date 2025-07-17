using UnityEngine;
using UnityEngine.Events;

public class NPCOrder : MonoBehaviour
{
    [Header("Order Data")]
    public GameObject[] orderPrefabs;
    public string[]     orderNames;

    [Header("Settings")]
    public KeyCode   interactKey      = KeyCode.E;
    public Transform orderDisplayPoint;
    public Transform storagePoint;

    [Header("Events")]
    public UnityEvent onOrderRequested;
    public UnityEvent onOrderComplete;

    NPCInventory    npcInventory;
    PlayerInventory playerInventory;
    bool            playerInRange;
    int             currentOrder    = -1;
    GameObject      displayGhost;

    void Awake()
    {
        // ensure this collider is a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        npcInventory = GetComponent<NPCInventory>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange   = true;
            playerInventory = other.GetComponent<PlayerInventory>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange   = false;
            playerInventory = null;
        }
    }

    void Update()
    {
        if (!playerInRange || playerInventory == null)
            return;

        if (!Input.GetKeyDown(interactKey))
            return;

        if (currentOrder < 0)
        {
            AskForRandomOrder();
        }
        else if (PlayerHasRequestedItem())
        {
            FulfillOrder();
        }
        else
        {
            Debug.Log("NPC: You already have an order. Bring it back!");
        }
    }

    bool PlayerHasRequestedItem()
    {
        if (playerInventory.heldVisual == null)
            return false;

        return playerInventory.heldDish == orderNames[currentOrder]
            || playerInventory.heldIngredient == orderNames[currentOrder];
    }

    /// <summary>
    /// Called to have the NPC request a new random order.
    /// Fires onOrderRequested immediately.
    /// </summary>
    public void AskForRandomOrder()
    {
        if (orderNames.Length == 0)
            return;

        currentOrder = Random.Range(0, orderNames.Length);
        string name  = orderNames[currentOrder];
        Debug.Log($"NPC requests: «{name}»");

        if (orderDisplayPoint != null)
        {
            displayGhost = Instantiate(
                orderPrefabs[currentOrder],
                orderDisplayPoint.position,
                Quaternion.identity,
                orderDisplayPoint
            );
        }

        onOrderRequested?.Invoke();
    }

    /// <summary>
    /// Called when the player delivers the requested item.
    /// Fires onOrderComplete at the end.
    /// </summary>
    void FulfillOrder()
    {
        GameObject heldObj = playerInventory.heldVisual;

        if (playerInventory.HasDish())
            playerInventory.PlaceDish(transform.position);
        else
            playerInventory.PlaceIngredient();

        npcInventory.ReceiveItem(orderNames[currentOrder], heldObj);
        Debug.Log("NPC: Thank you!");

        // clean up
        currentOrder = -1;
        if (displayGhost) Destroy(displayGhost);

        onOrderComplete?.Invoke();
    }
}
