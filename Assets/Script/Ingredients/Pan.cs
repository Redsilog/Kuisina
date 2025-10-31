using UnityEngine;
using UnityEngine.UI;

public class Pan : MonoBehaviour
{
    [Header("Cooking Settings")]
    public Transform cookPoint;
    public float cookTime = 3f;
    public bool IsCookingActive() => isCooking;

    [Header("Cooked Ingredient Prefabs")]
    public GameObject cookedHotdogPrefab;
    public GameObject cookedTocinoPrefab;
    public GameObject cookedTapaPrefab;
    public GameObject cookedItlogPrefab;
    public GameObject cookedSinangagPrefab;
    public GameObject cookedChickenPrefab; // Adobo

    [Header("UI")]
    public Slider cookingProgressBar;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private KeyCode interactKey = KeyCode.Space;

    private bool isCooking = false;
    private bool isCooked = false;
    private float cookingTimer = 0f;

    private string cookedIngredientName = "";
    private GameObject foodVisualOnPan;

    void Start()
    {
        if (cookingProgressBar != null)
        {
            cookingProgressBar.gameObject.SetActive(false);
            cookingProgressBar.value = 0f;
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (!isCooking && !isCooked && playerInventory.HasIngredient())
            {
                StartCooking(playerInventory.heldIngredient, playerInventory.heldVisual);
                playerInventory.PlaceIngredient();
            }
            else if (isCooked && !playerInventory.HasIngredient())
            {
                PickUpCookedFood();
            }
        }

        if (isCooking)
        {
            cookingTimer += Time.deltaTime;

            if (cookingProgressBar != null)
            {
                cookingProgressBar.gameObject.SetActive(true);
                cookingProgressBar.value = cookingTimer / cookTime;
            }

            if (cookingTimer >= cookTime)
            {
                FinishCooking();
            }
        }
    }

    void StartCooking(string ingredient, GameObject rawVisual)
    {
        isCooking = true;
        cookingTimer = 0f;
        cookedIngredientName = "Cooked " + ingredient;

        if (foodVisualOnPan != null)
            Destroy(foodVisualOnPan);

        foodVisualOnPan = Instantiate(rawVisual, cookPoint.position, cookPoint.rotation);
    }

    void FinishCooking()
    {
        isCooking = false;
        cookingTimer = 0f;

        if (cookingProgressBar != null)
        {
            cookingProgressBar.value = 0f;
            cookingProgressBar.gameObject.SetActive(false);
        }

        if (foodVisualOnPan != null)
            Destroy(foodVisualOnPan);

        GameObject cookedVisual = GetCookedVisual(cookedIngredientName);
        if (cookedVisual != null)
        {
            foodVisualOnPan = Instantiate(cookedVisual, cookPoint.position, cookPoint.rotation);
            isCooked = true;
        }
    }

    void PickUpCookedFood()
    {
        if (foodVisualOnPan != null)
            Destroy(foodVisualOnPan);

        GameObject cookedVisual = GetCookedVisual(cookedIngredientName);
        if (cookedVisual != null)
            playerInventory.PickUpIngredient(cookedIngredientName, cookedVisual);

        isCooked = false;
        cookedIngredientName = "";
    }

    GameObject GetCookedVisual(string name)
    {
        switch (name)
        {
            case "Cooked Hotdog": return cookedHotdogPrefab;
            case "Cooked Tocino": return cookedTocinoPrefab;
            case "Cooked Tapa": return cookedTapaPrefab;
            case "Cooked Itlog": return cookedItlogPrefab;
            case "Cooked Sinangag": return cookedSinangagPrefab;
            case "Cooked Chicken": return cookedChickenPrefab;
            default: return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();

            // Assign correct key for each player
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
