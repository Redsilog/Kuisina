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
    public bool HasDish() => heldVisual != null;

    public void PlaceIngredient()
    {
        string temp = heldIngredient;
        heldIngredient = "";

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

            heldVisual = null;
        }
    }


    public void PickUpIngredient(string ingredientName, GameObject prefab)
    {
        PlaceIngredient();

        heldDish = "";
        heldIngredient = ingredientName;
        heldVisual = Instantiate(prefab, holdPoint.position, Quaternion.identity, holdPoint);

    }

    public void PickUpDish(string dishName, GameObject dishObject)
    {
        PlaceIngredient();

        heldDish = dishName;
        heldVisual = dishObject;

        heldVisual.transform.SetParent(holdPoint);
        heldVisual.transform.localPosition = Vector3.zero;
        heldVisual.transform.localRotation = Quaternion.identity;

        if (heldVisual.TryGetComponent<Collider>(out var c)) c.isTrigger  = true;
        if (heldVisual.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;
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

        return placed;
    }
    
    public void PickUpDish(GameObject dish)
    {
        if (heldVisual != null) return; // Already holding something

        heldVisual = dish;
        heldDish = dish.name; // Optional: or dish ID if you have one

        dish.transform.SetParent(holdPoint); // holdingPoint = transform for hands, etc.
        dish.transform.localPosition = Vector3.zero;
        dish.transform.localRotation = Quaternion.identity;

        if (dish.TryGetComponent<Collider>(out var c)) c.enabled = false;
        if (dish.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;

        Debug.Log("Picked up: " + dish.name);
    }

    public bool HasIngredient()
    {
        return !string.IsNullOrEmpty(heldIngredient);
    }

    // public bool HasDish()
    // {
    //     return !string.IsNullOrEmpty(heldDish);
    // }
}