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

public class NPCOrder1 : MonoBehaviour
{
    [Header("Menu Source")]
    public NPCMenuData menuData;   

    [Header("Menu Items (Single)")]
    [Tooltip("All valid single dishes this NPC can request. Names must match delivered GameObject names (case-insensitive; '(Clone)' ignored).")]
    public List<GameObject> requestedItems = new List<GameObject>();

    [Header("Menu Items (Prefab Combos)")]
    [Tooltip("Prefab-defined combos. Each combo is a list of prefabs that must be delivered one-by-one, in any order.")]
    public List<OrderCombo> prefabCombos = new List<OrderCombo>();

    // Events
    public event Action OnOrderFulfilled;
    public event Action<string, bool> OnItemAccepted;

    // Internal state
    private readonly List<string> _pendingOrderNames = new List<string>();
    private bool _orderFulfilled;

    public GameObject DeliveredDish { get; set; }
    public bool HasActiveOrder => _pendingOrderNames.Count > 0 && !_orderFulfilled;
    public string CurrentRequestName => _pendingOrderNames.Count == 0 ? string.Empty : string.Join(" + ", _pendingOrderNames);
    public int CurrentRequestIndex => 0;
    public int OriginalOrderCount { get; private set; }
    public OrderInteractionResult LastInteractionResult { get; private set; }
    public List<GameObject> DeliveredItems { get; private set; } = new List<GameObject>();

    // DEBUG: log what this NPC can order
    void Start()
    {
        Debug.Log($"[NPCOrder1:{name}] Menu at Start:");

        if (requestedItems != null)
        {
            foreach (var go in requestedItems)
            {
                if (go != null)
                    Debug.Log($"  Single: {go.name}");
            }
        }

        if (prefabCombos != null)
        {
            foreach (var combo in prefabCombos)
            {
                if (combo == null) continue;
                Debug.Log($"  Combo: {combo.displayName}");
                if (combo.items != null)
                {
                    foreach (var go in combo.items)
                    {
                        if (go != null)
                            Debug.Log($"    - {go.name}");
                    }
                }
            }
        }
    }

    void Awake()
    {
        // Load from menuData so this NPC is locked to that menu
        if (menuData != null)
        {
            requestedItems = new List<GameObject>(menuData.singleItems);
            prefabCombos = new List<OrderCombo>(menuData.prefabCombos);
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

        bool hasCombos = prefabCombos != null && prefabCombos.Count > 0;
        bool hasSingles = requestedItems != null && requestedItems.Count > 0;

        if (!hasCombos && !hasSingles)
        {
            //  ONLY here, when the menu is really empty
            Debug.LogWarning($"[NPCOrder1:{name}] No singles or combos set to randomize from.");
            _orderFulfilled = true;
            return;
        }

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

        OriginalOrderCount = _pendingOrderNames.Count;
    }


    // =============================
    // SPECIFIC ORDER (by string)
    // =============================
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
            _orderFulfilled = true;
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
        OriginalOrderCount = _pendingOrderNames.Count;
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

        var combo = prefabCombos.FirstOrDefault(c =>
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
    public bool StartOrTryFulfill(PlayerInventory player)
    {
        LastInteractionResult = OrderInteractionResult.None;

        if (!HasActiveOrder)
        {
            RandomizeOrder();
            if (HasActiveOrder)
                LastInteractionResult = OrderInteractionResult.OrderCreated;

            return true;
        }

        if (player == null || player.heldVisual == null) return false;

        string heldName = CleanName(player.heldVisual.name);

        int idx = _pendingOrderNames.FindIndex(n =>
            n.Equals(heldName, StringComparison.OrdinalIgnoreCase));

        if (idx >= 0)
        {
            _pendingOrderNames.RemoveAt(idx);
            DeliveredDish = player.heldVisual;
            DeliveredItems.Add(player.heldVisual);
            player.ClearHeldItemDirect();

            bool isComplete = _pendingOrderNames.Count == 0;

            LastInteractionResult = isComplete
                ? OrderInteractionResult.ItemAcceptedAndCompleted
                : OrderInteractionResult.ItemAcceptedInProgress;

            var dishRef = DeliveredDish.GetComponent<DishReference>();
            if (dishRef != null)
                LevelManager.Instance.AddStars(dishRef.starsEarned);

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
        _orderFulfilled = _pendingOrderNames.Count == 0;
    }

    private bool IsNameInAnyMenu(string cleanName)
    {
        bool inSingles = requestedItems != null &&
                         requestedItems.Any(go =>
                             go && CleanName(go.name)
                                 .Equals(cleanName, StringComparison.OrdinalIgnoreCase));

        bool inCombos = prefabCombos != null &&
                        prefabCombos.Any(c =>
                            c != null && c.items != null &&
                            c.items.Any(go =>
                                go && CleanName(go.name)
                                    .Equals(cleanName, StringComparison.OrdinalIgnoreCase)));

        return inSingles || inCombos;
    }

    private static string CleanName(string n) =>
        string.IsNullOrEmpty(n) ? "" : n.Replace("(Clone)", "").Trim();
}
