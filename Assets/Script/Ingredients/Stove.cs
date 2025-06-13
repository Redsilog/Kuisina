using UnityEngine;
using System.Collections.Generic;

public class Stove : MonoBehaviour
{
    public List<string> addedIngredients = new List<string>();
    public GameObject cookedTapsilog;
    public Transform cookedFoodPoint;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            resetStove();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.Space))
        {
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null && inventory.HasIngredient())
            {
                AddIngredient(inventory);
            }
        }
    }

    public void AddIngredient(PlayerInventory player)
    {
        addedIngredients.Add(player.heldIngredient);
        player.PlaceIngredient();

        CheckRecipe();
    }

    private void CheckRecipe()
    {
        List<string> tapsilogRecipe = new List<string> { "Hotdog", "Sinangag", "Itlog" };

        if (tapsilogRecipe.Count == addedIngredients.Count)
        {
            bool allMatch = true;
            foreach (string item in tapsilogRecipe)
            {
                if (!addedIngredients.Contains(item))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                Debug.Log("Serving Tapsilog");
                Instantiate(cookedTapsilog, cookedFoodPoint.position, cookedFoodPoint.rotation);
                addedIngredients.Clear();
            }
        }
    }

    public void resetStove()
    {
        addedIngredients.Clear();
        Debug.Log("Food Discarded");
    }
}