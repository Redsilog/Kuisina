using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public string heldIngredient = "";
    public GameObject heldVisual;

    public Transform holdPoint;

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
}
