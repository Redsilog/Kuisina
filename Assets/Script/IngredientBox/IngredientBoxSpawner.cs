using UnityEngine;

public class IngredientBoxSpawner : MonoBehaviour
{
    public GameObject ingredientPrefab;  // The object to spawn
    public Transform holdPoint;          // The transform under camera where held object is attached

    private bool playerInRange = false;
    private GameObject heldObject = null;

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
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E pressed in interaction range. Attempting to spawn ingredient.");
            SpawnAndHold();
        }
    }

    void SpawnAndHold()
    {
        if (ingredientPrefab == null)
        {
            Debug.LogWarning("Ingredient prefab is not assigned!");
            return;
        }

        if (heldObject != null)
        {
            Debug.Log("Already holding an object.");
            return; // Optional: prevent spawning multiple held objects
        }

        // Instantiate at holdPoint position and rotation, parented to holdPoint
        heldObject = Instantiate(ingredientPrefab, holdPoint.position, holdPoint.rotation, holdPoint);

        // Reset local position and rotation to zero so it sits exactly at holdPoint
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;

        Debug.Log("Spawned and holding the ingredient.");
    }
}
