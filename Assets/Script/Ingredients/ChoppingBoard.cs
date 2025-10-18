using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChopMapping
{
    public string inputName;
    public GameObject inputPrefab;
    public GameObject outputPrefab;
}

public class ChoppingBoard : MonoBehaviour
{
    [Header("Settings")]
    public float chopTime = 2f;
    public Transform displayPoint;
    public Transform resultPoint;
    public List<ChopMapping> chopMappings = new List<ChopMapping>();

    private GameObject stagedRawInstance;
    private string stagedRawName;
    private GameObject stagedRawSourcePrefab;

    private GameObject choppedSpawnedObject;
    private GameObject lastChoppedPrefabRef;
    private bool hasChoppedItem;

    private bool isChopping;
    private float chopProgress;

    private Animator playerAnimator;
    private PlayerInventory currentPlayer;

    public float ChopProgress01 => chopTime <= 0f ? 0f : Mathf.Clamp01(chopProgress / chopTime);

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory p))
        {
            currentPlayer = p;
            playerAnimator = p.animator;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory p) && p == currentPlayer)
        {
            if (isChopping) PauseChop();
            currentPlayer = null;
            playerAnimator = null;
        }
    }

    public void HandlePlayerInteract(PlayerInventory player)
    {
        if (player == null) return;

        // 1) Place raw ingredient if hands are holding one
        if (stagedRawInstance == null && !hasChoppedItem && player.IsHoldingItem())
        {
            string name = ResolveHeldName(player);
            var map = GetMapping(name);
            if (map != null)
            {
                PlaceFromHand(map, player);
                
                BeginChop(player);
                return;
            }
        }

        // 2) Pick up chopped result if board has one
        if (hasChoppedItem && !player.IsHoldingItem() && choppedSpawnedObject != null)
        {
            PickupChoppedResult(player);
            return;
        }

        // 3) Pick up staged raw if hands empty
        if (stagedRawInstance != null && !player.IsHoldingItem())
        {
            PickupStagedRaw(player);
            return;
        }

        // 4) Begin chopping manually if raw is staged and not yet chopping
        if (stagedRawInstance != null && !isChopping)
        {
            BeginChop(player);
            return;
        }
    }

    private void PlaceFromHand(ChopMapping map, PlayerInventory player)
    {
        if (displayPoint == null) return;

        stagedRawInstance = Instantiate(map.inputPrefab, displayPoint.position, displayPoint.rotation);

        if (stagedRawInstance.TryGetComponent<Collider>(out var c)) c.enabled = false;
        if (stagedRawInstance.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

        stagedRawName = map.inputName;
        stagedRawSourcePrefab = map.inputPrefab;

        player.ClearHeldItemDirect();
        chopProgress = 0f;
        isChopping = false;
        hasChoppedItem = false;
    }

    private void PickupStagedRaw(PlayerInventory player)
    {
        player.PickUpIngredient(stagedRawName, stagedRawSourcePrefab);
        Destroy(stagedRawInstance);

        stagedRawInstance = null;
        stagedRawName = null;
        stagedRawSourcePrefab = null;
        chopProgress = 0f;
    }

    private void PickupChoppedResult(PlayerInventory player)
    {
        string itemName = lastChoppedPrefabRef.name.Replace("(Clone)", "");
        player.PickUpIngredient(itemName, lastChoppedPrefabRef);
        Destroy(choppedSpawnedObject);

        choppedSpawnedObject = null;
        hasChoppedItem = false;
        chopProgress = 0f;
    }

    private void BeginChop(PlayerInventory player)
    {
        if (stagedRawInstance == null) return;

        isChopping = true;
        playerAnimator = player.animator;

        if (playerAnimator != null)
            playerAnimator.SetBool("IsChopping", true);

        StartCoroutine(ChopWhileHeld());
    }



    private IEnumerator ChopWhileHeld()
    {
        while (isChopping && chopProgress < chopTime)
        {
            chopProgress += Time.deltaTime;
            yield return null;
        }

        if (!isChopping) yield break;
        if (chopProgress >= chopTime) FinishChop();
    }

    // Called when player presses and holds interact
    public void StartChop(PlayerInventory player)
    {
        if (stagedRawInstance == null) return; // nothing to chop
        if (isChopping) return; // already chopping

        isChopping = true;
        playerAnimator = player.animator;
        if (playerAnimator != null) playerAnimator.SetBool("IsChopping", true);

        StartCoroutine(ChopWhileHeld());
    }

    // Called when player releases interact
    public void PauseChop()
    {
        if (!isChopping) return;
        isChopping = false;
        if (playerAnimator != null) playerAnimator.SetBool("IsChopping", false);
    }

    private void FinishChop()
    {
        var map = GetMapping(stagedRawName);
        lastChoppedPrefabRef = map != null ? map.outputPrefab : null;

        if (stagedRawInstance != null) Destroy(stagedRawInstance);
        stagedRawInstance = null;
        stagedRawName = null;
        stagedRawSourcePrefab = null;

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
    }

    private ChopMapping GetMapping(string ingredientName)
    {
        if (string.IsNullOrWhiteSpace(ingredientName)) return null;
        string key = ingredientName.Trim();

        foreach (var m in chopMappings)
        {
            if (!string.IsNullOrEmpty(m.inputName) &&
                string.Equals(m.inputName.Trim(), key, System.StringComparison.OrdinalIgnoreCase))
                return m;
        }

        return null;
    }

    private string ResolveHeldName(PlayerInventory inv)
    {
        if (!string.IsNullOrWhiteSpace(inv.heldIngredient)) return inv.heldIngredient.Trim();
        if (inv.heldVisual != null) return inv.heldVisual.name.Replace("(Clone)", "").Trim();
        return "";
    }
}
