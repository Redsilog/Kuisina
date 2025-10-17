using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCOrder1 : MonoBehaviour
{
    [Header("Order Settings")]
    [Tooltip("Possible items this NPC may request (pick one at random).")]
    public List<GameObject> requestedItems = new List<GameObject>();

    [Tooltip("Where the delivered item is shown.")]
    public Transform orderDisplayPoint;

    public event Action OnOrderFulfilled;

    // runtime
    public bool HasActiveOrder => currentRequestedItem != null && !orderFulfilled;
    public string CurrentRequestName => currentRequestedItem ? TrimName(currentRequestedItem.name) : "";

    private GameObject currentRequestedItem;
    private bool orderFulfilled;

    static string TrimName(string n) => string.IsNullOrEmpty(n) ? "" : n.Replace("(Clone)", "").Trim();

    // Randomize the order (pick a random item from the list)
    public void RandomizeOrder()
    {
        if (requestedItems == null || requestedItems.Count == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, requestedItems.Count);
        currentRequestedItem = requestedItems[randomIndex];
        orderFulfilled = false;

        Debug.Log($"NPC has requested: {currentRequestedItem.name}");
    }

    // Try to fulfill the NPC's order
    public bool StartOrTryFulfill(PlayerInventory player)
    {
        if (player == null) return false;

        if (!HasActiveOrder)
        {
            // pick a random item to request
            if (requestedItems == null || requestedItems.Count == 0) return false;

            int idx = UnityEngine.Random.Range(0, requestedItems.Count);
            currentRequestedItem = requestedItems[idx];
            orderFulfilled = false;
            Debug.Log($"NPC has requested {currentRequestedItem.name}");
            return true; // started order
        }
        else
        {
            // try to accept the player's held item
            string held = ResolveHeldName(player);
            if (string.IsNullOrEmpty(held)) return false;

            if (held.Equals(CurrentRequestName, StringComparison.OrdinalIgnoreCase))
            {
                FulfillOrder(player);
                return true;
            }
        }

        return false;
    }

    void FulfillOrder(PlayerInventory player)
    {
        orderFulfilled = true;

        // spawn/anchor the delivered item (optional visual)
        if (orderDisplayPoint != null && currentRequestedItem != null)
        {
            Instantiate(currentRequestedItem, orderDisplayPoint.position, orderDisplayPoint.rotation);
        }

        // consume from player's hand
        if (player != null)
        {
            if (player.heldVisual) Destroy(player.heldVisual);
            player.ClearHeldItemDirect();
        }

        // notify
        OnOrderFulfilled?.Invoke();

        // clear internal order after a short delay (optional)
        Invoke(nameof(ClearOrder), 1.0f);
    }

    void ClearOrder()
    {
        currentRequestedItem = null;
        orderFulfilled = false;
    }

    string ResolveHeldName(PlayerInventory inv)
    {
        if (inv == null) return "";
        if (!string.IsNullOrWhiteSpace(inv.heldIngredient)) return inv.heldIngredient.Trim();
        if (inv.heldVisual != null) return TrimName(inv.heldVisual.name);
        return "";
    }
}
