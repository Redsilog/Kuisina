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

    private bool playerInRange = false;
    private PlayerInventory activePlayerInventory;
    private float reopenCooldown = 0f;

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
        activePlayerInventory = playerInventory; 
        FridgeUI targetUI = GetTargetUIForActivePlayer();

        HandleFridgeToggle(targetUI);
    }

    private void HandleFridgeToggle(FridgeUI targetUI)
    {
        if (targetUI.fridgePanel.activeSelf) // already open
        {
            targetUI.CloseFridge(storageType);
            reopenCooldown = 0.25f;
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

    public void SetCooldown(float time)
    {
        reopenCooldown = time;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            activePlayerInventory = other.GetComponent<PlayerInventory>();
            activePlayerInventory.currentFridge = this;
            Debug.Log($"{other.name} entered fridge: {gameObject.name}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            PlayerInventory leavingInv = other.GetComponent<PlayerInventory>();

            if (leavingInv != null && leavingInv.currentFridge == this)
            {
                leavingInv.currentFridge = null;

                FridgeUI fridgeUI = null;
                if (leavingInv.playerID == 1)
                    fridgeUI = fridgeUI_Player1;
                else if (leavingInv.playerID == 2)
                    fridgeUI = fridgeUI_Player2;

                // ✅ Only close if the panel was actually open
                if (fridgeUI != null && fridgeUI.fridgePanel.activeSelf)
                {
                    fridgeUI.CloseFridge(storageType);
                    Debug.Log($"Closed fridge UI for {other.name} leaving {gameObject.name}");
                }
            }

            playerInRange = false;
            activePlayerInventory = null;
            Debug.Log($"{other.name} left fridge: {gameObject.name}");
        }
    }
}
