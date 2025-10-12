using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class CookRecipe
{
    public string dishName;

    // Names of the required inputs (e.g., ["Chicken", "ChoppedGarlic", "Oil"])
    public List<string> requiredInputs = new List<string>();

    // Optional: per-slot raw prefabs for staging the actual raw piece (same size/order as requiredInputs).
    // If a slot prefab is null, we fall back to the player's held visual.
    public List<GameObject> requiredPrefabs = new List<GameObject>();

    // Per-slot STAGE visuals (SAME length/order as requiredInputs).
    // When slot i is placed, stagePrefabs[i] is shown (remains visible during cooking).
    public List<GameObject> stagePrefabs = new List<GameObject>();

    // Final cooked dish
    public GameObject outputPrefab;
}

public class Stove : MonoBehaviour
{
    // -------- Public (Inspector) --------
    public float cookTime = 3f;              // First cook duration; burn also uses this duration
    public Transform displayPoint;           // Base reference for slot poses
    public Transform resultPoint;            // Where cooked result appears (fallback to displayPoint)
    public List<CookRecipe> recipes = new List<CookRecipe>();

    // -------- Private runtime --------
    PlayerInventory currentPlayer;
    Animator playerAnimator;

    class StagedItem
    {
        public string name;
        public int slotIndex;                // index in recipe.requiredInputs
        public GameObject rawInstance;       // optional per-piece visual (only used if not relying solely on stage visuals)
        public GameObject sourcePrefab;      // prefab to give back to the player's hand
    }

    CookRecipe currentRecipe;                // locked by first placed valid item
    readonly List<StagedItem> staged = new();

    // Per-slot stage visual instances (same length as requiredInputs):
    List<GameObject> stageInstances;         // null entries = not shown yet

    bool isCooking;
    float cookProgress;
    Coroutine cookRoutine;

    GameObject cookedSpawnedObject;
    bool hasCookedItem;
    Coroutine burnRoutine;                   // second cycle (burn)

