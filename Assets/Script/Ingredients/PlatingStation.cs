using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PlatingStation : MonoBehaviour
{

    public Transform cookedFoodPoint;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;

    [SerializeField]
    private GameObject placedDish;
    //Adobo
    public GameObject cookedClassicAdobo;
    public GameObject cookedAdobongPuti;

    [SerializeField]
    private List<string> addedIngredients = new List<string>();

    [SerializeField]
    private float ingredientTimer = 0f;
    [SerializeField]
    private float maxWaitTime = 10f; // seconds to wait before checking

    private Dictionary<string, List<string>> recipeBook;

    private Dictionary<string, GameObject> cookedPrefabs;

    private GameObject currentCookedFood;
    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            if (playerInventory != null)
            {
                // Player is holding a dish → Place it
                if (playerInventory.HasDish() && currentCookedFood == null)
                {
                    currentCookedFood = playerInventory.PlaceDish(cookedFoodPoint.position);
                    Debug.Log("Placed dish on station: " + currentCookedFood?.name);
                }
                // Player is not holding a dish → Pick it up
                else if (!playerInventory.HasDish() && currentCookedFood != null)
                {
                    playerInventory.PickUpDish("Dish", currentCookedFood);
                    Debug.Log("Picked up dish from station");
                    currentCookedFood = null;
                }
            }
        }
    }

     public void AddIngredient(PlayerInventory player)
    {
        string ingredient = player.heldIngredient;
        Debug.Log("Adding ingredient: " + ingredient); 
        addedIngredients.Add(player.heldIngredient);
        player.PlaceIngredient();

        // switch (ingredient)
        // {
        //     case "Cooked Garlic":
        //     case "Garlic":
        //         garlic.SetActive(true);
        //         break;

        //     case "Cooked Chicken":
        //     case "Chicken":
        //         classicChicken.SetActive(true);
        //         break;

        //     case "Soy Sauce":
        //         liquid1.SetActive(true);
        //         break;

        //     case "Vinegar":
        //         liquid2.SetActive(true);
        //         break;
        // }
    }

    private void CheckRecipes()
    {
        bool foundRecipe = false;

        foreach (var recipe in recipeBook)
        {
            var expected = recipe.Value.OrderBy(i => i).ToList();
            var actual = addedIngredients.OrderBy(i => i).ToList();

            if (expected.SequenceEqual(actual))
            {
                Debug.Log("Serving " + recipe.Key);

                if (cookedPrefabs.TryGetValue(recipe.Key, out GameObject foodPrefab))
                {
                    currentCookedFood = Instantiate(foodPrefab, cookedFoodPoint.position, cookedFoodPoint.rotation);
                }

                addedIngredients.Clear();
                foundRecipe = true;

                return;
            }
        }

        if (!foundRecipe)
        {
            Debug.Log("Walang ganyan boss");
            addedIngredients.Clear();
            Debug.Log("Stove cleared.");
        }
    }

    private bool CheckForValidRecipe()
    {
        foreach (var recipe in recipeBook)
        {
            if (recipe.Value.Count != addedIngredients.Count)
                continue;

            if (!recipe.Value.Except(addedIngredients).Any() && !addedIngredients.Except(recipe.Value).Any())
            {
                return true;
            }
        }

        return false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerInventory = null;
        }
    }

}

