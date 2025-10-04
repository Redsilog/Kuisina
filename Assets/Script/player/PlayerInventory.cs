using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public GameObject rice, ulam;

    public string heldIngredient = "";
    public string heldDish = "";
    public GameObject heldVisual;
    public Transform holdPoint;

    public Transform leftArm;
    public Transform rightArm;

    public Vector3 leftArmDefaultRotation = new Vector3(-63.027f, 10.057f, 21.59f);
    public Vector3 rightArmDefaultRotation = new Vector3(-63.027f, 10.057f, 21.59f);

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
        ResetArms();
    }

    void ResetArms()
    {
        leftArm.localRotation = Quaternion.Euler(leftArmDefaultRotation);
        rightArm.localRotation = Quaternion.Euler(rightArmDefaultRotation);
    }

    void SetHoldingPose()
    {
        leftArm.localRotation = Quaternion.Euler(leftArmHoldRotation);
        rightArm.localRotation = Quaternion.Euler(rightArmHoldRotation);
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
        ResetArms();
    }

    public void PickUpIngredient(string ingredientName, GameObject prefab)
    {
        heldDish = "";
        heldIngredient = ingredientName;
        heldVisual = Instantiate(prefab, holdPoint.position, Quaternion.identity, holdPoint);
        SetHoldingPose();
    }

    public void PickUpDish(string dishName, GameObject dishObject)
    {
        PlaceIngredient();
        heldDish = dishName;
        heldVisual = dishObject;

        heldVisual.transform.SetParent(holdPoint);
        heldVisual.transform.localPosition = Vector3.zero;
        heldVisual.transform.localRotation = Quaternion.identity;

        if (heldVisual.TryGetComponent<Collider>(out var c)) c.isTrigger = true;
        if (heldVisual.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

        SetHoldingPose();
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

        ResetArms();

        return placed;
    }

    public void PickUpDish(GameObject dish)
    {
        if (heldVisual != null) return;

        heldVisual = dish;
        heldDish = dish.name;

        dish.transform.SetParent(holdPoint);
        dish.transform.localPosition = Vector3.zero;
        dish.transform.localRotation = Quaternion.identity;

        if (dish.TryGetComponent<Collider>(out var c)) c.enabled = false;
        if (dish.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

        Debug.Log("Picked up: " + dish.name);
        SetHoldingPose();
    }

    public bool HasIngredient()
    {
        return !string.IsNullOrEmpty(heldIngredient);
    }

    public void InitializeGame()
    {
        isGameInitialized = true;
        Debug.Log("Game Initialized");
    }
}
