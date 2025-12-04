using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum OrderInteractionResult
{
    None,
    OrderCreated,
    ItemAcceptedInProgress,
    ItemAcceptedAndCompleted
}

/// <summary>
/// Handles which items an NPC requests and tracks whether they have been fulfilled.
/// Supports:
/// - Single items (from NPCMenuData or local requestedItems)
/// - Inspector-authored prefab combos (from NPCMenuData or local prefabCombos)
/// - Forcing a combo by index or name
/// - Randomizing from singles or combos
/// </summary>
public class NPCOrder1 : MonoBehaviour
{
    [Header("Menu Data Source (Per NPC)")]
    [Tooltip("If set, this ScriptableObject is the local menu for THIS NPC (e.g., Menu_Level 2). " +
             "If left empty, the fallback lists below are used instead.")]
    [SerializeField] private NPCMenuData menuData;

    [Header("Fallback Menu Items (Single)")]
    [Tooltip("Used ONLY if no NPCMenuData is assigned or its singles list is empty.")]
    public List<GameObject> requestedItems = new List<GameObject>();

    [Header("Fallback Menu Items (Prefab Combos)")]
    [Tooltip("Used ONLY if no NPCMenuData is assigned or its combos list is empty.")]
    public List<OrderCombo> prefabCombos = new List<OrderCombo>();

    /// <summary>Raised when the last required item has been delivered.</summary>
    public event Action OnOrderFulfilled;

    /// <summary>
    /// Raised whenever a correct item is delivered.
    /// string = delivered item name, bool = isOrderComplete
    /// </summary>
    public event Action<string, bool> OnItemAccepted;

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

    /// <summary>
    /// How many items were in the order when it was first created.
    /// Used to detect combos vs single orders.
    /// </summary>
    public int OriginalOrderCount { get; private set; }

    /// <summary>
    /// Last result of StartOrTryFulfill, so callers can know what happened.
    /// </summary>
    public OrderInteractionResult LastInteractionResult { get; private set; }

    /// <summary>All items delivered so far for the current order.</summary>
    public List<GameObject> DeliveredItems { get; private set; } = new List<GameObject>();

    // Helpers to always use NPCMenuData first, then fallback lists
    private List<GameObject> ActiveSingles
    {
        get
        {
            if (menuData != null && menuData.singleItems != null && menuData.singleItems.Count > 0)
                return menuData.singleItems;
            return requestedItems;
        }
    }

    private List<OrderCombo> ActiveCombos
    {
        get
        {
            if (menuData != null && menuData.prefabCombos != null && menuData.prefabCombos.Count > 0)
                return menuData.prefabCombos;
            return prefabCombos;
        }
    }

    // =============================
    // RANDOM ORDER
    // =============================
    public void RandomizeOrder()
    {
        _pendingOrderNames.Clear();
        _orderFulfilled = false;
        OriginalOrderCount = 0;

        var singles = ActiveSingles;
        var combos = ActiveCombos;

        bool hasCombos = combos != null && combos.Count > 0;
        bool hasSingles = singles != null && singles.Count > 0;

        if (!hasCombos && !hasSingles)
        {
            //  ONLY here, when the menu is really empty
            Debug.LogWarning($"[NPCOrder1:{name}] No singles or combos set to randomize from.");
            _orderFulfilled = true;
            return;
        }

        // 50/50 between combos and singles if both exist
        bool pickCombo = hasCombos && (!hasSingles || UnityEngine.Random.value < 0.5f);
        if (pickCombo)
        {
            var combo = combos[UnityEngine.Random.Range(0, combos.Count)];
            ApplyCombo(combo);
            Debug.Log($"[NPCOrder1] Random combo set: {CurrentRequestName}");
        }
        else
        {
            var chosen = singles[UnityEngine.Random.Range(0, singles.Count)];
            _pendingOrderNames.Add(CleanName(chosen.name));
            Debug.Log($"[NPCOrder1] Random single set: {CurrentRequestName}");
        }

        OriginalOrderCount = _pendingOrderNames.Count;
    }


