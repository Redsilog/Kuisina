using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class Stove : MonoBehaviour
{
    [SerializeField] private float ingredientTimer = 0f;
    [SerializeField] private float maxWaitTime = 10f;
    private bool isTimerRunning = false;

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
    private Dictionary<string, List<string>> recipeBook;
    private Dictionary<string, GameObject> cookedPrefabs;

    // ✅ now tracks both players separately
    private bool playerInRange = false;
    private PlayerInventory player1Inventory;
    private PlayerInventory player2Inventory;

    private GameObject currentCookedFood;

    [Header("Cooked State Visuals")]
    public GameObject liquid1;
    public GameObject liquid2;
    public GameObject garlic;
    public GameObject classicChicken;
    public GameObject putiChicken;

    private bool justCooked = false;

    //AUDIO
    [SerializeField] private AudioClip cookingClip;
    [SerializeField] private AudioClip burntClip;
    [SerializeField] private AudioClip finishedCookingClip;

    void Start()
    {
        recipeBook = new Dictionary<string, List<string>>()
        {
            { "Hotsilog", new List<string> { "Cooked Hotdog", "Cooked Sinangag", "Cooked Itlog" } },
            { "Tocilog", new List<string> { "Cooked Tocino", "Cooked Sinangag", "Cooked Itlog" } },
            { "Tapsilog", new List<string> { "Cooked Tapa", "Cooked Sinangag", "Cooked Itlog" } },
            { "Chicken", new List<string> { "Cooked Chicken", "Tuyo", "Vinegar", "Cooked Garlic" } },

            { "Classic Adobo", new List<string> { "Chicken", "Soy Sauce", "Vinegar", "Chopped Garlic"} },
            { "Adobong Puti", new List<string> { "Chicken", "Vinegar", "Chopped Garlic"} },

            { "Pancit Malabon", new List<string> { "Noodles", "Chopped Pork", "Mussels", "Dried Fish"} },
            { "Pancit Batil Patung", new List<string> { "Chopped Beef", "Chopped Pork", "Noodles", "Chicharon"} },

            { "Sinigang na Baboy", new List<string> { "Pork", "Onion", "Tomato", "Radish", "Eggplant", "Green Chili", "String Beans", "Okra", "Kangkong"} },
            { "Cansi", new List<string> { "Beef", "Garlic", "Onion", "Tomato", "Lemongrass", "Jackfruit"} },

            { "Kapampangan Sisig", new List<string> { "Chopped Pork", "Chopped Chicken"} },
            { "Dinakdakan", new List<string> { "Chopped Pork", "Vinegar"} }
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
            { "Sinigang na Baboy", cookedSinigangBaboy },
            { "Cansi", cookedSinigangCansi },
            { "Kapampangan Sisig", cookedKapampanganSisig },
            { "Dinakdakan", cookedDinakdakanSisig },
        };

        ResetVisuals();

        if (stoveSlider != null)
            stoveSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        // ✅ Player 1 actions (Space)
        if (player1Inventory != null)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (player1Inventory.HasIngredient())
                {
                    AddIngredient(player1Inventory);
                }
                else if (currentCookedFood != null && !player1Inventory.HasDish())
                {
                    if (justCooked)
                    {
                        justCooked = false;
                    }
                    else
                    {
                        player1Inventory.PickUpDish("Dish", currentCookedFood);
                        currentCookedFood = null;
                        Debug.Log("Player 1 picked up dish");
                    }
                }
            }
        }

        // ✅ Player 2 actions (Return)
        if (player2Inventory != null)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (player2Inventory.HasIngredient())
                {
                    AddIngredient(player2Inventory);
                }
                else if (currentCookedFood != null && !player2Inventory.HasDish())
                {
                    if (justCooked)
                    {
                        justCooked = false;
                    }
                    else
                    {
                        player2Inventory.PickUpDish("Dish", currentCookedFood);
                        currentCookedFood = null;
                        Debug.Log("Player 2 picked up dish");
                    }
                }
            }
        }

        // ✅ Reset stove (Tab works for anyone)
        if (playerInRange && Input.GetKeyDown(KeyCode.Tab))
        {
            resetStove();
            SoundFXManager.instance.StopLoopingSound(); 
        }

        // ✅ Serve dish (P works for anyone)
        if (playerInRange && Input.GetKeyDown(KeyCode.P) && currentCookedFood != null)
        {
            Destroy(currentCookedFood);
            currentCookedFood = null;
            Debug.Log("Served na boss");
        }

        // ✅ Ingredient timeout countdown
        if (isTimerRunning)
        {
            ingredientTimer += Time.deltaTime;

            if (stoveSlider != null)
            {
                stoveSlider.gameObject.SetActive(true);
                //removed "1f -"
                //forces slider to go from right to left, dk if it changes cook time tho
                stoveSlider.value = ingredientTimer / maxWaitTime;
            }

            if (ingredientTimer >= maxWaitTime)
            {
                Debug.Log("Na-burn boss, di ka nagdagdag ng ingredient sa oras!");
                resetStove();
                isTimerRunning = false;
                SoundFXManager.instance.PlaySoundFXClip(burntClip, transform, 1f);
                SoundFXManager.instance.StopLoopingSound(); 
            }
        }
    }

    public void AddIngredient(PlayerInventory player)
    {
        string ingredient = player.heldIngredient;
        Debug.Log("Adding ingredient: " + ingredient);
        addedIngredients.Add(ingredient);
        player.PlaceIngredient();

        if (!isTimerRunning)
        {
            SoundFXManager.instance.PlayLoopWithCrossfade(cookingClip, transform, 1f, 2f);
        }
        
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

        string matchedRecipe = GetMatchingRecipe();
        if (matchedRecipe != null)
        {
            Debug.Log("Matched recipe: " + matchedRecipe);

            if (cookedPrefabs.TryGetValue(matchedRecipe, out GameObject foodPrefab))
            {
                currentCookedFood = Instantiate(foodPrefab, cookedFoodPoint.position, cookedFoodPoint.rotation);
                justCooked = true;
            }

            addedIngredients.Clear();
            ResetVisuals();
            isTimerRunning = false;
            SoundFXManager.instance.PlaySoundFXClip(finishedCookingClip, transform, 1f);
            SoundFXManager.instance.StopLoopingSound();

            if (stoveSlider != null)
            {
                stoveSlider.value = 0f;
                stoveSlider.gameObject.SetActive(false);
            }
        }
        else
        {
            // Start or restart timer after each added ingredient
            ingredientTimer = 0f;
            isTimerRunning = true;
            SoundFXManager.instance.PlayLoopWithCrossfade(cookingClip, transform, 1f, 2f);
        }
    }

    private string GetMatchingRecipe()
    {
        var actual = addedIngredients.OrderBy(i => i).ToList();

        foreach (var recipe in recipeBook)
        {
            var expected = recipe.Value.OrderBy(i => i).ToList();

            if (expected.SequenceEqual(actual))
                return recipe.Key;
        }

        return null;
    }

    public void resetStove()
    {
        addedIngredients.Clear();
        justCooked = false;
        ResetVisuals();

        if (currentCookedFood != null)
        {
            Destroy(currentCookedFood);
            currentCookedFood = null;
        }

        isTimerRunning = false;
        ingredientTimer = 0f;
        SoundFXManager.instance.StopLoopingSound();

        if (stoveSlider != null)
        {
            stoveSlider.value = 0f;
            stoveSlider.gameObject.SetActive(false);
        }

        Debug.Log("Nagaksaya ng pagkain ba");
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
        if (other.CompareTag("Player"))
        {
            player1Inventory = other.GetComponent<PlayerInventory>();
            playerInRange = true;
        }
        else if (other.CompareTag("Player2"))
        {
            player2Inventory = other.GetComponent<PlayerInventory>();
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player1Inventory = null;
        }
        else if (other.CompareTag("Player2"))
        {
            player2Inventory = null;
        }

        // ✅ Only set false if BOTH players are gone
        if (player1Inventory == null && player2Inventory == null)
        {
            playerInRange = false;
        }
    }
}
