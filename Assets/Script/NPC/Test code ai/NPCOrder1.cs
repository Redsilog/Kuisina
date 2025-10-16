using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class NPCOrder1 : MonoBehaviour
{
    [Header("Order Settings")]
    [Tooltip("List of requested items (prefabs) the NPC may ask for")]
    public List<GameObject> requestedItems = new List<GameObject>();

    [Header("Display Point")]
    [Tooltip("Where the item is placed once it's handed to the NPC")]
    public Transform orderDisplayPoint;

    // Runtime Variables
    private PlayerInventory playerInventory;
    private bool playerInRange;
    private bool orderFulfilled;

    // Cooldown for the next interaction
    private float nextAllowedTime = 0f;
    public float interactCooldown = 1f; // seconds

    // Store the currently requested item
    private GameObject currentRequestedItem;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerInventory = null;
        }
    }

    // Interact functionality for the NPC order (called from NPCInteractable)
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !playerInRange || playerInventory == null) return;

        // Cooldown gate
        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + interactCooldown;

        // Check if the player has the requested item in hand
        if (!orderFulfilled && playerHasRequestedItem())
        {
            // Fulfill the order
            FulfillOrder();
        }
        else
        {
            // Inform the player they need to bring the right item
            Debug.Log("NPC: You need to bring the requested item!");
        }
    }

    // Check if the player has the requested item
    bool playerHasRequestedItem()
    {
        if (playerInventory.heldVisual == null) return false;
        if (playerInventory.heldVisual.name.Replace("(Clone)", "") == currentRequestedItem.name)
        {
            return true;
        }
        return false;
    }

    // Fulfill the order
    void FulfillOrder()
    {
        orderFulfilled = true;

        // Place the item at the display point (showing the requested item)
        if (orderDisplayPoint != null && currentRequestedItem != null)
        {
            Instantiate(currentRequestedItem, orderDisplayPoint.position, orderDisplayPoint.rotation);
        }

        // Reset the order after some time
        Invoke(nameof(ClearOrder), 2f);
    }

    // Clear the order after some time (reset order)
    void ClearOrder()
    {
        orderFulfilled = false;
        Debug.Log("NPC: Order reset! Ready for a new item.");
    }

    // Randomly choose the requested item
    public void RandomizeOrder()
    {
        int randomIndex = Random.Range(0, requestedItems.Count);
        currentRequestedItem = requestedItems[randomIndex];
        Debug.Log("NPC: I need a " + currentRequestedItem.name + "!");
    }

    // Get the current requested item
    public GameObject GetCurrentRequestedItem()
    {
        return currentRequestedItem;
    }
}
