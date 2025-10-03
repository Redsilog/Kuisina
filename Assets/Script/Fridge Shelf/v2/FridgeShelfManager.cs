using UnityEngine;
using System.Collections.Generic;

public class FridgeShelfManager : MonoBehaviour
{
    public enum StorageType { Fridge, Shelf }
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
        {
            reopenCooldown -= Time.deltaTime;
            return;
        }

        if (!playerInRange || activePlayerInventory == null) return;

        FridgeUI targetUI = GetTargetUIForActivePlayer();
        if (targetUI == null) return;

        // Player 1 uses Space, Player 2 uses Enter/Return
        if (activePlayerInventory.playerID == 1 && Input.GetKeyDown(KeyCode.Space))
        {
            HandleFridgeToggle(targetUI);
        }
        else if (activePlayerInventory.playerID == 2 && Input.GetKeyDown(KeyCode.Return))
        {
            HandleFridgeToggle(targetUI);
        }
    }

    FridgeUI GetTargetUIForActivePlayer()
    {
        if (activePlayerInventory == null) return null;
        if (activePlayerInventory.playerID == 1) return fridgeUI_Player1;
        if (activePlayerInventory.playerID == 2) return fridgeUI_Player2;
        return null;
    }

    public Ingredients TakeIngredient(int index)
    {
        if (index >= 0 && index < storedIngredients.Count)
        {
            Ingredients item = storedIngredients[index];
            storedIngredients.RemoveAt(index); // remove it from fridge
            return item;
        }
        return null;
    }

    private void HandleFridgeToggle(FridgeUI targetUI)
    {
        if (targetUI.fridgePanel.activeSelf) // already open
        {
            targetUI.CloseFridge();
            reopenCooldown = 0.25f;
        }

        else if (storedIngredients.Count > 0)
        {
            playerPermissions perms = activePlayerInventory.GetComponent<playerPermissions>();

            bool openOnLeft = activePlayerInventory.playerID == 1;

            if (storageType == StorageType.Fridge && perms.canUseFridge)
            {
                targetUI.OpenFridge(this, activePlayerInventory, openOnLeft);
            }
            else if (storageType == StorageType.Shelf && perms.canUseShelf)
            {
                targetUI.OpenFridge(this, activePlayerInventory, openOnLeft);
            }
            else
            {
                Debug.Log(activePlayerInventory.name + " cannot open this " + storageType + "!");
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
            Debug.Log("Player near fridge/shelf");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            PlayerInventory leavingInv = other.GetComponent<PlayerInventory>();
            if (leavingInv == activePlayerInventory)
            {
                playerInRange = false;
                activePlayerInventory = null;
            }

            Debug.Log("Player left fridge/shelf");

            FridgeUI targetUI = null;
            if (leavingInv != null)
            {
                targetUI = (leavingInv.playerID == 1) ? fridgeUI_Player1 : fridgeUI_Player2;
            }

            if (targetUI != null && targetUI.fridgePanel != null && targetUI.fridgePanel.activeSelf)
            {
                targetUI.CloseFridge();
            }
        }
    }
}
