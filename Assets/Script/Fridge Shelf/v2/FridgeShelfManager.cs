using UnityEngine;
using System.Collections.Generic;

public class FridgeShelfManager : MonoBehaviour
{
    public enum StorageType { Fridge, Shelf }
    public StorageType storageType;

    public List<Ingredients> storedIngredients = new List<Ingredients>();
    private bool playerInRange = false;
    public FridgeUI fridgeUI;

    private PlayerInventory activePlayerInventory;

    private float reopenCooldown = 0f;

    void Update()
    {
        if (fridgeUI == null || activePlayerInventory == null) return;

        if (reopenCooldown > 0f)
        {
            reopenCooldown -= Time.deltaTime;
            return;
        }

        if (playerInRange && activePlayerInventory != null)
        {
            if (activePlayerInventory.playerID == 1 && Input.GetKeyDown(KeyCode.Space))
            {
                HandleFridgeToggle();
            }
            else if (activePlayerInventory.playerID == 2 && Input.GetKeyDown(KeyCode.Return))
            {
                HandleFridgeToggle();
            }
        }
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

    private void HandleFridgeToggle()
    {
        if (fridgeUI.fridgePanel.activeSelf) // already open
        {
            fridgeUI.CloseFridge();
            reopenCooldown = 0.25f;
        }
        else if (storedIngredients.Count > 0)
        {
            playerPermissions perms = activePlayerInventory.GetComponent<playerPermissions>();
            if (storageType == StorageType.Fridge && perms.canUseFridge)
            {
                fridgeUI.OpenFridge(this, activePlayerInventory, true);
            }
            else if (storageType == StorageType.Shelf && perms.canUseShelf)
            {
                fridgeUI.OpenFridge(this, activePlayerInventory, false);
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
            playerInRange = false;
            Debug.Log("Player left fridge/shelf");

            if (fridgeUI != null)
            {
                fridgeUI.CloseFridge();
            }
        }
    }
}
