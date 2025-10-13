using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class CookRecipe
{
    public string dishName;
    // Match by prefab NAME (clone-safe). Any order is fine.
    public List<GameObject> requiredPrefabs = new List<GameObject>();
    // stagePrefabs[0] after first piece, [1] after second, etc. (single stage shown at a time)
    public List<GameObject> stagePrefabs = new List<GameObject>();
    // Given to player when they interact after cooking is done
    public GameObject outputPrefab;
}

public class Stove : MonoBehaviour
{
    [Tooltip("Seconds to complete cooking; burn window uses the same duration again")]
    public float cookTime = 3f;

    [Tooltip("Where to place the stage visuals")]
    public Transform displayPoint;

    [Tooltip("Local offset from Display Point (use to lift off the surface)")]
    public Vector3 displayOffset = Vector3.zero;

    [Tooltip("(Optional) not used for output now; kept for future use")]
    public Transform resultPoint;

    public List<CookRecipe> recipes = new List<CookRecipe>();

    // ---- runtime ----
    PlayerInventory currentPlayer;

    CookRecipe currentRecipe;
    bool[] slotFilled;
    int placedCount;

    GameObject stageInstance;      // only one stage shown at a time
    int currentStageIndex = -1;

    bool isCooking;
    float cookProgress;
    Coroutine cookRoutine;

