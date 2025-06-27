using UnityEngine;

public class IngredientBox : MonoBehaviour
{
    public string ingredientName;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;

    void Update()
    {
        if (!playerInRange || playerInventory == null || !playerInventory.IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Space) && !playerInventory.HasIngredient())
        {
            playerInventory.PickUpIngredient(ingredientName);
            Debug.Log("Picked up " + ingredientName);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInventory = other.GetComponent<PlayerInventory>();
            if (playerInventory != null) playerInRange = true;
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
