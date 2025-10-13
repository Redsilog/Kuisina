using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class CookRecipe
{
    [Tooltip("Label for this dish (for debugging)")]
    public string dishName;

    [Tooltip("Prefabs required to build this dish. Order matters for staging positions.")]
    public List<GameObject> requiredPrefabs = new List<GameObject>();

    [Tooltip("Per-slot stage visuals shown when that slot is filled. Same length/order as requiredPrefabs.")]
    public List<GameObject> stagePrefabs = new List<GameObject>();

    [Tooltip("Final cooked result prefab")]
    public GameObject outputPrefab;
}

public class Stove : MonoBehaviour
{
    // -------- Public (Inspector) --------
    [Tooltip("Seconds for the cook to finish (burn uses the same duration again)")]
    public float cookTime = 3f;

    [Tooltip("Base reference for laying out slots (items spread along local Right)")]
    public Transform displayPoint;

    [Tooltip("Where the cooked result spawns (fallback to displayPoint if null)")]
    public Transform resultPoint;

    [Tooltip("Available recipes. The first placed prefab locks which recipe is used.")]
    public List<CookRecipe> recipes = new List<CookRecipe>();

    // -------- Private runtime --------
    PlayerInventory currentPlayer;

    CookRecipe currentRecipe;            // locked when first valid prefab is placed
    bool[] slotFilled;                   // per-slot filled flags
    List<GameObject> stageInstances;     // per-slot stage visuals instances

    bool isCooking;
    float cookProgress;
    Coroutine cookRoutine;

    GameObject cookedSpawnedObject;
    Coroutine burnRoutine;

