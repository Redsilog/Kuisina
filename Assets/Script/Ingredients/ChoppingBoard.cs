using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class ChopMapping
{
    public string inputName;        // e.g. "Garlic"
    public GameObject inputPrefab;  // raw visual to stage on the board / give back
    public GameObject outputPrefab; // chopped result to spawn
}

public class ChoppingBoard : MonoBehaviour
{
    // ---- Public (Inspector) ----
    public float chopTime = 2f;             // seconds to finish chopping
    public float holdThreshold = 0.25f;     // hold time to count as "hold" vs "tap"
    public Transform displayPoint;          // where raw sits while staged (world spawn)
    public Transform resultPoint;           // optional; if null, uses displayPoint
    public List<ChopMapping> chopMappings = new List<ChopMapping>();

    // ---- Private runtime ----
    PlayerInventory currentPlayer;
    Animator playerAnimator;

    GameObject stagedRawInstance;           // world object (UNPARENTED)
    string stagedRawName;
    GameObject stagedRawSourcePrefab;       // which prefab to give back to hand

    bool isChopping;
    float chopProgress;                     // persists across pauses
    string currentIngredientName;

    GameObject choppedSpawnedObject;        // world object (UNPARENTED)
    GameObject lastChoppedPrefabRef;        // prefab used for result
    bool hasChoppedItem;

    bool buttonPressed;
    float pressStartTime;
    Coroutine holdGateRoutine;

    public float ChopProgress01 => chopTime <= 0f ? 0f : Mathf.Clamp01(chopProgress / chopTime);

