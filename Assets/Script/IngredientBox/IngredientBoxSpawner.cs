using UnityEngine;

public class IngredientBoxSpawner : MonoBehaviour
{
    public GameObject ingredientPrefab;  // The object to spawn
    public Transform spawnPoint;         // Where to spawn the ingredient (optional)

    private bool playerInRange = false;
    private GameObject spawnedObject = null;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered interaction range. Press E to spawn ingredient.");
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player left interaction range.");
            playerInRange = false;
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && spawnedObject == null)
        {
            Debug.Log("E pressed in interaction range. Attempting to spawn ingredient.");
            SpawnIngredient();
        }
    }

    void SpawnIngredient()
    {
        if (ingredientPrefab == null)
        {
            Debug.LogWarning("Ingredient prefab is not assigned!");
            return;
        }

        // Spawn the ingredient at the spawn point or box position
        spawnedObject = Instantiate(ingredientPrefab, spawnPoint.position, spawnPoint.rotation);

        Debug.Log("Ingredient spawned.");
    }

    public GameObject GetSpawnedObject()
    {
        return spawnedObject;
    }
}
