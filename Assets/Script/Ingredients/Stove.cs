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

    private Dictionary<string, List<string>> recipeBook;

    private Dictionary<string, GameObject> cookedPrefabs;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private GameObject currentCookedFood;

    [SerializeField]
    private float ingredientTimer = 0f;
    [SerializeField]
    private float maxWaitTime = 10f; // seconds to wait before checking
    private bool isTimerRunning = false;

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
            //replace onion with chopped onion later
            { "Classic Adobo", new List<string> { "Chicken", "Soy Sauce", "Vinegar", "Onion"}},
            { "Adobong Puti", new List<string> { "Chicken", "Vinegar", "Garlic", "Salt"}},

            //Pancit
            //replace beef and pork with chopped versions later
            { "Pancit Malabon", new List<string> { "Noodles", "Pork", "Mussels", "Dried Fish"}},
            { "Pancit Batil Patung", new List<string> { "Beef", "Pork", "Noodles", "Chicharon"}},

            //Sinigang
            { "Sinigang na Baboy", new List<string> { "Pork", "Onion", "Tomato", "Radish", "Eggplant", "Green Chili", "String Beans", "Okra", "Kangkong"}},
            { "Cansi", new List<string> { "Beef", "Garlic", "Onion", "Tomato", "Lemongrass", "Jackfruit"}},

            //Sisig
            //replace pork and chicken with chopped versions later
            { "Kapampangan Sisig", new List<string> { "Pork", "Chicken"}},
            { "Dinakdakan", new List<string> { "Pork", "Vinegar"}}
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
            // { "Pancit Batil Patung", cookedPancitBatil},

            //Sinigang
            // { "Sinigang na Baboy", cookedSinigangBaboy},
            // { "Cansi", cookedSinigangCansi},

            //Sisig
            // { "Kapampangan Sisig", cookedKapampanganSisig},
            // { "Dinakdakan", cookedDinakdakanSisig}
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
        addedIngredients.Add(player.heldIngredient);
        player.PlaceIngredient();

        if (CheckForValidRecipe())
        {
            Debug.Log("Correct recipe made immediately!");
            CheckRecipes();
            isTimerRunning = false;
        }
        else
        {
            // Start or reset the timer if not yet valid
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

    public void resetStove()
    {
        addedIngredients.Clear();
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
}
