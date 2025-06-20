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

        heldVisual.transform.SetParent(holdPoint);
        heldVisual.transform.localPosition = Vector3.zero;
        heldVisual.transform.localRotation = Quaternion.identity;

        Collider col = heldVisual.GetComponent<Collider>();
        Rigidbody rb = heldVisual.GetComponent<Rigidbody>();

        if (col)
        {
            col.enabled = false;
            Debug.Log("Dish collider disabled.");
        }
        else
        {
            Debug.LogWarning("Dish has no collider.");
        }

        if (rb)
        {
            rb.isKinematic = true;
            Debug.Log("Dish rigidbody set to kinematic.");
        }
        else
        {
            Debug.LogWarning("Dish has no Rigidbody.");
        }
    }

    public void PlaceDish(Vector3 position)
    {
        if (heldVisual != null)
        {
            heldVisual.transform.SetParent(null);
            heldVisual.transform.position = position;

            Collider col = heldVisual.GetComponent<Collider>();
            if (col) col.enabled = true;

            Rigidbody rb = heldVisual.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = false;
        }

        heldDish = "";
        heldVisual = null;
    }

    public bool HasDish()
    {
        return heldDish != "";
    }


}
