using UnityEngine;

public class IngredientBox : MonoBehaviour
{
    public string ingredientName;
    public GameObject ingredientPrefab;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.Space))
        {
            var inventory = other.GetComponent<PlayerInventory>();

            if (inventory != null && !inventory.HasIngredient())
            {
                inventory.PickUpIngredient(ingredientName, ingredientPrefab);
                Debug.Log("Dinampot ang " + ingredientName);
            }
        }
    }
}
