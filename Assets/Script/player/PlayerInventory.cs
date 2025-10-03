using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public GameObject rice, ulam;

    [Header("Inventory State")]
    [Tooltip("Name of the held ingredient (empty if none)")]
    public string heldIngredient = "";
    [Tooltip("Name of the held dish (empty if none)")]
    public string heldDish = "";
    [Tooltip("Visual GameObject of whatever is held")]
    public GameObject heldVisual;
    [Tooltip("Transform under which held items are parented")]
    public Transform holdPoint;

    [Header("Arms Setup")]
    public Transform leftArm;
    public Transform rightArm;

    [Header("Default Arm Poses")]
    public Transform leftArmDefaultPose;
    public Transform rightArmDefaultPose;

    [Header("Holding Arm Poses")]
    public Transform leftArmHoldPose;
    public Transform rightArmHoldPose;

    public int playerID = 1;

    // Flag to prevent reset during initialization or reload
    private bool isGameInitialized = false;

    public bool HasDish() => heldVisual != null;

    private void ClearHeldItem()
    {
        heldVisual = null;
        heldIngredient = "";
        heldDish = "";

        // Only reset arms when the game is initialized
        if (isGameInitialized)
        {
            ResetArms(); // Only reset arms after initialization
        }
    }

    void ApplyPose(Transform arm, Transform pose)
    {
        if (arm != null && pose != null)
        {
            arm.localPosition = pose.localPosition;
            arm.localRotation = pose.localRotation;
        }
    }

    void ResetArms()
    {
        ApplyPose(leftArm, leftArmDefaultPose);
        ApplyPose(rightArm, rightArmDefaultPose);

        // Debugging the positions
        Debug.Log("Left Arm Position after Reset: " + leftArm.localPosition);
        Debug.Log("Right Arm Position after Reset: " + rightArm.localPosition);
    }

    void SetHoldingPose()
    {
        ApplyPose(leftArm, leftArmHoldPose);
        ApplyPose(rightArm, rightArmHoldPose);
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

        ClearHeldItem(); // Reset held item and arms
    }

    public void PickUpIngredient(string ingredientName, GameObject prefab)
    {
        // Don't call PlaceIngredient here, to prevent unnecessary resets
        heldDish = "";
        heldIngredient = ingredientName;
        heldVisual = Instantiate(prefab, holdPoint.position, Quaternion.identity, holdPoint);

        SetHoldingPose(); // Set the arm pose for holding the ingredient
    }

    public void PickUpDish(string dishName, GameObject dishObject)
    {
        // Prevent resetting arms when picking up a dish
        PlaceIngredient(); // Place any previous item before picking a new dish

        heldDish = dishName;
        heldVisual = dishObject;

        heldVisual.transform.SetParent(holdPoint);
        heldVisual.transform.localPosition = Vector3.zero;
        heldVisual.transform.localRotation = Quaternion.identity;

        if (heldVisual.TryGetComponent<Collider>(out var c)) c.isTrigger = true;
        if (heldVisual.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

        SetHoldingPose(); // Set the arm pose for holding the dish
    }

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

        ResetArms(); // Reset arms when the item is placed

        return placed;
    }

    public void PickUpDish(GameObject dish)
    {
        if (heldVisual != null) return; // Already holding something

        heldVisual = dish;
        heldDish = dish.name;

        dish.transform.SetParent(holdPoint);
        dish.transform.localPosition = Vector3.zero;
        dish.transform.localRotation = Quaternion.identity;

        if (dish.TryGetComponent<Collider>(out var c)) c.enabled = false;
        if (dish.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

        Debug.Log("Picked up: " + dish.name);

        SetHoldingPose(); // Set the arm pose for holding the dish
    }

    public bool HasIngredient()
    {
        return !string.IsNullOrEmpty(heldIngredient);
    }

    // Call this method once the game is fully initialized (when the first item is picked up)
    public void InitializeGame()
    {
        isGameInitialized = true;
        Debug.Log("Game Initialized");
    }
}