    // ---------- Trigger ----------
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory p))
        {
            currentPlayer = p;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory p) && p == currentPlayer)
        {
            // Stove continues cooking/burning even if player leaves.
            currentPlayer = null;
        }
    }

    // ---------- Interact (tap-only) ----------
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // 1) If cooked exists in world and player is free -> pick it up
        if (cookedSpawnedObject != null && currentPlayer != null && !currentPlayer.IsHoldingItem())
        {
            PickupCookedResult();
            return;
        }

        // 2) If player is holding something -> try to place it (must match required prefab)
        if (currentPlayer != null && (currentPlayer.HasIngredient() || currentPlayer.heldVisual != null))
        {
            TryPlaceFromHand();
            return;
        }

        // Otherwise do nothing (we’re not supporting unstaging via tap in this simple build)
    }

    // ---------- Placement ----------
    void TryPlaceFromHand()
    {
        // Determine the held object's "prefab name"
        string heldName = ResolveHeldName(currentPlayer);
        if (string.IsNullOrWhiteSpace(heldName)) return;

        // If we haven't locked a recipe yet, pick a recipe that contains this prefab name
        if (currentRecipe == null)
        {
            currentRecipe = FindRecipeByFirstPrefabName(heldName);
            if (currentRecipe == null)
            {
                Debug.LogWarning($"[Stove] No recipe accepts prefab '{heldName}'.");
                return;
            }
            InitRecipeState(currentRecipe);
        }

        // Find a free slot whose required prefab name matches what we're holding
        int slot = NextFreeMatchingSlot(heldName);
        if (slot == -1)
        {
            Debug.LogWarning($"[Stove] '{heldName}' not needed or all its slots filled for '{currentRecipe.dishName}'.");
            return;
        }

        // Show per-slot stage visual (if provided)
        ShowStageVisual(slot);

        // Take the item from the player's hand (destroy visual + clear)
        if (currentPlayer.heldVisual != null) Destroy(currentPlayer.heldVisual);
        currentPlayer.ClearHeldItemDirect();

        // Mark filled and maybe start cooking
        slotFilled[slot] = true;

        if (AllSlotsFilled())
        {
            BeginCook();
        }
    }

    bool AllSlotsFilled()
    {
        if (currentRecipe == null || slotFilled == null) return false;
        for (int i = 0; i < slotFilled.Length; i++)
            if (!slotFilled[i]) return false;
        return true;
    }

    // ---------- Cooking ----------
    void BeginCook()
    {
        if (isCooking) return;

        // Keep stage visuals visible during cooking (as requested)
        isCooking = true;
        if (cookRoutine != null) StopCoroutine(cookRoutine);
        cookRoutine = StartCoroutine(CookRoutine());
    }

    IEnumerator CookRoutine()
    {
        cookProgress = 0f;
        while (cookProgress < cookTime)
        {
            cookProgress += Time.deltaTime;
            yield return null;
        }
        FinishCook();
    }

    void FinishCook()
    {
        // When done cooking:
        // - Remove stage visuals
        // - Spawn cooked result at resultPoint (or displayPoint)
        // - Start burn cycle (second cookTime). If unpicked, destroy + "Nasunog na".

        DestroyAllStageVisuals();

        Transform pt = resultPoint != null ? resultPoint : displayPoint;
        if (currentRecipe != null && currentRecipe.outputPrefab != null && pt != null)
        {
            cookedSpawnedObject = Instantiate(currentRecipe.outputPrefab, pt.position, pt.rotation);
            MakeStatic(cookedSpawnedObject);

            // Start burn timer
            if (burnRoutine != null) StopCoroutine(burnRoutine);
            burnRoutine = StartCoroutine(BurnRoutine());
        }

        // Reset cooking loop
        isCooking = false;
        cookRoutine = null;
        cookProgress = 0f;

        // We keep currentRecipe until picked up or burned, so the pickup uses the correct prefab.
    }

    IEnumerator BurnRoutine()
    {
        float t = 0f;
        while (t < cookTime && cookedSpawnedObject != null)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (cookedSpawnedObject != null)
        {
            Destroy(cookedSpawnedObject);
            cookedSpawnedObject = null;
            Debug.Log("Nasunog na");
        }

        // Full reset after burn
        currentRecipe = null;
        slotFilled = null;
        stageInstances = null;
        burnRoutine = null;
    }

    // ---------- Pickup ----------
    void PickupCookedResult()
    {
        if (cookedSpawnedObject == null || currentPlayer == null) return;

        // Put the cooked prefab into the player's hand (uses the recipe's output prefab)
        if (currentRecipe != null && currentRecipe.outputPrefab != null)
        {
            string cookedName = currentRecipe.outputPrefab.name.Replace("(Clone)", "");
            currentPlayer.PickUpIngredient(cookedName, currentRecipe.outputPrefab);
        }
        else
        {
            // Fallback: hand over the actual object instance (shouldn't happen if recipe is set)
            string cookedName = cookedSpawnedObject.name.Replace("(Clone)", "");
            currentPlayer.PickUpIngredient(cookedName, cookedSpawnedObject);
        }

        // Clean world + cancel burn
        if (cookedSpawnedObject != null)
        {
            Destroy(cookedSpawnedObject);
            cookedSpawnedObject = null;
        }
        if (burnRoutine != null) StopCoroutine(burnRoutine);
        burnRoutine = null;

        // Reset for new dish
        currentRecipe = null;
        slotFilled = null;
        stageInstances = null;
        cookProgress = 0f;
    }

    // ---------- Helpers ----------
    static string Norm(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

    // Resolve the name of what the player is holding (clone-safe)
    string ResolveHeldName(PlayerInventory inv)
    {
        // Prefer the inventory's explicit name if you set it elsewhere
        if (!string.IsNullOrWhiteSpace(inv.heldIngredient))
            return inv.heldIngredient.Trim();

        if (inv.heldVisual != null)
            return inv.heldVisual.name.Replace("(Clone)", "").Trim();

        return "";
    }

    CookRecipe FindRecipeByFirstPrefabName(string heldPrefabName)
    {
        string key = Norm(heldPrefabName);
        CookRecipe best = null;
        int bestCount = int.MaxValue;

        foreach (var r in recipes)
        {
            if (r == null || r.requiredPrefabs == null || r.requiredPrefabs.Count == 0 || r.outputPrefab == null)
                continue;

            bool contains = false;
            foreach (var req in r.requiredPrefabs)
            {
                if (req == null) continue;
                if (Norm(req.name) == key) { contains = true; break; }
            }
            if (!contains) continue;

            // Prefer the recipe with the fewest required items
            if (r.requiredPrefabs.Count < bestCount)
            {
                best = r;
                bestCount = r.requiredPrefabs.Count;
            }
        }
        return best;
    }

    void InitRecipeState(CookRecipe r)
    {
        int n = r.requiredPrefabs.Count;
        slotFilled = new bool[n];
        stageInstances = new List<GameObject>(n);
        for (int i = 0; i < n; i++) stageInstances.Add(null);
    }

    int NextFreeMatchingSlot(string heldPrefabName)
    {
        if (currentRecipe == null || currentRecipe.requiredPrefabs == null) return -1;

        string key = Norm(heldPrefabName);
        for (int i = 0; i < currentRecipe.requiredPrefabs.Count; i++)
        {
            if (slotFilled[i]) continue;
            var req = currentRecipe.requiredPrefabs[i];
            if (req == null) continue;
            if (Norm(req.name) == key) return i;
        }
        return -1;
    }

    void ShowStageVisual(int slotIndex)
    {
        if (currentRecipe == null) return;
        if (slotIndex < 0 || slotIndex >= currentRecipe.requiredPrefabs.Count) return;

        // Already showing? skip
        if (stageInstances[slotIndex] != null) return;

        // Stage visual for that slot (optional)
        GameObject stagePrefab = (currentRecipe.stagePrefabs != null && slotIndex < currentRecipe.stagePrefabs.Count)
            ? currentRecipe.stagePrefabs[slotIndex]
            : null;

        if (stagePrefab == null) return;

        Vector3 pos; Quaternion rot;
        GetSlotPose(slotIndex, out pos, out rot);
        var inst = Instantiate(stagePrefab, pos, rot);
        MakeStatic(inst);
        stageInstances[slotIndex] = inst;
    }

    void DestroyAllStageVisuals()
    {
        if (stageInstances == null) return;
        for (int i = 0; i < stageInstances.Count; i++)
        {
            if (stageInstances[i] != null)
            {
                Destroy(stageInstances[i]);
                stageInstances[i] = null;
            }
        }
    }

    void GetSlotPose(int slotIndex, out Vector3 pos, out Quaternion rot)
    {
        Transform basePt = displayPoint != null ? displayPoint : transform;
        int total = (currentRecipe != null && currentRecipe.requiredPrefabs != null)
            ? currentRecipe.requiredPrefabs.Count : 1;

        // Spread across local right axis
        float spacing = 0.22f;
        float start = -(total - 1) * 0.5f * spacing;
        Vector3 right = basePt.right;
        pos = basePt.position + right * (start + slotIndex * spacing);
        rot = basePt.rotation;
    }

    void MakeStatic(GameObject go)
    {
        if (go == null) return;
        if (go.TryGetComponent<Collider>(out var c)) c.enabled = false;
        if (go.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;
    }
}
