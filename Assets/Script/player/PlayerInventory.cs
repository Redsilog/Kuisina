using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory State")]
    [Tooltip("Name of the held ingredient (empty if none)")]
    public string heldIngredient = "";
    [Tooltip("Name of the held dish (empty if none)")]
    public string heldDish = "";
    [Tooltip("Visual GameObject of whatever is held")]
    public GameObject heldVisual;
    [Tooltip("Transform under which held items are parented")]
    public Transform holdPoint;

    public void PlaceIngredient()
    {
        heldIngredient = "";
        if (heldVisual != null)
        {
            Destroy(heldVisual);
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

        if (heldVisual.TryGetComponent<Collider>(out var c)) c.enabled = false;
        if (heldVisual.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = true;
    }

    public void PlaceDish(Vector3 position)
    {
        if (heldVisual == null) return;

        heldVisual.transform.SetParent(null);
        heldVisual.transform.position = position;
        heldVisual.transform.rotation = Quaternion.identity;

        if (heldVisual.TryGetComponent<Collider>(out var c)) c.enabled = true;
        if (heldVisual.TryGetComponent<Rigidbody>(out var r)) r.isKinematic = false;

        heldDish = "";
        heldVisual = null;
    }

    public bool HasIngredient()
    {
        return !string.IsNullOrEmpty(heldIngredient);
    }

    public bool HasDish()
    {
        return !string.IsNullOrEmpty(heldDish);
    }
}