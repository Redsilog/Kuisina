using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class Stove : MonoBehaviour
{
    public Transform cookedFoodPoint;

    public GameObject cookedHotsilog;
    public GameObject cookedTocilog;
    public GameObject cookedTapsilog;

    public GameObject cookedClassicAdobo;
    public GameObject cookedAdobongPuti;

    public GameObject cookedPancitMalabon;
    public GameObject cookedPancitBatil;

    public GameObject cookedSinigangBaboy;
    public GameObject cookedSinigangCansi;

    public GameObject cookedKapampanganSisig;
    public GameObject cookedDinakdakanSisig;

    public GameObject failedDish;

    public Slider stoveSlider;

    [SerializeField] private List<string> addedIngredients = new List<string>();
    [SerializeField] private float ingredientTimer = 0f;
    [SerializeField] private float maxWaitTime = 10f;

    private Dictionary<string, List<string>> recipeBook;
    private Dictionary<string, GameObject> cookedPrefabs;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private GameObject currentCookedFood;
    private KeyCode interactKey = KeyCode.Space;

    [Header("Cooked State Visuals")]
    public GameObject liquid1;
    public GameObject liquid2;
    public GameObject garlic;
    public GameObject classicChicken;
    public GameObject putiChicken;

    private bool isTimerRunning = false;
    private bool justCooked = false;

    void Start()
    {
        recipeBook = new Dictionary<string, List<string>>()
        {
            { "Hotsilog", new List<string> { "Cooked Hotdog", "Cooked Sinangag", "Cooked Itlog" } },
            { "Tocilog", new List<string> { "Cooked Tocino", "Cooked Sinangag", "Cooked Itlog" } },
            { "Tapsilog", new List<string> { "Cooked Tapa", "Cooked Sinangag", "Cooked Itlog" } },
            { "Chicken", new List<string> { "Cooked Chicken", "Tuyo", "Vinegar", "Cooked Garlic" } },

            { "Classic Adobo", new List<string> { "Chicken", "Soy Sauce", "Vinegar", "Chopped Onion"}},
            { "Adobong Puti", new List<string> { "Chicken", "Vinegar", "Garlic", "Salt"}},

            { "Pancit Malabon", new List<string> { "Noodles", "Chopped Pork", "Mussels", "Dried Fish"}},
            { "Pancit Batil Patung", new List<string> { "Chopped Beef", "Chopped Pork", "Noodles", "Chicharon"}},

            { "Sinigang na Baboy", new List<string> { "Pork", "Onion", "Tomato", "Radish", "Eggplant", "Green Chili", "String Beans", "Okra", "Kangkong"}},
            { "Cansi", new List<string> { "Beef", "Garlic", "Onion", "Tomato", "Lemongrass", "Jackfruit"}},

            { "Kapampangan Sisig", new List<string> { "Chopped Pork", "Chopped Chicken"}},
            { "Dinakdakan", new List<string> { "Chopped Pork", "Vinegar"}}
        };

        cookedPrefabs = new Dictionary<string, GameObject>()
        {
            { "Hotsilog", cookedHotsilog },
            { "Tocilog", cookedTocilog },
            { "Tapsilog", cookedTapsilog },
            { "Classic Adobo", cookedClassicAdobo },
            { "Adobong Puti", cookedAdobongPuti },
            { "Pancit Malabon", cookedPancitMalabon },
            { "Pancit Batil Patung", cookedPancitBatil },
            // Add more as needed
        };

        ResetVisuals();

        if (stoveSlider!= null)
            stoveSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (playerInventory != null && playerInventory.HasIngredient())
            {
                AddIngredient(playerInventory);
            }
            else if (currentCookedFood != null && playerInventory != null && !playerInventory.HasDish())
            {
                if (justCooked)
                {
                    justCooked = false;
                }
                else
                {
                    playerInventory.PickUpDish("Dish", currentCookedFood);
                    currentCookedFood = null;
                    Debug.Log("Player picked up dish");
                    stoveSlider.gameObject.SetActive(false);
                }
            }
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.Tab))
        {
            resetStove();
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.P) && currentCookedFood != null)
        {
            Destroy(currentCookedFood);
            currentCookedFood = null;
            Debug.Log("Served na boss");
        }

        if (isTimerRunning)
        {
            ingredientTimer += Time.deltaTime;

            if (ingredientTimer >= maxWaitTime)
            {
                CheckRecipes();
                isTimerRunning = false;
                ingredientTimer = 0f;
            }
        }
        if (isTimerRunning)
        {
            ingredientTimer += Time.deltaTime;

            if (stoveSlider != null)
            {
                stoveSlider.gameObject.SetActive(true);
                stoveSlider.value = ingredientTimer / maxWaitTime;
            }

            if (ingredientTimer >= maxWaitTime)
            {
                CheckRecipes();
                isTimerRunning = false;
                ingredientTimer = 0f;

                if (stoveSlider != null)
                {
                    stoveSlider.value = 0f;
                    stoveSlider.gameObject.SetActive(false);
                }
            }
        }
    }

    public void AddIngredient(PlayerInventory player)
    {
        string ingredient = player.heldIngredient;
        Debug.Log("Adding ingredient: " + ingredient);
        addedIngredients.Add(ingredient);
        player.PlaceIngredient();

        switch (ingredient)
        {
            case "Cooked Garlic":
            case "Garlic":
                garlic.SetActive(true); break;
            case "Cooked Chicken":
            case "Chicken":
                classicChicken.SetActive(true); break;
            case "Soy Sauce":
                liquid1.SetActive(true); break;
            case "Vinegar":
                liquid2.SetActive(true); break;
        }

        if (CheckForValidRecipe())
        {
            Debug.Log("Correct recipe made immediately!");
            CheckRecipes();
            isTimerRunning = false;
        }
        else
        {
            ingredientTimer = 0f;
            isTimerRunning = true;
        }
    }

    private void CheckRecipes()
    {
        bool foundRecipe = false;

        var actual = addedIngredients.OrderBy(i => i).ToList();

        foreach (var recipe in recipeBook)
        {
            var expected = recipe.Value.OrderBy(i => i).ToList();

            if (expected.SequenceEqual(actual))
            {
                Debug.Log("Serving " + recipe.Key);

                if (cookedPrefabs.TryGetValue(recipe.Key, out GameObject foodPrefab))
                {
                    currentCookedFood = Instantiate(foodPrefab, cookedFoodPoint.position, cookedFoodPoint.rotation);
                }

                addedIngredients.Clear();
                foundRecipe = true;
                ResetVisuals();
                justCooked = true;
                return;
            }
        }

        if (!foundRecipe)
        {
            Debug.Log("Walang ganyan boss");
            addedIngredients.Clear();
            justCooked = false;
            ResetVisuals();
        }

        if (stoveSlider != null)
        {
            stoveSlider.value = 0f;
            stoveSlider.gameObject.SetActive(false);
        }
    }

    private bool CheckForValidRecipe()
    {
        foreach (var recipe in recipeBook)
        {
            if (recipe.Value.Count != addedIngredients.Count) continue;
            if (!recipe.Value.Except(addedIngredients).Any() && !addedIngredients.Except(recipe.Value).Any())
                return true;
        }
        return false;
    }

    public void resetStove()
    {
        addedIngredients.Clear();
        justCooked = false;
        ResetVisuals();
        Debug.Log("Nagaksaya ng pagkain ba");

        if (stoveSlider != null)
        {
            stoveSlider.value = 0f;
            stoveSlider.gameObject.SetActive(false);
        }
    }

    void ResetVisuals()
    {
        classicChicken.SetActive(false);
        garlic.SetActive(false);
        liquid1.SetActive(false);
        liquid2.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            interactKey = other.CompareTag("Player") ? KeyCode.Space : KeyCode.Return;
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
