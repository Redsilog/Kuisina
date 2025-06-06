using UnityEngine;

public class CookingStation : MonoBehaviour
{
    public Transform placePoint;          // Where the ingredient should be placed on the station
    private bool playerInRange = false;   // Whether the player is close enough
    private GameObject heldObject = null; // The object that the player is holding

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
        // If the player is in range and presses E, place the held object at the station
        if (playerInRange && heldObject != null && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E pressed. Placing ingredient at the cooking station.");
            PlaceIngredient();
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

            // Clear the reference in the player
            heldObject.transform.SetParent(null);
            heldObject = null;

            Debug.Log("Ingredient placed at the cooking station.");
        }
    }
}
