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
    public float chopTime = 2f;

    private bool isChopping = false;
    private GameObject currentIngredientObject;
    private string currentIngredientName;
    private PlayerInventory currentPlayer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory player))
        {
            currentPlayer = player;
            Debug.Log("Player ready to chop");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory player) && player == currentPlayer)
        {
            currentPlayer = null;
            Debug.Log("Player left chopping board");
        }
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || isChopping || currentPlayer == null) return;

        // Player is holding something?
        if (currentPlayer.HasIngredient())
        {
            StartChopping(currentPlayer);
        }
        else
        {
            Debug.Log("Player has nothing to chop");
        }
    }

    private void StartChopping(PlayerInventory player)
    {
        currentIngredientName = player.heldIngredient;
        currentIngredientObject = player.heldVisual;
        player.ClearHeldItemDirect(); // custom helper we’ll add below

        isChopping = true;
        Debug.Log("Started chopping " + currentIngredientName);

        StartCoroutine(ChopRoutine());
    }

    private IEnumerator ChopRoutine()
    {
        yield return new WaitForSeconds(chopTime);

        GameObject choppedPrefab = GetChoppedPrefab(currentIngredientName);
        if (choppedPrefab != null)
        {
            Instantiate(choppedPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Debug.Log("Chopped " + currentIngredientName + " into " + choppedPrefab.name);
        }
        else
        {
            Debug.LogWarning("No chopped prefab found for: " + currentIngredientName);
        }

        isChopping = false;
        currentIngredientObject = null;
        currentIngredientName = "";
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

