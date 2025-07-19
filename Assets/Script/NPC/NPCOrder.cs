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
        if (!playerInRange || playerInventory == null) return;
        if (!Input.GetKeyDown(interactKey))           return;

        if (currentOrder < 0)              AskForRandomOrder();
        else if (PlayerHasRequestedItem()) FulfillOrder();
        else                               Debug.Log("NPC: You already have an order. Bring it back!");
    }

    bool PlayerHasRequestedItem()
    {
        if (playerInventory.heldVisual == null) return false;
        string held   = playerInventory.heldVisual.name.Replace("(Clone)","");
        string needed = orderNames[currentOrder];
        return held == needed;
    }

    public void AskForRandomOrder()
    {
     // just pick the index and log
        currentOrder = Random.Range(0, orderNames.Length);
        Debug.Log($"NPC requests: «{orderNames[currentOrder]}»");

        onOrderRequested?.Invoke();
    }

    void FulfillOrder()
    {
        var heldObj = playerInventory.heldVisual;
        if (playerInventory.HasDish()) playerInventory.PlaceDish(transform.position);
        else playerInventory.PlaceIngredient();

        npcInventory.ReceiveItem(orderNames[currentOrder], heldObj);
        Debug.Log("NPC: Thank you!");

        currentOrder = -1;
        if (displayGhost) Destroy(displayGhost);

        onOrderComplete?.Invoke();
        
            if (orderDisplayPoint != null)
        Instantiate(
            orderPrefabs[currentOrder],
            orderDisplayPoint.position,
            Quaternion.identity,
            orderDisplayPoint
        );

        onOrderComplete?.Invoke();
    }
}