    // =============================
    // SPECIFIC ORDER (by string)
    // =============================
    /// <summary>
    /// Force a specific order by name or combo, e.g. "Burger" or "Burger+Fries+Soda".
    /// Names are case-insensitive; "(Clone)" ignored. Any unknown names are skipped with a warning.
    /// </summary>
    public void SetOrderByName(string namesPlusSeparated)
    {
        _pendingOrderNames.Clear();
        _orderFulfilled = false;
        OriginalOrderCount = 0;

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
            OriginalOrderCount = _pendingOrderNames.Count;
            Debug.Log($"[NPCOrder1] Order set: {CurrentRequestName}");
        }
    }

    // =============================
    // SPECIFIC ORDER (by prefab combo index / name)
    // =============================
    public void SetOrderByComboIndex(int index)
    {
        var combos = ActiveCombos;

        if (combos == null || combos.Count == 0)
        {
            Debug.LogWarning("[NPCOrder1] No prefab combos defined.");
            return;
        }
        if (index < 0 || index >= combos.Count)
        {
            Debug.LogWarning($"[NPCOrder1] Combo index {index} out of range.");
            return;
        }

        _orderFulfilled = false;
        _pendingOrderNames.Clear();
        ApplyCombo(combos[index]);
        OriginalOrderCount = _pendingOrderNames.Count;
        Debug.Log($"[NPCOrder1] Combo set by index: {CurrentRequestName}");
    }

    public void SetOrderByComboName(string comboName)
    {
        if (string.IsNullOrWhiteSpace(comboName)) return;

        var combos = ActiveCombos;

        if (combos == null || combos.Count == 0)
        {
            Debug.LogWarning("[NPCOrder1] No prefab combos defined.");
            return;
        }

        var combo = combos.FirstOrDefault(c =>
            string.Equals(c.displayName, comboName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (combo == null)
        {
            Debug.LogWarning($"[NPCOrder1] No combo found with name '{comboName}'.");
            return;
        }

        _orderFulfilled = false;
        _pendingOrderNames.Clear();
        ApplyCombo(combo);
        OriginalOrderCount = _pendingOrderNames.Count;
        Debug.Log($"[NPCOrder1] Combo set by name: {CurrentRequestName}");
    }

    // =============================
    // INTERACTION / DELIVERY
    // =============================
    /// <summary>
    /// Called by NPCInteractable when the player interacts.
    /// If no active order, we randomize once (keeps original behavior).
    /// If the player is holding a correct item, consume it and progress the combo.
    /// </summary>
    public bool StartOrTryFulfill(PlayerInventory player)
    {
        LastInteractionResult = OrderInteractionResult.None;

        // No order yet? Set one up
        if (!HasActiveOrder)
        {
            RandomizeOrder();
            if (HasActiveOrder)
            {
                LastInteractionResult = OrderInteractionResult.OrderCreated;
            }
            return true; // NPC will show the request line
        }

        if (player == null || player.heldVisual == null) return false;

        string heldName = CleanName(player.heldVisual.name);

        // Is the held item part of the remaining combo?
        int idx = _pendingOrderNames.FindIndex(n => n.Equals(heldName, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            // Correct item delivered
            _pendingOrderNames.RemoveAt(idx);
            DeliveredDish = player.heldVisual; // hand reference so NPCInteractable can award stars
            DeliveredItems.Add(player.heldVisual);
            player.ClearHeldItemDirect();      // consume the player's held item

            bool isComplete = _pendingOrderNames.Count == 0;

            LastInteractionResult = isComplete
                ? OrderInteractionResult.ItemAcceptedAndCompleted
                : OrderInteractionResult.ItemAcceptedInProgress;

            // Notify listeners for mid-combo / per-item dialogue
            OnItemAccepted?.Invoke(heldName, isComplete);

            if (isComplete)
            {
                _orderFulfilled = true;
                OnOrderFulfilled?.Invoke();
                Debug.Log("[NPCOrder1] Order complete!");

                DeliveredItems.Clear();
            }
            else
            {
                Debug.Log($"[NPCOrder1] Accepted '{heldName}'. Still needs: {CurrentRequestName}");
            }
            return true;
        }

        // Wrong item
        return false;
    }

    // =============================
    // HELPERS
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

        OriginalOrderCount = _pendingOrderNames.Count;
        // If combo is empty, mark fulfilled
        _orderFulfilled = _pendingOrderNames.Count == 0;
    }

    private bool IsNameInAnyMenu(string cleanName)
    {
        var singles = ActiveSingles;
        var combos = ActiveCombos;

        bool inSingles = singles != null &&
                         singles.Any(go => go && CleanName(go.name)
                             .Equals(cleanName, StringComparison.OrdinalIgnoreCase));

        bool inCombos = combos != null &&
                        combos.Any(c => c != null && c.items != null &&
                            c.items.Any(go => go && CleanName(go.name)
                                .Equals(cleanName, StringComparison.OrdinalIgnoreCase)));

        return inSingles || inCombos;
    }

    private static string CleanName(string n) =>
        string.IsNullOrEmpty(n) ? "" : n.Replace("(Clone)", "").Trim();
}
