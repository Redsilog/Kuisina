using UnityEngine;

public class FoodServing : MonoBehaviour
{
    private bool playerInRange = false;
    private PlayerInventory playerInventory;

    public Transform servingSpot;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            if (playerInventory != null && playerInventory.HasDish())
            {
                // 🟢 Use the correct serving position with small Y offset
                Vector3 placePosition = servingSpot != null
                    ? servingSpot.position + Vector3.up * 0.05f
                    : transform.position + Vector3.up;

                playerInventory.PlaceDish(placePosition);
                Debug.Log("Serving spot world position: " + servingSpot.position);
                Debug.Log("Dish served!");
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