using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCOrder1 : MonoBehaviour
{
    [Header("Order Settings")]
    [Tooltip("Possible items this NPC may request (pick one at random).")]
    public List<GameObject> requestedItems = new List<GameObject>();

    [Tooltip("Where the delivered item is shown (optional visual).")]
    public Transform orderDisplayPoint;

    [Header("Dialog (index MUST match requestedItems)")]
    public List<string> requestLines = new List<string>();    // shown when order starts
    public List<string> thankLines = new List<string>();    // shown when fulfilled
    public List<string> wrongItemLines = new List<string>();  // shown when wrong item given

    [Header("Fallback Lines")]
    [SerializeField] private string defaultRequestFormat = "I�d like {0}, please.";
    [SerializeField] private string defaultThankFormat = "Thank you!";
    [SerializeField] private string defaultWrongFormat = "That�s not what I ordered. I asked for {0}.";
    [SerializeField] private string timeoutLine = "I�ll come back later.";

    public event Action OnOrderFulfilled;

    // � Runtime state �
    public bool HasActiveOrder => _currentItem != null && !_orderFulfilled;
    public string CurrentRequestName => _currentItem ? TrimName(_currentItem.name) : "";
    public int CurrentRequestIndex => _currentIndex;

    private GameObject _currentItem;
    private bool _orderFulfilled;
    private int _currentIndex = -1;

    static string TrimName(string n) => string.IsNullOrEmpty(n) ? "" : n.Replace("(Clone)", "").Trim();

    // Pick a random item and start an order
    public void RandomizeOrder()
    {
        if (requestedItems == null || requestedItems.Count == 0) return;

        _currentIndex = UnityEngine.Random.Range(0, requestedItems.Count);
        _currentItem = requestedItems[_currentIndex];
        _orderFulfilled = false;

        Debug.Log($"[NPCOrder1] Requested: {CurrentRequestName} (#{_currentIndex})");
    }

    /// Try to start an order (if none) or fulfill it (if active). Returns true if something happened.
    public bool StartOrTryFulfill(PlayerInventory player)
    {
        if (player == null) return false;

        if (!HasActiveOrder)
        {
            if (requestedItems == null || requestedItems.Count == 0) return false;
            RandomizeOrder();
            return true; // started order
        }

        // Try to fulfill
        string held = ResolveHeldName(player);
        if (string.IsNullOrEmpty(held)) return false;

        if (held.Equals(CurrentRequestName, StringComparison.OrdinalIgnoreCase))
        {
            FulfillOrder(player);
            return true;
        }

        return false; // wrong item
    }

    void FulfillOrder(PlayerInventory player)
    {
        _orderFulfilled = true;

        // Optional: spawn delivered visual
        if (orderDisplayPoint != null && _currentItem != null)
            Debug.Log("Instantiate");
            Instantiate(_currentItem, orderDisplayPoint.position, orderDisplayPoint.rotation);

        // Clear player's hand
        if (player != null)
        {
            if (player.heldVisual) Destroy(player.heldVisual);
            player.ClearHeldItemDirect();
        }

        OnOrderFulfilled?.Invoke();
        Invoke(nameof(ClearOrder), 1.0f);
    }

    void ClearOrder()
    {
        _currentItem = null;
        _orderFulfilled = false;
        _currentIndex = -1;
    }

    string ResolveHeldName(PlayerInventory inv)
    {
        if (inv == null) return "";
        if (!string.IsNullOrWhiteSpace(inv.heldIngredient)) return inv.heldIngredient.Trim();
        if (inv.heldVisual != null) return TrimName(inv.heldVisual.name);
        return "";
    }

    // ------- Dialog Handling --------
    // Get Request Line for the item at the current index
    public string GetRequestLine()
    {
        return FormatByIndex(requestLines, _currentIndex, defaultRequestFormat, CurrentRequestName);
    }

    // Get Wrong Item Line for the item at the current index
    public string GetWrongLine()
    {
        return FormatByIndex(wrongItemLines, _currentIndex, defaultWrongFormat, CurrentRequestName);
    }

    // Get Thank Line for the item at the current index
    public string GetThankLine()
    {
        return FormatByIndex(thankLines, _currentIndex, defaultThankFormat, CurrentRequestName);
    }

    // Format the dialog line based on index
    string FormatByIndex(List<string> list, int idx, string fallbackFmt, string itemName)
    {
        string line = null;
        if (list != null && idx >= 0 && idx < list.Count) line = list[idx];
        if (string.IsNullOrWhiteSpace(line)) line = fallbackFmt;
        return line.Contains("{0}") ? string.Format(line, itemName) : line;
    }
}
