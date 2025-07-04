using UnityEngine;

public class IngredientBox : MonoBehaviour
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
