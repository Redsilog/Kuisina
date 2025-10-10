using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlatingStation : MonoBehaviour
{
    public Transform cookedFoodPoint;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private KeyCode interactKey = KeyCode.Space;

    [SerializeField]
    private GameObject placedDish;

    // Adobo
    public GameObject cookedClassicAdobo;
    public GameObject cookedAdobongPuti;

    [SerializeField]
    private List<string> addedIngredients = new List<string>();

    [SerializeField]
    private float ingredientTimer = 0f;
    [SerializeField]
    private float maxWaitTime = 10f;

    private Dictionary<string, List<string>> recipeBook;
    private Dictionary<string, GameObject> cookedPrefabs;

    private GameObject currentCookedFood;

    [SerializeField] private AudioClip placeDishClip;

    void Start()
    {
        recipeBook = new Dictionary<string, List<string>>()
        {
            { "Classic Adobo", new List<string> { "Cooked Chicken", "Cooked Garlic", "Soy Sauce", "Vinegar" } },
            { "Adobong Puti", new List<string> { "Cooked Chicken", "Cooked Garlic", "Vinegar" } },
        };

        cookedPrefabs = new Dictionary<string, GameObject>()
        {
            { "Classic Adobo", cookedClassicAdobo },
            { "Adobong Puti", cookedAdobongPuti },
        };
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (playerInventory != null)
            {
                if (playerInventory.HasDish() && currentCookedFood == null)
                {
                    currentCookedFood = playerInventory.PlaceDish(cookedFoodPoint.position);
                    SoundFXManager.instance.PlaySoundFXClip(placeDishClip, transform, 1f);
                    Debug.Log("Placed dish on station: " + currentCookedFood?.name);
                }
                else if (!playerInventory.HasDish() && currentCookedFood != null)
                {
                    playerInventory.PickUpDish("Dish", currentCookedFood);
                    Debug.Log("Picked up dish from station");
                    currentCookedFood = null;
                }
            }
        }

        if (addedIngredients.Count > 0)
        {
            ingredientTimer += Time.deltaTime;

            if (ingredientTimer >= maxWaitTime)
            {
                CheckRecipes();
                ingredientTimer = 0f;
            }
        }
    }

    public void AddIngredient(PlayerInventory player)
    {
        string ingredient = player.heldIngredient;
        Debug.Log("Adding ingredient: " + ingredient);
        addedIngredients.Add(ingredient);
        player.PlaceIngredient();
        ingredientTimer = 0f;
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
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();

            interactKey = (other.CompareTag("Player")) ? KeyCode.Space : KeyCode.Return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = false;
            playerInventory = null;
        }
    }
}
