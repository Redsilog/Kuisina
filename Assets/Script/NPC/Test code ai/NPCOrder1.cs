using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles which item an NPC requests and tracks whether it has been fulfilled.
/// The NPCInteractable script handles all dialogue and chat display.
/// </summary>
public class NPCOrder1 : MonoBehaviour
{
    [Header("Requested Items")]
    [Tooltip("Possible items this NPC may request (pick one at random).")]
    public List<GameObject> requestedItems = new List<GameObject>();

    [SerializeField, HideInInspector]
    private Transform orderDisplayPoint;


    /// <summary>
    /// Event fired when the correct item is delivered.
    /// </summary>
    public event Action OnOrderFulfilled;

    // --- Runtime State ---
    public bool HasActiveOrder => _currentItem != null && !_orderFulfilled;
    public string CurrentRequestName => _currentItem ? TrimName(_currentItem.name) : "";
    public int CurrentRequestIndex => _currentIndex;

    private GameObject _currentItem;
    private bool _orderFulfilled;
    private int _currentIndex = -1;

    // ---------------- ORDER LOGIC ----------------

    /// <summary>
    /// Randomly picks one of the available requestedItems for this NPC.
    /// </summary>
    public void RandomizeOrder()
    {
        if (requestedItems == null || requestedItems.Count == 0)
            return;

        _currentIndex = UnityEngine.Random.Range(0, requestedItems.Count);
        _currentItem = requestedItems[_currentIndex];
        _orderFulfilled = false;

        Debug.Log($"[NPCOrder1] Requested: {CurrentRequestName} (#{_currentIndex})");
    }


    public void SetOrderDisplayPoint(Transform displayPoint)
    {
        orderDisplayPoint = displayPoint;
    }

    /// <summary>
    /// Called by NPCInteractable when the player interacts.
    /// Starts a new order or tries to fulfill the current one.
    /// </summary>
    public bool StartOrTryFulfill(PlayerInventory player)
    {
        if (player == null)
            return false;

        // No active order → start a new one
        if (!HasActiveOrder)
        {
            if (requestedItems == null || requestedItems.Count == 0)
                return false;

            RandomizeOrder();
            return true; // Started new order
        }

        // Try to fulfill the existing order
        string held = ResolveHeldName(player);
        if (string.IsNullOrEmpty(held))
            return false;

        if (held.Equals(CurrentRequestName, StringComparison.OrdinalIgnoreCase))
        {
            FulfillOrder(player);
            return true; // Fulfilled successfully
        }

        // Wrong item
        return false;
    }

    private void FulfillOrder(PlayerInventory player)
    {
        _orderFulfilled = true;

        // Instantiate visual of delivered item if an anchor exists
        if (orderDisplayPoint != null && _currentItem != null)
        {
            Instantiate(_currentItem, orderDisplayPoint.position, orderDisplayPoint.rotation);
        }

        // Clear player's held item
        if (player != null)
        {
            if (player.heldVisual) Destroy(player.heldVisual);
            player.ClearHeldItemDirect();
        }

        Debug.Log($"[NPCOrder1] Order fulfilled: {CurrentRequestName}");

        // Notify listeners (NPCInteractable)
        OnOrderFulfilled?.Invoke();

        // Reset order after short delay
        Invoke(nameof(ClearOrder), 1.0f);
    }

    private void ClearOrder()
    {
        _currentItem = null;
        _orderFulfilled = false;
        _currentIndex = -1;
    }

    // ---------------- HELPERS ----------------

    private string ResolveHeldName(PlayerInventory inv)
    {
        if (inv == null) return "";

        // Check if player is holding an ingredient name
        if (!string.IsNullOrWhiteSpace(inv.heldIngredient))
            return inv.heldIngredient.Trim();

        // Or use the held visual object's name
        if (inv.heldVisual != null)
            return TrimName(inv.heldVisual.name);

        return "";
    }

    private static string TrimName(string n)
    {
        return string.IsNullOrEmpty(n) ? "" : n.Replace("(Clone)", "").Trim();
    }
}
