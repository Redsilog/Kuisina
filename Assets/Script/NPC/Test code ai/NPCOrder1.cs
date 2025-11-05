using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles which items an NPC requests and tracks whether they have been fulfilled.
/// Supports:
///  - Single items (requestedItems)
///  - Inspector-authored prefab combos (prefabCombos)
///  - Forcing a combo by index or name
///  - Randomizing from singles or combos
/// </summary>
public class NPCOrder1 : MonoBehaviour
{
    [Header("Menu Items (Single)")]
    [Tooltip("All valid single dishes this NPC can request. Names must match delivered GameObject names (case-insensitive; '(Clone)' ignored).")]
    public List<GameObject> requestedItems = new List<GameObject>();

    [Header("Menu Items (Prefab Combos)")]
    [Tooltip("Prefab-defined combos. Each combo is a list of prefabs that must be delivered one-by-one, in any order.")]
    public List<OrderCombo> prefabCombos = new List<OrderCombo>();

    /// <summary>Raised when the last required item has been delivered.</summary>
    public event Action OnOrderFulfilled;

    // Internal state
    private readonly List<string> _pendingOrderNames = new List<string>();
    private bool _orderFulfilled;

    /// <summary>
    /// The last delivered GameObject (set when a correct item is handed over).
    /// NPCInteractable reads this to award stars, then sets it back to null.
    /// </summary>
    public GameObject DeliveredDish { get; set; }

    /// <summary>True if there is an active order and items are still missing.</summary>
    public bool HasActiveOrder => _pendingOrderNames.Count > 0 && !_orderFulfilled;

    /// <summary>A friendly string like "Burger + Fries + Soda".</summary>
    public string CurrentRequestName => _pendingOrderNames.Count == 0 ? string.Empty : string.Join(" + ", _pendingOrderNames);

    /// <summary>
    /// Kept for backward compatibility with code that formats by index.
    /// For combos this returns 0; dialogue should use CurrentRequestName for accurate text.
    /// </summary>
    public int CurrentRequestIndex => 0;

    // =============================
    //  RANDOM ORDER
    // =============================
    public void RandomizeOrder()
    {
        _pendingOrderNames.Clear();
        _orderFulfilled = false;

        bool hasCombos = prefabCombos != null && prefabCombos.Count > 0;
        bool hasSingles = requestedItems != null && requestedItems.Count > 0;

        if (!hasCombos && !hasSingles)
        {
            Debug.LogWarning("[NPCOrder1] No singles or combos set to randomize from.");
            _orderFulfilled = true;
            return;
        }

        // 50/50 between combos and singles if both exist, else pick whichever exists
        bool pickCombo = hasCombos && (!hasSingles || UnityEngine.Random.value < 0.5f);

        if (pickCombo)
        {
            var combo = prefabCombos[UnityEngine.Random.Range(0, prefabCombos.Count)];
            ApplyCombo(combo);
            Debug.Log($"[NPCOrder1] Random combo set: {CurrentRequestName}");
        }
        else
        {
            var chosen = requestedItems[UnityEngine.Random.Range(0, requestedItems.Count)];
            _pendingOrderNames.Add(CleanName(chosen.name));
            Debug.Log($"[NPCOrder1] Random single set: {CurrentRequestName}");
        }
    }

    // =============================
    //  SPECIFIC ORDER (by string)
    // =============================
    /// <summary>
    /// Force a specific order by name or combo, e.g. "Burger" or "Burger+Fries+Soda".
    /// Names are case-insensitive; "(Clone)" ignored. Any unknown names are skipped with a warning.
    /// </summary>
    public void SetOrderByName(string namesPlusSeparated)
    {
        _pendingOrderNames.Clear();
        _orderFulfilled = false;

        if (string.IsNullOrWhiteSpace(namesPlusSeparated))
        {
            Debug.LogWarning("[NPCOrder1] Empty combo string passed to SetOrderByName.");
            return;
        }

        var parts = namesPlusSeparated.Split('+');
        foreach (var raw in parts)
        {
            string trimmed = CleanName(raw);
            if (IsNameInAnyMenu(trimmed))
                _pendingOrderNames.Add(trimmed);
            else
                Debug.LogWarning($"[NPCOrder1] '{trimmed}' not found in singles or combos.");
        }

        if (_pendingOrderNames.Count == 0)
        {
            _orderFulfilled = true; // nothing valid; treat as completed
            Debug.LogWarning("[NPCOrder1] No valid items in order; marked fulfilled.");
        }
        else
        {
            Debug.Log($"[NPCOrder1] Order set: {CurrentRequestName}");
        }
    }

