using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Stove : MonoBehaviour
{
    public Transform cookedFoodPoint;

    public GameObject cookedHotsilog;
    public GameObject cookedTocilog;
    public GameObject cookedTapsilog;
    public GameObject cookedAdobongManok;

    private List<string> addedIngredients = new List<string>();

    private Dictionary<string, List<string>> recipeBook;

    private Dictionary<string, GameObject> cookedPrefabs;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private GameObject currentCookedFood;

    void Start()
    {
        recipeBook = new Dictionary<string, List<string>>()
        {
            { "Hotsilog", new List<string> { "Cooked Hotdog", "Cooked Sinangag", "Cooked Itlog" } },
            { "Tocilog", new List<string> { "Cooked Tocino", "Cooked Sinangag", "Cooked Itlog" } },
            { "Tapsilog", new List<string> { "Cooked Tapa", "Cooked Sinangag", "Cooked Itlog" } },
            { "Adobong Manok", new List<string> { "Cooked Chicken", "Vinegar", "Soy Sauce", "Chopped Garlic" } }

        };

        cookedPrefabs = new Dictionary<string, GameObject>()
        {
            { "Hotsilog", cookedHotsilog },
            { "Tocilog", cookedTocilog },
            { "Tapsilog", cookedTapsilog },
            { "Adobong Manok", cookedAdobongManok }
        };
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Q))
        {
            if (playerInventory != null && playerInventory.HasIngredient())
            {
                AddIngredient(playerInventory);
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            resetStove();
        }

        if (Input.GetKeyDown(KeyCode.P) && currentCookedFood != null)
        {
            Destroy(currentCookedFood);
            currentCookedFood = null;
            Debug.Log("Served na boss");
        }
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

    public void AddIngredient(PlayerInventory player)
    {
        addedIngredients.Add(player.heldIngredient);
        player.PlaceIngredient();
        CheckRecipes();
    }

    private void CheckRecipes()
    {
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
                return;
            }
        }

        if (addedIngredients.Count >= 3)
        {
            Debug.Log("Walang ganyan boss");
        }
    }

    public void resetStove()
    {
        addedIngredients.Clear();
        Debug.Log("Nagaksaya ng pagkain ba");
    }
}
