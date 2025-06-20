using UnityEngine;

public class NPCOrder : MonoBehaviour
{
    [Header("Order Data")]
    public GameObject[] orderPrefabs;
    public string[] orderNames;

    [Header("Settings")]
    public KeyCode interactKey = KeyCode.E;
    public Transform orderDisplayPoint;    // optional ghost
    public Transform storagePoint;         // to parent delivered objects

    NPCInventory npcInventory;
    PlayerInventory playerInventory;
    bool playerInRange;
    int currentOrder = -1;
    GameObject displayGhost;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        npcInventory = GetComponent<NPCInventory>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Waiters"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Waiter"))
        {
            playerInRange = false;
            playerInventory = null;
        }
    }

    void Update()
    {
        if (!playerInRange || playerInventory == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (currentOrder < 0)
            {
                AskForRandomOrder();
            }
            else
            {
                // **Try** to fulfill if the player has the right thing
                if (PlayerHasRequestedItem())
                    FulfillOrder();
                else
                    Debug.Log("NPC: You already have an order. Bring it back!");
            }
        }
    }

    void AskForRandomOrder()
    {
        if (orderNames.Length == 0) return;
        currentOrder = Random.Range(0, orderNames.Length);
        string name = orderNames[currentOrder];
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
    }

    bool PlayerHasRequestedItem()
    {
        // adapt if you only use dishes or only ingredients
        string held = playerInventory.HasDish()
            ? playerInventory.heldDish
            : playerInventory.HasIngredient()
                ? playerInventory.heldIngredient
                : "";
        return held == orderNames[currentOrder];
    }

    void FulfillOrder()
    {
        // grab the actual GameObject the player is holding
        GameObject heldObj = playerInventory.heldVisual;

        // remove from player
        if (playerInventory.HasDish())
            playerInventory.PlaceDish(transform.position);
        else
            playerInventory.PlaceIngredient();

        // give to NPC
        npcInventory.ReceiveItem(orderNames[currentOrder], heldObj);

        Debug.Log("NPC: Thank you!");

        // cleanup
        currentOrder = -1;
        if (displayGhost) Destroy(displayGhost);
    }
}