    // =============================
    //  SPECIFIC ORDER (by prefab combo index / name)
    // =============================
    public void SetOrderByComboIndex(int index)
    {
        if (prefabCombos == null || prefabCombos.Count == 0)
        {
            Debug.LogWarning("[NPCOrder1] No prefab combos defined.");
            return;
        }
        if (index < 0 || index >= prefabCombos.Count)
        {
            Debug.LogWarning($"[NPCOrder1] Combo index {index} out of range.");
            return;
        }

        _orderFulfilled = false;
        _pendingOrderNames.Clear();
        ApplyCombo(prefabCombos[index]);
        Debug.Log($"[NPCOrder1] Combo set by index: {CurrentRequestName}");
    }

    public void SetOrderByComboName(string comboName)
    {
        if (string.IsNullOrWhiteSpace(comboName)) return;
        if (prefabCombos == null || prefabCombos.Count == 0)
        {
            Debug.LogWarning("[NPCOrder1] No prefab combos defined.");
            return;
        }

        var combo = prefabCombos.FirstOrDefault(c => string.Equals(c.displayName, comboName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (combo == null)
        {
            Debug.LogWarning($"[NPCOrder1] No combo found with name '{comboName}'.");
            return;
        }

        _orderFulfilled = false;
        _pendingOrderNames.Clear();
        ApplyCombo(combo);
        Debug.Log($"[NPCOrder1] Combo set by name: {CurrentRequestName}");
    }

    // =============================
    //  INTERACTION / DELIVERY
    // =============================
    /// <summary>
    /// Called by NPCInteractable when the player interacts.
    /// If no active order, we randomize once (keeps original behavior).
    /// If the player is holding a correct item, consume it and progress the combo.
    /// </summary>
    public bool StartOrTryFulfill(PlayerInventory player)
    {
        // No order yet? Set one up (keeps original behavior).
        if (!HasActiveOrder)
        {
            RandomizeOrder();
            return true; // NPC will show the request line
        }

        if (player == null || player.heldVisual == null)
            return false;

        string heldName = CleanName(player.heldVisual.name);

        // Is the held item part of the remaining combo?
        int idx = _pendingOrderNames.FindIndex(n => n.Equals(heldName, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            //  correct item delivered
            _pendingOrderNames.RemoveAt(idx);

            DeliveredDish = player.heldVisual;     // hand reference so NPCInteractable can award stars
            player.ClearHeldItemDirect();          // consume the player's held item

            if (_pendingOrderNames.Count == 0)
            {
                _orderFulfilled = true;
                OnOrderFulfilled?.Invoke();
                Debug.Log("[NPCOrder1] Order complete!");
            }
            else
            {
                Debug.Log($"[NPCOrder1] Accepted '{heldName}'. Still needs: {CurrentRequestName}");
            }

            return true;
        }

        //  wrong item
        return false;
    }

    // =============================
    //  HELPERS
    // =============================
    private void ApplyCombo(OrderCombo combo)
    {
        _pendingOrderNames.Clear();
        if (combo == null || combo.items == null) return;

        foreach (var go in combo.items)
        {
            if (go == null) continue;
            _pendingOrderNames.Add(CleanName(go.name));
        }

        // If combo is empty, mark fulfilled
        _orderFulfilled = _pendingOrderNames.Count == 0;
    }

    private bool IsNameInAnyMenu(string cleanName)
    {
        bool inSingles = requestedItems != null &&
                         requestedItems.Any(go => go && CleanName(go.name).Equals(cleanName, StringComparison.OrdinalIgnoreCase));

        bool inCombos = prefabCombos != null &&
                        prefabCombos.Any(c => c != null && c.items != null &&
                            c.items.Any(go => go && CleanName(go.name).Equals(cleanName, StringComparison.OrdinalIgnoreCase)));

        return inSingles || inCombos;
    }

    private static string CleanName(string n)
    {
        if (string.IsNullOrEmpty(n)) return "";
        return n.Replace("(Clone)", "").Trim();
    }
}

[Serializable]
public class OrderCombo
{
    [Tooltip("Optional label for this combo (used by SetOrderByComboName).")]
    public string displayName;

    [Tooltip("Prefabs that make up this combo (names must match delivered GameObjects).")]
    public List<GameObject> items = new List<GameObject>();
}