    // ---------- Trigger ----------
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
            if (isChopping) PauseChop(); // keep staged + progress
            currentPlayer = null;
            playerAnimator = null;
        }
    }

    // ---------- Interact input ----------
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            buttonPressed = true;
            pressStartTime = Time.time;
            if (holdGateRoutine != null) StopCoroutine(holdGateRoutine);
            holdGateRoutine = StartCoroutine(HoldGate());
        }

        if (ctx.canceled)
        {
            float held = Time.time - pressStartTime;
            buttonPressed = false;

            if (isChopping)
            {
                PauseChop(); // keep progress
                return;
            }

            if (held < holdThreshold)
                TapAction();
        }
    }

    IEnumerator HoldGate()
    {
        float t0 = pressStartTime;
        while (buttonPressed && (Time.time - t0) < holdThreshold) yield return null;
        if (!buttonPressed) yield break;

        if (!isChopping && stagedRawInstance != null && !hasChoppedItem)
            BeginChop();
    }

    // ---------- Tap behavior ----------
    void TapAction()
    {
        // 1) Place from hand (board empty)
        if (!isChopping && stagedRawInstance == null && !hasChoppedItem && currentPlayer != null)
        {
            string name = ResolveHeldName(currentPlayer);
            var map = GetMapping(name);
            if (map != null) { PlaceFromHand(map); return; }
        }

        // 2) Pick up chopped (hands empty)
        if (!isChopping && hasChoppedItem && choppedSpawnedObject != null &&
            currentPlayer != null && !currentPlayer.IsHoldingItem())
        {
            PickupChoppedResult();
            return;
        }

        // 3) Pick up staged raw (hands empty) – resets progress
        if (!isChopping && stagedRawInstance != null &&
            currentPlayer != null && !currentPlayer.IsHoldingItem())
        {
            PickupStagedRaw();
            return;
        }
    }

    // ---------- Chop flow ----------
    void BeginChop()
    {
        SnapToPoint(stagedRawInstance, displayPoint);
        isChopping = true;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsChopping", true);
            playerAnimator.SetBool("IsHoldingWalk", false);
            playerAnimator.SetBool("IsHoldingStill", false);
        }

        StartCoroutine(ChopWhileHeld());
    }

    IEnumerator ChopWhileHeld()
    {
        while (isChopping && chopProgress < chopTime)
        {
            chopProgress += Time.deltaTime;
            yield return null;
        }

        if (!isChopping) yield break;
        if (chopProgress >= chopTime) FinishChop();
    }

    void PauseChop()
    {
        if (!isChopping) return;
        isChopping = false;
        if (playerAnimator != null) playerAnimator.SetBool("IsChopping", false);
        // progress kept
    }

    void FinishChop()
    {
        var map = GetMapping(currentIngredientName);
        lastChoppedPrefabRef = map != null ? map.outputPrefab : null;

        // consume staged
        if (stagedRawInstance != null)
        {
            Destroy(stagedRawInstance);
            stagedRawInstance = null;
            stagedRawName = null;
            stagedRawSourcePrefab = null;
        }

        // spawn result (no parenting)
        Transform point = resultPoint != null ? resultPoint : displayPoint;
        if (lastChoppedPrefabRef != null && point != null)
        {
            choppedSpawnedObject = Instantiate(lastChoppedPrefabRef, point.position, point.rotation);
            hasChoppedItem = true;

            if (choppedSpawnedObject.TryGetComponent<Collider>(out var c)) c.enabled = false;
            if (choppedSpawnedObject.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;
        }

        isChopping = false;
        if (playerAnimator != null) playerAnimator.SetBool("IsChopping", false);
        chopProgress = 0f;
        currentIngredientName = null;
    }

    // ---------- Place / Pickup ----------
    void PlaceFromHand(ChopMapping map)
    {
        if (displayPoint == null) return;

        GameObject rawPrefab = map.inputPrefab != null ? map.inputPrefab : currentPlayer.heldVisual;
        if (rawPrefab == null) return;

        stagedRawInstance = Instantiate(rawPrefab, displayPoint.position, displayPoint.rotation);

        if (stagedRawInstance.TryGetComponent<Collider>(out var c1)) c1.enabled = false;
        if (stagedRawInstance.TryGetComponent<Rigidbody>(out var r1)) r1.isKinematic = true;

        stagedRawName = map.inputName;
        stagedRawSourcePrefab = map.inputPrefab != null ? map.inputPrefab : rawPrefab;
        currentIngredientName = stagedRawName;

        if (currentPlayer.heldVisual != null) Destroy(currentPlayer.heldVisual);
        currentPlayer.ClearHeldItemDirect();
    }

    void PickupStagedRaw()
    {
        if (stagedRawSourcePrefab == null) return;

        currentPlayer.PickUpIngredient(stagedRawName, stagedRawSourcePrefab);

        Destroy(stagedRawInstance);
        stagedRawInstance = null;
        stagedRawName = null;
        stagedRawSourcePrefab = null;

        chopProgress = 0f;
    }

    void PickupChoppedResult()
    {
        if (lastChoppedPrefabRef != null)
        {
            string itemName = lastChoppedPrefabRef.name.Replace("(Clone)", "");
            currentPlayer.PickUpIngredient(itemName, lastChoppedPrefabRef);
            Destroy(choppedSpawnedObject);
        }
        else
        {
            // fallback: give the actual object
            var obj = choppedSpawnedObject;
            obj.transform.SetParent(currentPlayer.holdPoint, true);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            if (obj.TryGetComponent<Collider>(out var c)) c.enabled = false;
            if (obj.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

            currentPlayer.heldVisual = obj;
            currentPlayer.heldIngredient = obj.name.Replace("(Clone)", "");
            currentPlayer.heldDish = "";
        }

        choppedSpawnedObject = null;
        hasChoppedItem = false;
        chopProgress = 0f;
    }

    // ---------- Helpers ----------
    ChopMapping GetMapping(string ingredientName)
    {
        if (string.IsNullOrWhiteSpace(ingredientName)) return null;
        string key = ingredientName.Trim();
        foreach (var m in chopMappings)
            if (!string.IsNullOrEmpty(m.inputName) &&
                string.Equals(m.inputName.Trim(), key, System.StringComparison.OrdinalIgnoreCase))
                return m;
        return null;
    }

    string ResolveHeldName(PlayerInventory inv)
    {
        if (!string.IsNullOrWhiteSpace(inv.heldIngredient)) return inv.heldIngredient.Trim();
        if (inv.heldVisual != null) return inv.heldVisual.name.Replace("(Clone)", "").Trim();
        return "";
    }

    static void SnapToPoint(GameObject obj, Transform point)
    {
        if (obj == null || point == null) return;
        obj.transform.SetPositionAndRotation(point.position, point.rotation);
    }
}
