using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class CookingStation : MonoBehaviour
{
    public Transform placePoint;          // Where the ingredient should be placed on the station
    private bool playerInRange = false;   // Whether the player is close enough
    private GameObject heldObject = null; // The object that the player is holding

    public string[] allowedIngredientNames = { "Bun", "Meat", "Cheese", "Bottom Bun" };

    public List<string> placedIngredientNames = new List<string>();
    public List<string> placedIngredients = new List<string>();
    public List<Recipe> recipes = new List<Recipe>();  // Add recipes in the Inspector
    private List<string> currentIngredients = new List<string>();  // Tracks what the player puts in

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered interaction range. Press E to place ingredient.");
            playerInRange = true;
            heldObject = other.GetComponent<playerMovement>().GetHeldObject(); // Get the held object from the player
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player left interaction range.");
            playerInRange = false;
            heldObject = null; // Reset when the player leaves the station
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Get the latest held object directly from the player
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                heldObject = player.GetComponent<playerMovement>().GetHeldObject();
            }

            if (heldObject != null)
            {
                Debug.Log("E pressed. Placing ingredient at the cooking station.");
                PlaceIngredient();
            }
            else
            {
                Debug.Log("No object held when trying to place.");
            }
        }
    }

    void PlaceIngredient()
    {

        if (heldObject != null && placePoint != null)
        {
            // Detach the object from the camera and reset physics
            Rigidbody heldObjectRb = heldObject.GetComponent<Rigidbody>();
            if (heldObjectRb != null)
            {
                heldObjectRb.isKinematic = false;  // Enable physics for the ingredient
            }

            // Set the object's position to the placePoint on the Cooking Station
            heldObject.transform.position = placePoint.position;
            heldObject.transform.rotation = Quaternion.identity;  // Optional: reset rotation


            string cleanedName = heldObject.name.Split('(')[0].Trim();
            placedIngredientNames.Add(cleanedName);
            placedIngredients.Add(cleanedName);
            currentIngredients.Add(cleanedName);

            // Clear the reference in the player
            heldObject.transform.SetParent(null);
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.GetComponent<playerMovement>().RemoveHeldObject(heldObject);
            }

            heldObject = null;

            Debug.Log("Placed ingredient: " + cleanedName);
        }

        // Check for matching recipe
        foreach (Recipe foodRecipe in recipes)
        {
            if (foodRecipe.ingredients.Count == currentIngredients.Count &&
                !foodRecipe.ingredients.Except(currentIngredients).Any())
            {
                Debug.Log("Recipe matched: " + foodRecipe.recipeName);
                currentIngredients.Clear(); // Reset for next cooking
                break;
            }
        }

        bool isAllowed = false;
        foreach (string name in allowedIngredientNames)
        {
            if (heldObject.name.StartsWith(name))
            {
                isAllowed = true;
                break;
            }
        }
        if (!isAllowed)
        {
            Debug.Log("This ingredient is not allowed.");
            return;
        }
    }
}
