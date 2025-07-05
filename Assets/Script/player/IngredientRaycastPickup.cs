using UnityEngine;
using TMPro; // Required for TextMeshPro

public class IngredientRaycastPickup : MonoBehaviour
{
    public float pickupRange = 3f;
    public Camera playerCamera;
    private PlayerInventory inventory;

    public TextMeshProUGUI pickupText;

    private IngredientObject currentLookedAtIngredient;

    void Start()
    {
        inventory = GetComponent<PlayerInventory>();

        if (pickupText != null)
        {
            pickupText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        ShowPickupPrompt();

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    void ShowPickupPrompt()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider.CompareTag("Ingredient"))
            {
                IngredientObject ingredient = hit.collider.GetComponent<IngredientObject>();

                if (ingredient != null && !inventory.HasIngredient())
                {
                    pickupText.text = "[E]\nPick up " + ingredient.ingredientName;
                    pickupText.gameObject.SetActive(true);
                    currentLookedAtIngredient = ingredient;
                    return;
                }
            }
        }

        pickupText.gameObject.SetActive(false);
        currentLookedAtIngredient = null;
    }

    void TryPickup()
    {
        if (currentLookedAtIngredient != null && !inventory.HasIngredient())
        {
            inventory.PickUpIngredient(currentLookedAtIngredient.ingredientName, currentLookedAtIngredient.ingredientPrefab);
            pickupText.gameObject.SetActive(false);
            Debug.Log("Picked up " + currentLookedAtIngredient.ingredientName);
            currentLookedAtIngredient = null;
        }
    }
}
