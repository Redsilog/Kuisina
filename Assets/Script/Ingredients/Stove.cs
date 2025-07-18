using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class Stove : MonoBehaviour
{
    public Transform cookedFoodPoint;

    public GameObject cookedHotsilog;
    public GameObject cookedTocilog;
    public GameObject cookedTapsilog;

    //Adobo
    public GameObject cookedClassicAdobo;
    public GameObject cookedAdobongPuti;

    //Pancit
    public GameObject cookedPancitMalabon;
    public GameObject cookedPancitBatil;

    //Sinigang
    public GameObject cookedSinigangBaboy;
    public GameObject cookedSinigangCansi;

    //Sisig
    public GameObject cookedKapampanganSisig;
    public GameObject cookedDinakdakanSisig;

    //failed dish
    public GameObject failedDish;

    [SerializeField]
    private List<string> addedIngredients = new List<string>();

    [SerializeField]
    private float ingredientTimer = 0f;
    [SerializeField]
    private float maxWaitTime = 10f; // seconds to wait before checking

    private Dictionary<string, List<string>> recipeBook;

    private Dictionary<string, GameObject> cookedPrefabs;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private GameObject currentCookedFood;

    [Header("Cooked State Adobo")]
    public GameObject liquid1;
    public GameObject liquid2;
    public GameObject garlic;
    public GameObject classicChicken;
    public GameObject putiChicken;
    public GameObject v1, v2;
    private bool suka = false;

    private bool isTimerRunning = false;

    [SerializeField]
    int adoboCounter = 0;



    void Start()
    {
        recipeBook = new Dictionary<string, List<string>>()
        {
            { "Hotsilog", new List<string> { "Cooked Hotdog", "Cooked Sinangag", "Cooked Itlog" } },
            { "Tocilog", new List<string> { "Cooked Tocino", "Cooked Sinangag", "Cooked Itlog" } },
            { "Tapsilog", new List<string> { "Cooked Tapa", "Cooked Sinangag", "Cooked Itlog" } },
            { "Chicken", new List<string> { "Cooked Chicken", "Tuyo", "Vinegar", "Cooked Garlic" } },

            //Adobo
            //replace chicken with cooked version later
            { "Classic Adobo", new List<string> { "Chicken", "Soy Sauce", "Vinegar", "Chopped Onion"}},
            { "Adobong Puti", new List<string> { "Chicken", "Vinegar", "Garlic", "Salt"}},

            //Pancit
            { "Pancit Malabon", new List<string> { "Noodles", "Chopped Pork", "Mussels", "Dried Fish"}},
            { "Pancit Batil Patung", new List<string> { "Chopped Beef", "Chopped Pork", "Noodles", "Chicharon"}},

            //Sinigang
            { "Sinigang na Baboy", new List<string> { "Pork", "Onion", "Tomato", "Radish", "Eggplant", "Green Chili", "String Beans", "Okra", "Kangkong"}},
            { "Cansi", new List<string> { "Beef", "Garlic", "Onion", "Tomato", "Lemongrass", "Jackfruit"}},

            //Sisig
            //replace pork and chicken with chopped versions later
            { "Kapampangan Sisig", new List<string> { "Chopped Pork", "Chopped Chicken"}},
            { "Dinakdakan", new List<string> { "Chopped Pork", "Vinegar"}}
        };

        cookedPrefabs = new Dictionary<string, GameObject>()
        {
            { "Hotsilog", cookedHotsilog },
            { "Tocilog", cookedTocilog },
            { "Tapsilog", cookedTapsilog },
            //Adobo
            { "Classic Adobo", cookedClassicAdobo},
            { "Adobong Puti", cookedAdobongPuti},

            //Pancit
            { "Pancit Malabon", cookedPancitMalabon},
            { "Pancit Batil Patung", cookedPancitBatil},

            //Sinigang
            // { "Sinigang na Baboy", cookedSinigangBaboy},
            // { "Cansi", cookedSinigangCansi},

            //Sisig
            // { "Kapampangan Sisig", cookedKapampanganSisig},
            // { "Dinakdakan", cookedDinakdakanSisig}
        };

        classicChicken.SetActive(false);
        putiChicken.SetActive(false);
        garlic.SetActive(false);
        liquid1.SetActive(false);
        liquid2.SetActive(false);
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

        if (Input.GetKeyDown(KeyCode.P) && currentCookedFood != null)
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
        string ingredient = player.heldIngredient;
        Debug.Log("Adding ingredient: " + ingredient); 
        addedIngredients.Add(player.heldIngredient);
        player.PlaceIngredient();

        switch (ingredient)
        {
            case "Cooked Garlic":
            case "Garlic":
                garlic.SetActive(true);
                break;

            case "Cooked Chicken":
            case "Chicken":
                classicChicken.SetActive(true);
                break;

            case "Soy Sauce":
                liquid1.SetActive(true);
                break;

            case "Vinegar":
                liquid2.SetActive(true);
                break;
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
                ResetVisuals();

                return;
            }
        }

        if (!foundRecipe)
        {
            Debug.Log("Walang ganyan boss");
            addedIngredients.Clear();
            ResetVisuals();
            Debug.Log("Stove cleared.");
        }
    }

    public void resetStove()
    {
        addedIngredients.Clear();
        ResetVisuals();
        Debug.Log("Nagaksaya ng pagkain ba");
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
    
    void ResetVisuals()
    {
        classicChicken.SetActive(false);
        garlic.SetActive(false);
        liquid1.SetActive(false);
        liquid2.SetActive(false);
    }
}
