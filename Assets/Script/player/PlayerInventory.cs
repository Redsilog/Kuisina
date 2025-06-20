using Unity.Mathematics;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public string heldIngredient = "";
    public string heldDish = "";
    public GameObject heldVisual;
    public Transform holdPoint;

    //ingredients
    public void PickUpIngredient(string ingredientName, GameObject prefab)
    {
        heldIngredient = ingredientName;
        heldVisual = Instantiate(prefab, holdPoint.position, Quaternion.identity, holdPoint);
    }

    public void PlaceIngredient()
    {
        heldIngredient = "";
        if (heldVisual != null)
        {
            Destroy(heldVisual);
        }
    }

    public bool HasIngredient()
    {
        return heldIngredient != "";
    }

    //dishes
    public void PickUpDish(string dishName, GameObject dishObject)
    {
        heldDish = dishName;
        heldVisual = dishObject;

        // Move the object to the hold point
        heldVisual.transform.SetParent(holdPoint);
        heldVisual.transform.localPosition = Vector3.zero;
        heldVisual.transform.localRotation = Quaternion.identity;

        // Disable collider to prevent pushing the player
        Collider col = heldVisual.GetComponent<Collider>();
        if (col) col.enabled = false;

        // If it has a Rigidbody, make it kinematic while held
        Rigidbody rb = heldVisual.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    public void PlaceDish(Vector3 position)
    {
        if (heldVisual != null)
        {
            heldVisual.transform.SetParent(null);

            heldVisual.transform.position = position;

            heldVisual.transform.rotation = Quaternion.identity;

            Collider col = heldVisual.GetComponent<Collider>();
            if (col) col.enabled = true;

            Rigidbody rb = heldVisual.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
        }

        heldDish = "";
        heldVisual = null;
    }

    public bool HasDish()
    {
        return heldDish != "";
    }


}
