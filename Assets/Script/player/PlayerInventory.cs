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

    [Header("Default Arm Rotation Values")]
    public Vector3 leftArmDefaultRotation = new Vector3(-63.027f, 10.057f, 21.59f);
    public Vector3 rightArmDefaultRotation = new Vector3(-63.027f, 10.057f, 21.59f);

    [Header("Holding Arm Rotation Values")]
    public Vector3 leftArmHoldRotation = new Vector3(25.471f, -68.801f, 62.151f);
    public Vector3 rightArmHoldRotation = new Vector3(25.471f, -68.801f, 62.151f);

    public int playerID = 1;

    private bool isGameInitialized = false;

    public bool HasDish() => heldVisual != null;

    private void ClearHeldItem()
    {
        heldVisual = null;
        heldIngredient = "";
        heldDish = "";

        // Always reset arms when no item is held
        ResetArms();
    }

    // This manually sets the arm rotation to the default or holding rotation
    void ResetArms()
    {
        // Reset arm rotations to the default pose
        leftArm.localRotation = Quaternion.Euler(leftArmDefaultRotation);
        rightArm.localRotation = Quaternion.Euler(rightArmDefaultRotation);

        // Debugging the rotation after reset
        Debug.Log("Left Arm Rotation after Reset: " + leftArm.localRotation.eulerAngles);
        Debug.Log("Right Arm Rotation after Reset: " + rightArm.localRotation.eulerAngles);
    }

    // Set arms to the holding rotation when the player picks something up
    void SetHoldingPose()
    {
        leftArm.localRotation = Quaternion.Euler(leftArmHoldRotation);
        rightArm.localRotation = Quaternion.Euler(rightArmHoldRotation);
    }

    // Place an ingredient and reset arms
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

        // Arms are always reset here, no need to check for initialization anymore
        ResetArms();
    }

    // Pick up an ingredient and set arm pose to the holding pose
    public void PickUpIngredient(string ingredientName, GameObject prefab)
    {
        // Don't call PlaceIngredient here, to prevent unnecessary resets
        heldDish = "";
        heldIngredient = ingredientName;
        heldVisual = Instantiate(prefab, holdPoint.position, Quaternion.identity, holdPoint);

        SetHoldingPose(); // Set the arm pose for holding the ingredient
    }

    // Pick up a dish and set arm pose to the holding pose
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

    // Place a dish in the world and reset the arms when the item is placed
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

    // Pick up a dish (overloaded method)
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

    // Check if the player has an ingredient
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
