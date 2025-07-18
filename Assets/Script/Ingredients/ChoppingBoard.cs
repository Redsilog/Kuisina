using UnityEngine;
using UnityEngine.UI;

public class ChoppingBoard : MonoBehaviour
{
    public Transform chopPoint;
    public float chopTime = 3f;

    [Header("Chopped Prefabs")]
    public GameObject choppedGarlicPrefab;
    public GameObject choppedOnionPrefab;
    //add dito

    private bool playerInRange = false;
    private PlayerInventory playerInventory;

    private bool isChopping = false;
    private bool isChopped = false;
    private string choppedIngredientName = "";
    private GameObject foodOnBoard;

    public Slider choppingProgressBar;

    private float choppingTimer = 0f;

    void Start()
    {
        if (choppingProgressBar != null)
            choppingProgressBar.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            if (!isChopping && !isChopped && playerInventory.HasIngredient())
            {
                StartChopping(playerInventory.heldIngredient, playerInventory.heldVisual);
                playerInventory.PlaceIngredient();
            }
            else if (isChopped && !playerInventory.HasIngredient())
            {
                PickUpChoppedIngredient();
            }
        }

        if (isChopping)
        {
            choppingTimer += Time.deltaTime;

            if (choppingProgressBar != null)
            {
                choppingProgressBar.gameObject.SetActive(true);
                choppingProgressBar.value = choppingTimer / chopTime;
            }

            if (choppingTimer >= chopTime)
            {
                FinishChopping();
            }
        }
    }

    void StartChopping(string ingredient, GameObject rawVisual)
    {
        isChopping = true;
        choppingTimer = 0f;
        choppedIngredientName = "Chopped " + ingredient;

        foodOnBoard = Instantiate(rawVisual, chopPoint.position, chopPoint.rotation);
    }

    void FinishChopping()
    {
        isChopping = false;
        choppingTimer = 0f;

        if (choppingProgressBar != null)
        {
            choppingProgressBar.value = 0;
            choppingProgressBar.gameObject.SetActive(false);
        }

        if (choppedIngredientName != "")
        {
            Destroy(foodOnBoard);
            GameObject choppedVisual = GetChoppedVisual(choppedIngredientName);
            if (choppedVisual != null)
            {
                foodOnBoard = Instantiate(choppedVisual, chopPoint.position, chopPoint.rotation);
                isChopped = true;
            }
        }
    }

    void PickUpChoppedIngredient()
    {
        if (foodOnBoard != null)
            Destroy(foodOnBoard);

        GameObject choppedVisual = GetChoppedVisual(choppedIngredientName);
        if (choppedVisual != null)
            playerInventory.PickUpIngredient(choppedIngredientName, choppedVisual);

        isChopped = false;
        choppedIngredientName = "";
    }

    GameObject GetChoppedVisual(string name)
    {
        switch (name)
        {
            case "Chopped Garlic": return choppedGarlicPrefab;
            case "Chopped Onion": return choppedOnionPrefab;
            //add dito
            default: return null;
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
}
