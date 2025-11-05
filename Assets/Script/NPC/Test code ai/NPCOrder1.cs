using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles which item an NPC requests and tracks whether it has been fulfilled.
/// The NPCInteractable script handles all dialogue and chat display.
/// </summary>
public class NPCOrder1 : MonoBehaviour
{
    public List<GameObject> requestedItems = new List<GameObject>();
    public event Action OnOrderFulfilled;

    private GameObject _currentItem;
    private bool _orderFulfilled;
    private int _currentIndex = -1;

    public bool HasActiveOrder => _currentItem != null && !_orderFulfilled;
    public string CurrentRequestName => _currentItem ? _currentItem.name.Replace("(Clone)", "").Trim() : "";
    public int CurrentRequestIndex => _currentIndex;

    public GameObject DeliveredDish;

    public void RandomizeOrder()
    {
        if (requestedItems.Count == 0) return;
        _currentIndex = UnityEngine.Random.Range(0, requestedItems.Count);
        _currentItem = requestedItems[_currentIndex];
        _orderFulfilled = false;
        Debug.Log($"NPC wants: {CurrentRequestName}");
    }

    public bool StartOrTryFulfill(PlayerInventory player)
    {
        if (!HasActiveOrder)
        {
            RandomizeOrder();
            return true;
        }

        string held = player.heldVisual ? player.heldVisual.name.Replace("(Clone)", "").Trim() : "";
        if (held.Equals(CurrentRequestName, StringComparison.OrdinalIgnoreCase))
        {
            _orderFulfilled = true;
            DeliveredDish = player.heldVisual;
            player.ClearHeldItemDirect();
            OnOrderFulfilled?.Invoke();
            return true;
        }

        return false;
    }
}

