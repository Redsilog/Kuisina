using UnityEngine;

public class NPCOrder : MonoBehaviour, Interaction
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
        if (other.CompareTag("Waiter"))
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
        if (!Input.GetKeyDown(interactKey)) return;

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
        // no held object  can't fulfill
        if (playerInventory.heldVisual == null)
            return false;

        // check both dish & ingredient slots
        return playerInventory.heldDish == orderNames[currentOrder]
            || playerInventory.heldIngredient == orderNames[currentOrder];
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

    public string GetDescription()
    {
        var player = Object.FindFirstObjectByType<PlayerInventory>();
        if (player == null) return null;

        if (currentOrder < 0)
            return "Ask for order";
        if (player.heldDish == orderNames[currentOrder] ||
            player.heldIngredient == orderNames[currentOrder])
            return "Give order";
        return null;
    }

    public KeyCode GetKey() => KeyCode.E;

    public void Interact()
    {
        var player = Object.FindFirstObjectByType<PlayerInventory>();
        if (player == null) return;

        if (currentOrder < 0)
        {
            AskForRandomOrder();
        }
        else if (player.heldDish == orderNames[currentOrder] ||
                 player.heldIngredient == orderNames[currentOrder])
        {
            FulfillOrder();
        }
    }
}
