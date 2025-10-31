using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class FridgeShelfManager : MonoBehaviour
{
    public enum StorageType { Fridge, Freezer, Pantry, Condiments }
    public StorageType storageType;

    public List<Ingredients> storedIngredients = new List<Ingredients>();

    public FridgeUI fridgeUI_Player1;
    public FridgeUI fridgeUI_Player2;


    private PlayerInventory activePlayerInventory;
    private float reopenCooldown = 0f;

    private OutlineHighlighter highlighter;
    void Awake()
    {
        Transform current = transform;
        while (current.parent != null)
        {
            current = current.parent;
            highlighter = current.GetComponent<OutlineHighlighter>();
            if (highlighter != null)
                break;
        }

        if (highlighter == null)
            Debug.LogWarning($"{name} could not find OutlineHighlighter in any parent.");
    }
    void Update()
    {
        if (reopenCooldown > 0f)
            reopenCooldown -= Time.deltaTime;
    }

    FridgeUI GetTargetUIForActivePlayer()
    {
        if (activePlayerInventory == null) return null;

        if (storageType == StorageType.Fridge)
            return activePlayerInventory.playerID == 1 ? fridgeUI_Player1 : fridgeUI_Player2;
        else
            return activePlayerInventory.playerID == 1 ? fridgeUI_Player1 : fridgeUI_Player2;
    }

    public Ingredients GetIngredient(int index)
    {
        if (index >= 0 && index < storedIngredients.Count)
        {
            return storedIngredients[index];
        }
        return null;
    }

    public void TryOpenOrCloseFridge(PlayerInventory playerInventory)
    {
        if (reopenCooldown > 0f)
        {
            Debug.Log($"Fridge on cooldown for {reopenCooldown:F2}s");
            return;
        }

        activePlayerInventory = playerInventory;
        FridgeUI targetUI = GetTargetUIForActivePlayer();

        HandleFridgeToggle(targetUI);
}

    private void HandleFridgeToggle(FridgeUI targetUI)
    {
        if (targetUI.fridgePanel.activeSelf) // already open
        {
            targetUI.CloseFridge(storageType);
            SetCooldown(0f);
        }
        else if (storedIngredients.Count > 0)
        {
            playerPermissions perms = activePlayerInventory.GetComponent<playerPermissions>();

            bool openOnLeft = activePlayerInventory.playerID == 1;
            if (activePlayerInventory.IsHoldingItem()) return;

            if (perms != null && perms.CanUse(storageType))
            {
                targetUI.OpenFridge(this, activePlayerInventory, openOnLeft, storageType);
            }
            else
            {
                Debug.Log($"{activePlayerInventory.name} cannot open this {storageType}!");
            }
        }
    }
    
    public bool IsFridgeUIOpenFor(PlayerInventory playerInventory)
    {
        if (playerInventory == null) return false;
        FridgeUI targetUI = playerInventory.playerID == 1 ? fridgeUI_Player1 : fridgeUI_Player2;
        return targetUI != null && targetUI.isOpen;
    }

    public void SetCooldown(float time)
    {
        reopenCooldown = time;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Player2"))
            return;

        var inv = other.GetComponent<PlayerInventory>();
        if (inv == null) return;

        if (!inv.nearbyFridges.Contains(this))
            inv.nearbyFridges.Add(this);

        if (highlighter != null)
            highlighter.SetHighlight(true, other.tag);

        Debug.Log($"{other.name} entered fridge: {gameObject.name}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Player2"))
            return;

        var inv = other.GetComponent<PlayerInventory>();
        if (inv == null) return;

        inv.nearbyFridges.Remove(this);

        if (highlighter != null)
            highlighter.SetHighlight(false, other.tag);

        // Close only if this was the active one
        if (inv.currentFridge == null)
        {
            FridgeUI fridgeUI = inv.playerID == 1 ? fridgeUI_Player1 : fridgeUI_Player2;
            if (fridgeUI != null && fridgeUI.fridgePanel.activeSelf)
                fridgeUI.CloseFridge(storageType);
        }

        Debug.Log($"{other.name} left fridge: {gameObject.name}");
    }

    public bool IsOpenFor(PlayerInventory player)
    {
        return IsFridgeUIOpenFor(player);
    }

}