    bool cookedReady;
    Coroutine burnRoutine;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory p)) currentPlayer = p;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory p) && p == currentPlayer) currentPlayer = null;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // cooked ready → hand to player
        if (cookedReady && currentPlayer != null && !currentPlayer.IsHoldingItem())
        {
            GiveCookedToPlayer();
            return;
        }

        // try placing from hand
        if (currentPlayer != null && (currentPlayer.HasIngredient() || currentPlayer.heldVisual != null))
        {
            TryPlaceFromHand();
        }
    }

    void TryPlaceFromHand()
    {
        string heldName = ResolveHeldName(currentPlayer);
        if (string.IsNullOrWhiteSpace(heldName)) return;

        // lock a recipe
        if (currentRecipe == null)
        {
            currentRecipe = FindRecipeByFirstPrefabName(heldName);
            if (currentRecipe == null) { Debug.LogWarning($"[Stove] No recipe accepts '{heldName}'"); return; }
            InitRecipeState(currentRecipe);
        }

        // find free slot for this prefab
        int slot = NextFreeMatchingSlot(heldName);
        if (slot == -1) { Debug.LogWarning($"[Stove] '{heldName}' not needed or already filled."); return; }

        // consume from hand
        if (currentPlayer.heldVisual != null) Destroy(currentPlayer.heldVisual);
        currentPlayer.ClearHeldItemDirect();

        // mark filled
        slotFilled[slot] = true;
        placedCount = CountFilled();

        // show the ONE progress stage: index = placedCount - 1
        UpdateStageVisual(placedCount - 1);

        // done placing? cook!
        if (AllSlotsFilled()) BeginCook();
    }

    void BeginCook()
    {
        if (isCooking) return;
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
        // swap to final stage (Stage 3 if exists, else last provided)
        int finalIdx = GetFinalStageIndex();
        UpdateStageVisual(finalIdx);

        // 👇 NEW: log when cooking completes
        string dish = currentRecipe != null ? currentRecipe.dishName : "Dish";
        Debug.Log($"[Stove] Finished cooking '{dish}'. Showing final stage index = {finalIdx}.");

        cookedReady = true;

        if (burnRoutine != null) StopCoroutine(burnRoutine);
        burnRoutine = StartCoroutine(BurnRoutine());

        isCooking = false;
        cookRoutine = null;
        cookProgress = 0f;
    }

    IEnumerator BurnRoutine()
    {
        float t = 0f;
        while (t < cookTime && cookedReady) { t += Time.deltaTime; yield return null; }

        if (cookedReady)
        {
            if (stageInstance != null) Destroy(stageInstance);
            stageInstance = null;
            currentStageIndex = -1;
            cookedReady = false;
            Debug.Log("Nasunog na");
            ResetRecipeState();
        }
        burnRoutine = null;
    }

    void GiveCookedToPlayer()
    {
        if (currentRecipe == null || currentRecipe.outputPrefab == null) return;
        if (currentPlayer == null || currentPlayer.IsHoldingItem()) return;

        string cookedName = currentRecipe.outputPrefab.name.Replace("(Clone)", "");
        currentPlayer.PickUpIngredient(cookedName, currentRecipe.outputPrefab);

        if (stageInstance != null) Destroy(stageInstance);
        stageInstance = null;
        currentStageIndex = -1;
        cookedReady = false;

        if (burnRoutine != null) StopCoroutine(burnRoutine);
        burnRoutine = null;

        ResetRecipeState();
    }

    // ----- Stage visual (single) -----
    void UpdateStageVisual(int index)
    {
        if (currentRecipe == null) return;
        if (currentRecipe.stagePrefabs == null || currentRecipe.stagePrefabs.Count == 0) return;

        int maxIdx = currentRecipe.stagePrefabs.Count - 1;
        int desired = Mathf.Clamp(index, 0, maxIdx);

        // same stage shown? nothing to do
        if (currentStageIndex == desired && stageInstance != null) return;

        // destroy old
        if (stageInstance != null)
        {
            Destroy(stageInstance);
            stageInstance = null;
        }

        // spawn new at DisplayPoint + local offset
        GameObject prefab = currentRecipe.stagePrefabs[desired];
        if (prefab != null && displayPoint != null)
        {
            Vector3 pos = displayPoint.position + displayPoint.TransformVector(displayOffset);
            Quaternion rot = displayPoint.rotation;
            stageInstance = Instantiate(prefab, pos, rot);
            MakeStatic(stageInstance);
            currentStageIndex = desired;
        }
        else
        {
            currentStageIndex = -1;
        }
    }

    int GetFinalStageIndex()
    {
        if (currentRecipe == null || currentRecipe.stagePrefabs == null || currentRecipe.stagePrefabs.Count == 0)
            return -1;
        return currentRecipe.stagePrefabs.Count > 2 ? 2 : currentRecipe.stagePrefabs.Count - 1;
    }

    // ----- Helpers -----
    static string Norm(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

    string ResolveHeldName(PlayerInventory inv)
    {
        if (!string.IsNullOrWhiteSpace(inv.heldIngredient)) return inv.heldIngredient.Trim();
        if (inv.heldVisual != null) return inv.heldVisual.name.Replace("(Clone)", "").Trim();
        return "";
    }

    CookRecipe FindRecipeByFirstPrefabName(string heldPrefabName)
    {
        string key = Norm(heldPrefabName);
        CookRecipe best = null; int bestCount = int.MaxValue;

        foreach (var r in recipes)
        {
            if (r == null || r.requiredPrefabs == null || r.requiredPrefabs.Count == 0 || r.outputPrefab == null) continue;

            bool contains = false;
            foreach (var req in r.requiredPrefabs)
            {
                if (req == null) continue;
                if (Norm(req.name) == key) { contains = true; break; }
            }
            if (!contains) continue;

            if (r.requiredPrefabs.Count < bestCount) { best = r; bestCount = r.requiredPrefabs.Count; }
        }
        return best;
    }

    void InitRecipeState(CookRecipe r)
    {
        int n = r.requiredPrefabs.Count;
        slotFilled = new bool[n];
        placedCount = 0;

        if (stageInstance != null) { Destroy(stageInstance); stageInstance = null; }
        currentStageIndex = -1;

        isCooking = false;
        cookProgress = 0f;
        cookedReady = false;
    }

    void ResetRecipeState()
    {
        currentRecipe = null;
        slotFilled = null;
        placedCount = 0;

        if (stageInstance != null) { Destroy(stageInstance); stageInstance = null; }
        currentStageIndex = -1;

        isCooking = false;
        cookProgress = 0f;
        cookedReady = false;
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

    int CountFilled()
    {
        if (slotFilled == null) return 0;
        int c = 0;
        for (int i = 0; i < slotFilled.Length; i++) if (slotFilled[i]) c++;
        return c;
    }

    bool AllSlotsFilled()
    {
        if (slotFilled == null) return false;
        for (int i = 0; i < slotFilled.Length; i++) if (!slotFilled[i]) return false;
        return true;
    }

    void MakeStatic(GameObject go)
    {
        if (go == null) return;
        if (go.TryGetComponent<Collider>(out var c)) c.enabled = false;
        if (go.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;
    }
}
