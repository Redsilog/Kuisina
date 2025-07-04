using UnityEngine;

public class IngredientBox : MonoBehaviour, Interaction

{
    public string ingredientName;
    public GameObject ingredientPrefab;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            if (playerInventory != null && !playerInventory.HasIngredient())
            {
                playerInventory.PickUpIngredient(ingredientName, ingredientPrefab);
                Debug.Log("Dinampot ang " + ingredientName);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            Debug.Log("Abot");
        }
        //Remove if not needed just for testing 
        if (other.CompareTag("Waiter"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            Debug.Log("Abot");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerInventory = null;
        }
        //Remove if not needed just for testing 
        if (other.CompareTag("Waiter"))
        {
            playerInRange = false;
            playerInventory = null;
        }
    }

    public string GetDescription() => $"Pick up {ingredientName}";
    public KeyCode GetKey() => KeyCode.Space;


    public void Interact()
    {
        var player = Object.FindFirstObjectByType<PlayerInventory>();
        if (player != null && !player.HasIngredient() && player.heldDish == "")
        {
            player.PickUpIngredient(ingredientName, ingredientPrefab);
            Debug.Log($"Picked up {ingredientName}");
        }
    }
}