    // ------------- Triggers -------------
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory p))
        {
            currentPlayer = p;
            playerAnimator = p.animator;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory p) && p == currentPlayer)
        {
            // Stove continues cooking/burning even if player leaves.
            currentPlayer = null;
            playerAnimator = null;
        }
    }

    // ------------- Input (tap-only) -------------
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // 1) Hands empty + cooked ready -> pick up cooked
        if (hasCookedItem && cookedSpawnedObject != null && currentPlayer != null && !currentPlayer.IsHoldingItem())
        {
            PickupCookedResult();
            return;
        }

        // 2) Place a piece from hand (if any)
        if (currentPlayer != null && (currentPlayer.HasIngredient() || currentPlayer.heldVisual != null))
        {
            TryPlaceFromHand();
            return;
        }

        // 3) Hands empty + staged exists + not cooking -> pick up last staged (reset progress & visuals)
        if (!isCooking && staged.Count > 0 && currentPlayer != null && !currentPlayer.IsHoldingItem())
        {
            PickupLastStaged();
            return;
        }
    }

    // ------------- Place logic -------------
    void TryPlaceFromHand()
    {
        string heldName = ResolveHeldName(currentPlayer);
        if (string.IsNullOrWhiteSpace(heldName)) return;

        // Lock recipe on first valid item
        if (currentRecipe == null)
        {
            currentRecipe = FindBestRecipeForFirst(heldName);
            if (currentRecipe == null)
            {
                Debug.LogWarning($"[Stove] No recipe accepts '{heldName}'.");
                return;
            }
            EnsureStageInstanceList(); // create per-slot stage instance list
        }

        // Find next slot for this name
        int nextSlot = NextAvailableSlotIndex(heldName);
        if (nextSlot == -1)
        {
            Debug.LogWarning($"[Stove] '{heldName}' not needed or already placed for '{currentRecipe.dishName}'.");
            return;
        }

        // Choose raw visual for this slot
        GameObject srcPrefab = null;
        if (currentRecipe.requiredPrefabs != null &&
            nextSlot < currentRecipe.requiredPrefabs.Count)
            srcPrefab = currentRecipe.requiredPrefabs[nextSlot];
        if (srcPrefab == null) srcPrefab = currentPlayer.heldVisual;

        // Spawn raw per-piece object only if you want to see the raw items themselves.
        // We'll keep this ON (instantiated) so you can still see pieces in addition to stage visuals.
        GameObject rawInst = null;
        if (srcPrefab != null)
        {
            Vector3 pos; Quaternion rot;
            GetSlotPose(nextSlot, out pos, out rot);
            rawInst = Instantiate(srcPrefab, pos, rot);
            MakeStatic(rawInst);
        }

        // Track staged
        staged.Add(new StagedItem
        {
            name = heldName,
            slotIndex = nextSlot,
            rawInstance = rawInst,
            sourcePrefab = srcPrefab
        });

        // Update the per-slot stage visual for THIS slot (show stagePrefabs[nextSlot] if set)
        UpdateStageVisualForSlot(nextSlot, true);

        // Clear player's hand
        if (currentPlayer.heldVisual != null) Destroy(currentPlayer.heldVisual);
        currentPlayer.ClearHeldItemDirect();

        // Auto-start cooking when complete
        if (staged.Count >= currentRecipe.requiredInputs.Count)
        {
            BeginCook();
        }
    }

    // ------------- Cook flow -------------
    void BeginCook()
    {
        if (isCooking) return;
        if (currentRecipe == null || staged.Count < currentRecipe.requiredInputs.Count) return;

        // Keep stage visuals visible during cooking (per your request).
        // Snap raw instances back to slot poses (if stove moved).
        foreach (var it in staged)
        {
            if (it.rawInstance == null) continue;
            Vector3 pos; Quaternion rot;
            GetSlotPose(it.slotIndex, out pos, out rot);
            it.rawInstance.transform.SetPositionAndRotation(pos, rot);
        }

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
        // Consume raw per-piece visuals
        foreach (var it in staged)
            if (it.rawInstance != null) Destroy(it.rawInstance);
        staged.Clear();

        // Stage visuals remain visible DURING cooking.
        // Now that cooking is finished and result will appear, we remove stage visuals:
        DestroyAllStageVisuals();

        // Spawn cooked result
        Transform pt = resultPoint != null ? resultPoint : displayPoint;
        if (currentRecipe != null && currentRecipe.outputPrefab != null && pt != null)
        {
            cookedSpawnedObject = Instantiate(currentRecipe.outputPrefab, pt.position, pt.rotation);
            MakeStatic(cookedSpawnedObject);
            hasCookedItem = true;

            // Start a burn timer (second full cookTime)
            if (burnRoutine != null) StopCoroutine(burnRoutine);
            burnRoutine = StartCoroutine(BurnRoutine());
        }

        isCooking = false;
        cookRoutine = null;
        cookProgress = 0f;
    }

    IEnumerator BurnRoutine()
    {
        float t = 0f;
        while (t < cookTime && cookedSpawnedObject != null)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Not picked up in time → burnt
        if (cookedSpawnedObject != null)
        {
            Destroy(cookedSpawnedObject);
            cookedSpawnedObject = null;
            hasCookedItem = false;
            Debug.Log("Nasunog na");
        }

        // Full reset after burn
        currentRecipe = null;
        stageInstances = null;
        burnRoutine = null;
    }

    // ------------- Pickup logic -------------
    void PickupCookedResult()
    {
        if (!hasCookedItem || cookedSpawnedObject == null) return;

        string cookedName = currentRecipe != null
            ? currentRecipe.outputPrefab.name.Replace("(Clone)", "")
            : cookedSpawnedObject.name.Replace("(Clone)", "");

        GameObject handPrefab = currentRecipe != null ? currentRecipe.outputPrefab : cookedSpawnedObject;
        currentPlayer.PickUpIngredient(cookedName, handPrefab);

        Destroy(cookedSpawnedObject);
        cookedSpawnedObject = null;
        hasCookedItem = false;

        if (burnRoutine != null) StopCoroutine(burnRoutine);
        burnRoutine = null;

        // Reset for a new dish
        currentRecipe = null;
        stageInstances = null;
        cookProgress = 0f;
    }

    void PickupLastStaged()
    {
        if (staged.Count == 0) return;

        // pop last
        var it = staged[staged.Count - 1];
        staged.RemoveAt(staged.Count - 1);

        currentPlayer.PickUpIngredient(it.name, it.sourcePrefab);
        if (it.rawInstance != null) Destroy(it.rawInstance);

        // Hide the stage visual for that slot
        UpdateStageVisualForSlot(it.slotIndex, false);

        // Reset progress; if nothing remains, reset recipe and visuals collection
        cookProgress = 0f;
        if (staged.Count == 0)
        {
            currentRecipe = null;
            DestroyAllStageVisuals();
            stageInstances = null;
        }
    }

    // ------------- Stage visuals (per-slot) -------------
    void EnsureStageInstanceList()
    {
        if (currentRecipe == null || currentRecipe.requiredInputs == null) return;
        if (stageInstances != null && stageInstances.Count == currentRecipe.requiredInputs.Count) return;

        stageInstances = new List<GameObject>(currentRecipe.requiredInputs.Count);
        for (int i = 0; i < currentRecipe.requiredInputs.Count; i++) stageInstances.Add(null);
    }

    void UpdateStageVisualForSlot(int slotIndex, bool show)
    {
        if (currentRecipe == null) return;
        EnsureStageInstanceList();

        // Validate slot & prefab
        if (slotIndex < 0 || slotIndex >= currentRecipe.requiredInputs.Count) return;

        GameObject wantedPrefab = null;
        if (currentRecipe.stagePrefabs != null && slotIndex < currentRecipe.stagePrefabs.Count)
            wantedPrefab = currentRecipe.stagePrefabs[slotIndex];

        // If no prefab configured for this slot, just ignore
        if (wantedPrefab == null)
        {
            // If hiding, make sure to destroy any leftover
            if (!show) DestroyStageVisualAt(slotIndex);
            return;
        }

        if (show)
        {
            // Already showing? keep it
            if (stageInstances[slotIndex] != null) return;

            // Spawn stage visual at this slot's pose
            Vector3 pos; Quaternion rot;
            GetSlotPose(slotIndex, out pos, out rot);
            var inst = Instantiate(wantedPrefab, pos, rot);
            MakeStatic(inst);
            stageInstances[slotIndex] = inst;
        }
        else
        {
            DestroyStageVisualAt(slotIndex);
        }
    }

    void DestroyStageVisualAt(int slotIndex)
    {
        if (stageInstances == null) return;
        if (slotIndex < 0 || slotIndex >= stageInstances.Count) return;

        if (stageInstances[slotIndex] != null)
        {
            Destroy(stageInstances[slotIndex]);
            stageInstances[slotIndex] = null;
        }
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

    bool HasAnyStageVisualConfigured()
    {
        if (currentRecipe == null || currentRecipe.stagePrefabs == null) return false;
        foreach (var p in currentRecipe.stagePrefabs) if (p != null) return true;
        return false;
    }

    // ------------- Helpers -------------
    string ResolveHeldName(PlayerInventory inv)
    {
        if (!string.IsNullOrWhiteSpace(inv.heldIngredient)) return inv.heldIngredient.Trim();
        if (inv.heldVisual != null) return inv.heldVisual.name.Replace("(Clone)", "").Trim();
        return "";
    }

    CookRecipe FindBestRecipeForFirst(string firstName)
    {
        CookRecipe best = null;
        int bestReqCount = int.MaxValue;
        foreach (var r in recipes)
        {
            if (r == null || r.requiredInputs == null) continue;
            if (r.outputPrefab == null) continue;
            if (r.requiredInputs.Contains(firstName))
            {
                // Prefer the fewest required parts (simple tie-breaker)
                if (r.requiredInputs.Count < bestReqCount)
                {
                    best = r;
                    bestReqCount = r.requiredInputs.Count;
                }
            }
        }
        return best;
    }

    int NextAvailableSlotIndex(string name)
    {
        if (currentRecipe == null || currentRecipe.requiredInputs == null) return -1;

        // Used slots
        var used = new HashSet<int>();
        foreach (var it in staged) used.Add(it.slotIndex);

        for (int i = 0; i < currentRecipe.requiredInputs.Count; i++)
        {
            if (used.Contains(i)) continue;
            if (string.Equals(currentRecipe.requiredInputs[i], name, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    void GetSlotPose(int slotIndex, out Vector3 pos, out Quaternion rot)
    {
        Transform basePt = displayPoint != null ? displayPoint : transform;
        int total = (currentRecipe != null && currentRecipe.requiredInputs != null)
            ? currentRecipe.requiredInputs.Count : 1;

        // Spread across local right axis for clarity
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
