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

    public bool IsChoppingActive() => isChopping;

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

        if (stagedRawInstance == null && !hasChoppedItem && player.IsHoldingItem())
        {
            string name = ResolveHeldName(player);
            var map = GetMapping(name);
            if (map != null)
            {
                PlaceFromHand(map, player);
                return;
            }
        }

        if (hasChoppedItem && !player.IsHoldingItem() && choppedSpawnedObject != null)
        {
            PickupChoppedResult(player);
            return;
        }

        if (stagedRawInstance != null && !player.IsHoldingItem() && !isChopping)
        {
            PickupStagedRaw(player);
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

    public void BeginChop(PlayerInventory player)
    {
        if (stagedRawInstance == null) return;

        isChopping = true;
        Animator anim = player.animator;

        if (anim != null)
            anim.SetBool("IsChopping", true);

        StartCoroutine(ChopWhileHeld(player));
    }

    private IEnumerator ChopWhileHeld(PlayerInventory player)
    {
        chopProgress = 0f;
        while (isChopping && chopProgress < chopTime)
        {
            chopProgress += Time.deltaTime;

            Vector3 lookPos = new Vector3(displayPoint.position.x, player.transform.position.y, displayPoint.position.z);
            player.transform.rotation = Quaternion.LookRotation(lookPos - player.transform.position);

            yield return null;
        }

        if (isChopping)
            FinishChop(player);
    }

    public void StartChop(PlayerInventory player)
    {
        if (stagedRawInstance == null) return; 
        if (isChopping) return; 

        isChopping = true;
        playerAnimator = player.animator;
        if (playerAnimator != null) playerAnimator.SetBool("IsChopping", true);

        if (displayPoint != null)
            player.transform.LookAt(new Vector3(displayPoint.position.x, player.transform.position.y, displayPoint.position.z));

        StartCoroutine(ChopWhileHeld(player));
    }


    // Called when player releases interact
    public void PauseChop()
    {
        if (!isChopping) return;
        isChopping = false;
        if (playerAnimator != null) playerAnimator.SetBool("IsChopping", false);
    }

    private void FinishChop(PlayerInventory player)
    {
        var map = GetMapping(stagedRawName);
        lastChoppedPrefabRef = map != null ? map.outputPrefab : null;

        if (stagedRawInstance != null)
            Destroy(stagedRawInstance);

        stagedRawInstance = null;
        stagedRawName = null;
        stagedRawSourcePrefab = null;

        Transform spawnPoint = resultPoint != null ? resultPoint : displayPoint;
        if (lastChoppedPrefabRef != null && spawnPoint != null)
        {
            choppedSpawnedObject = Instantiate(lastChoppedPrefabRef, spawnPoint.position, spawnPoint.rotation);
            hasChoppedItem = true;

            if (choppedSpawnedObject.TryGetComponent<Collider>(out var col))
                col.enabled = false;
            if (choppedSpawnedObject.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = true;
        }

        isChopping = false;
        chopProgress = 0f;

        if (player != null && player.animator != null)
            player.animator.SetBool("IsChopping", false);
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
