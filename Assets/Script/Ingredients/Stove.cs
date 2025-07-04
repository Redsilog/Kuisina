using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Stove : MonoBehaviour, Interaction
{
    public Transform cookedFoodPoint;

    public GameObject cookedHotsilog;
    public GameObject cookedTocilog;
    public GameObject cookedTapsilog;

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
            { "Hotsilog", new List<string> { "Hotdog", "Sinangag", "Itlog" } },
            { "Tocilog", new List<string> { "Tocino", "Sinangag", "Itlog" } },
            { "Tapsilog", new List<string> { "Tapa", "Sinangag", "Itlog" } }
        };

        cookedPrefabs = new Dictionary<string, GameObject>()
        {
            { "Hotsilog", cookedHotsilog },
            { "Tocilog", cookedTocilog },
            { "Tapsilog", cookedTapsilog }
        };
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
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

        if (Input.GetKeyDown(KeyCode.P) && currentCookedFood != null && playerInventory != null && !playerInventory.HasDish())
        {
            string cookedName = currentCookedFood.name.Replace("(Clone)", "");
            playerInventory.PickUpDish(cookedName, currentCookedFood);
            currentCookedFood = null;
            Debug.Log("Kinuha ni boss ang pagkain");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            Debug.Log("Touching Stove");
        }
        //Remove if not needed just for testing 
        if (other.CompareTag("Waiter"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            Debug.Log("Touching Stove");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerInventory = null;
        }
        //Remove if not needed just for testing 
        if (other.CompareTag("Waiter"))
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

    public string GetDescription()
    {
        var player = Object.FindFirstObjectByType<PlayerInventory>();
        return (player != null && player.HasIngredient())
            ? $"Add {player.heldIngredient} to stove"
            : null;
    }

    public KeyCode GetKey() => KeyCode.E;

    public void Interact()
    {
        var player = Object.FindFirstObjectByType<PlayerInventory>();
        if (player != null && player.HasIngredient())
            AddIngredient(player);
    }
}
