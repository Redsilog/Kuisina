using UnityEngine;

public class IngredientBox : MonoBehaviour
{
    public string ingredientName;
    public GameObject ingredientPrefab;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private KeyCode interactKey = KeyCode.Space;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
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
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();

            if (other.CompareTag("Player"))
                interactKey = KeyCode.Space;
            else if (other.CompareTag("Player2"))
                interactKey = KeyCode.Return;

            Debug.Log("Abot: " + other.tag);
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
