using UnityEngine;
using System.Collections.Generic;

public class FridgeShelfManager : MonoBehaviour
{
    public List<Ingredients> storedIngredients = new List<Ingredients>();
    private bool playerInRange = false;
    public FridgeUI fridgeUI;

    private PlayerInventory activePlayerInventory;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (fridgeUI != null)
            {
                if (fridgeUI.fridgePanel.activeSelf) // already open
                {
                    fridgeUI.CloseFridge();
                }
                else if (storedIngredients.Count > 0 && activePlayerInventory != null)
                {
                    fridgeUI.OpenFridge(this, activePlayerInventory);
                }
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