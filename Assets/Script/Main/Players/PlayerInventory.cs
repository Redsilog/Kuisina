using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public GameObject rice, ulam;

    public string heldIngredient = "";
    public string heldDish = "";
    public GameObject heldVisual;
    public Transform holdPoint;

    public int playerID = 1;

    public GameObject chatBubblePrefab;
    public Transform chatBubbleSpawnPoint;

    [Header("Animation")]
    public Animator animator;
    public Rigidbody playerRigidbody; // reference to player's Rigidbody for movement detection
    public float walkThreshold = 0.1f; // velocity magnitude to consider as walking

    public bool HasDish() => heldVisual != null;
    public List<FridgeShelfManager> nearbyFridges = new List<FridgeShelfManager>();
    public FridgeShelfManager currentFridge => 
        nearbyFridges.Count > 0 ? nearbyFridges[nearbyFridges.Count - 1] : null;

    private void ClearHeldItem()
    {
        heldVisual = null;
        heldIngredient = "";
        heldDish = "";

        UpdateHoldingAnimation();
    }



    public void PlaceIngredient()
    {
        string temp = heldIngredient;

        if (heldVisual != null)
        {
            if (temp == "Rice")
            {
                rice.SetActive(true);
                Destroy(heldVisual);
            }
            else if (temp == "Ulam")
            {
                ulam.SetActive(true);
                Destroy(heldVisual);
            }
            else
            {
                Destroy(heldVisual);
            }
        }

        ClearHeldItem();
    }

    // inside PlayerInventory class
    public void ClearHeldItemDirect()
    {
        if (heldVisual != null)
            Destroy(heldVisual);
        heldVisual = null;
        heldIngredient = "";
        heldDish = "";
    }


    public void PickUpIngredient(string ingredientName, GameObject prefab)
    {
        heldDish = "";
        heldIngredient = ingredientName;

        Quaternion finalRotation = transform.rotation * prefab.transform.localRotation;
        heldVisual = Instantiate(prefab, holdPoint.position, finalRotation, holdPoint);
        heldVisual.transform.localPosition = Vector3.zero;

        UpdateHoldingAnimation();
    }

    // public void PickUpDish(string dishName, GameObject dishObject)
    // {
    //     PlaceIngredient();
    //     heldDish = dishName;
    //     heldVisual = dishObject;

    //     heldVisual.transform.SetParent(holdPoint);
    //     heldVisual.transform.localPosition = Vector3.zero;
    //     heldVisual.transform.localRotation = Quaternion.identity;

    //     if (heldVisual.TryGetComponent<Collider>(out var c)) c.isTrigger = true;
    //     if (heldVisual.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

    //     UpdateHoldingAnimation();
    // }

    public GameObject PlaceDish(Vector3 position)
    {
        if (heldVisual == null) return null;

        heldVisual.transform.SetParent(null);
        heldVisual.transform.position = position;
        heldVisual.transform.rotation = Quaternion.identity;

        if (heldVisual.TryGetComponent<Collider>(out var c)) c.enabled = true;
        if (heldVisual.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = false;

        GameObject placed = heldVisual;

        heldDish = "";
        heldVisual = null;

        UpdateHoldingAnimation();

        return placed;
    }

    public void PickUpDish(GameObject dish)
    {
        if (heldVisual != null) return;

        heldVisual = dish;
        heldDish = dish.name;

        // Only set transforms if dish is NOT already a child of holdPoint
        if (dish.transform.parent != holdPoint)
        {
            dish.transform.SetParent(holdPoint, worldPositionStays: false);
            dish.transform.localPosition = Vector3.zero;
        }

        if (dish.TryGetComponent<Collider>(out var c)) c.enabled = false;
        if (dish.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

        Debug.Log("Picked up: " + dish.name);

        ChatBubble.Create(
            parent: chatBubbleSpawnPoint,
            localPosition: new Vector3(0, 3f, 0f), // adjust height as needed
            iconType: ChatBubble.IconType.Dish,
            text: GetDishDescription(dish),
            prefab: chatBubblePrefab,
            lifetime: 5f
        );

        UpdateHoldingAnimation();
    }

    public bool HasIngredient()
    {
        return !string.IsNullOrEmpty(heldIngredient);
    }

    public bool IsHoldingItem()
    {
        return heldVisual != null || !string.IsNullOrEmpty(heldIngredient) || !string.IsNullOrEmpty(heldDish);
    }

    void Update()
    {
        UpdateHoldingAnimation();
    }

    private void UpdateHoldingAnimation()
    {
        if (animator == null || playerRigidbody == null) return;

        if (IsHoldingItem())
        {
            float speed = playerRigidbody.linearVelocity.magnitude;
            if (speed > walkThreshold)
            {
                animator.SetBool("IsHoldingWalk", true);
                animator.SetBool("IsHoldingStill", false);
            }
            else
            {
                animator.SetBool("IsHoldingWalk", false);
                animator.SetBool("IsHoldingStill", true);
            }
        }
        else
        {
            animator.SetBool("IsHoldingWalk", false);
            animator.SetBool("IsHoldingStill", false);
        }
    }
    private string GetDishDescription(GameObject dish)
    {
        if (dish.TryGetComponent<DishReference>(out var info))
        {
            return string.IsNullOrEmpty(info.description) 
                ? "A delicious dish!" 
                : info.description;
        }

        return "Picked up a dish!";
    }

}
