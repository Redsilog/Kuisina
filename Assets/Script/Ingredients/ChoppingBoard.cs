using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class ChopMapping
{
    public string inputName;        // e.g. "Onion"
    public GameObject outputPrefab; // e.g. ChoppedOnion prefab
}

public class ChoppingBoard : MonoBehaviour
{
    [Header("Chopping Settings")]
    public List<ChopMapping> chopMappings = new List<ChopMapping>();
    public float chopTime = 2f; // time player must hold the key to finish chopping

    private bool isChopping = false;
    private bool hasChoppedItem = false;
    private bool isHoldingKey = false;

    private float chopProgress = 0f;

    private string currentIngredientName;
    private PlayerInventory currentPlayer;
    private Animator playerAnimator;
    private GameObject choppedSpawnedObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory player))
        {
            currentPlayer = player;
            playerAnimator = player.animator;
            Debug.Log("Player ready to chop");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory player) && player == currentPlayer)
        {
            currentPlayer = null;
            playerAnimator = null;
            Debug.Log("Player left chopping board");
        }
    }

    // Called when player presses or releases the interact key
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (currentPlayer == null) return;

        // if there’s a chopped item waiting, pick it up with a quick tap
        if (ctx.performed && !isChopping && hasChoppedItem && choppedSpawnedObject != null)
        {
            PickUpChoppedItem();
            return;
        }

        // Player pressed interact (start holding)
        if (ctx.started)
        {
            // Start chopping if valid ingredient
            if (!isChopping && currentPlayer.HasIngredient())
            {
                string ingredientName = currentPlayer.heldIngredient;
                if (CanBeChopped(ingredientName))
                {
                    StartChopping(currentPlayer);
                    isHoldingKey = true;
                }
                else
                {
                    Debug.LogWarning($"{ingredientName} cannot be chopped (not in chop list).");
                }
            }
        }

        // Player released interact (stop holding)
        if (ctx.canceled && isChopping)
        {
            isHoldingKey = false;
            StopChoppingEarly();
        }
    }

    private bool CanBeChopped(string ingredientName)
    {
        foreach (var mapping in chopMappings)
        {
            if (mapping.inputName == ingredientName)
                return true;
        }
        return false;
    }

    private void StartChopping(PlayerInventory player)
    {
        currentIngredientName = player.heldIngredient;

        if (player.heldVisual != null)
            Destroy(player.heldVisual);

        player.heldVisual = null;
        player.heldIngredient = "";

        isChopping = true;
        chopProgress = 0f;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsChopping", true);
            playerAnimator.SetBool("IsHoldingWalk", false);
            playerAnimator.SetBool("IsHoldingStill", false);
        }

        Debug.Log("Started chopping " + currentIngredientName);
        StartCoroutine(ChopWhileHolding());
    }

    private IEnumerator ChopWhileHolding()
    {
        while (isHoldingKey && chopProgress < chopTime)
        {
            chopProgress += Time.deltaTime;
            yield return null;
        }

        if (chopProgress >= chopTime)
        {
            FinishChop();
        }
        else
        {
            StopChoppingEarly();
        }
    }

    private void StopChoppingEarly()
    {
        if (!isChopping) return;

        Debug.Log("Chopping cancelled early");

        if (playerAnimator != null)
            playerAnimator.SetBool("IsChopping", false);

        isChopping = false;
        chopProgress = 0f;
        currentIngredientName = "";
    }

    private void FinishChop()
    {
        GameObject choppedPrefab = GetChoppedPrefab(currentIngredientName);

        if (choppedPrefab != null)
        {
            choppedSpawnedObject = Instantiate(
                choppedPrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity
            );

            hasChoppedItem = true;
            Debug.Log($"Finished chopping {currentIngredientName} → {choppedPrefab.name}");
        }
        else
        {
            Debug.LogWarning("No chopped prefab found for: " + currentIngredientName);
        }

        if (playerAnimator != null)
            playerAnimator.SetBool("IsChopping", false);

        isChopping = false;
        chopProgress = 0f;
        currentIngredientName = "";
        isHoldingKey = false;
    }

    private void PickUpChoppedItem()
    {
        if (currentPlayer == null || choppedSpawnedObject == null) return;

        Debug.Log("Player picked up chopped item: " + choppedSpawnedObject.name);

        currentPlayer.PickUpIngredient(
            choppedSpawnedObject.name.Replace("(Clone)", ""),
            choppedSpawnedObject
        );

        choppedSpawnedObject = null;
        hasChoppedItem = false;
    }

    private GameObject GetChoppedPrefab(string ingredientName)
    {
        foreach (var mapping in chopMappings)
        {
            if (mapping.inputName == ingredientName)
                return mapping.outputPrefab;
        }
        return null;
    }
}
