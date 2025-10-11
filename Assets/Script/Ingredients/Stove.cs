using UnityEngine;
using UnityEngine.InputSystem;

public class Stove : MonoBehaviour
{
    [Header("Setup")]
    public Transform cookPoint;
    public GameObject cookedPrefab;
    public float cookTime = 3f;

    private bool isCooking = false;
    private bool isCooked = false;

    private PlayerInventory currentInventory;
    private GameObject currentVisual;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory inv))
        {
            currentInventory = inv;
            Debug.Log("Player entered stove area");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerInventory inv) && inv == currentInventory)
        {
            currentInventory = null;
            Debug.Log("Player left stove area");
        }
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (currentInventory == null) return;

        Debug.Log($"[Stove] Interact pressed | isCooking: {isCooking}, isCooked: {isCooked}, hasIngredient: {currentInventory.HasIngredient()}");

        if (!isCooking && !isCooked && currentInventory.HasIngredient())
        {
            StartCooking(currentInventory.heldIngredient, currentInventory.heldVisual);
            currentInventory.PlaceIngredient();
        }
        else if (isCooked && !currentInventory.HasIngredient())
        {
            PickUpCookedIngredient();
        }
    }

    private void StartCooking(string ingredientName, GameObject ingredientVisual)
    {
        Debug.Log("[Stove] Started cooking " + ingredientName);
        isCooking = true;

        currentVisual = Instantiate(ingredientVisual, cookPoint.position, Quaternion.identity);
        currentVisual.transform.SetParent(transform);

        Invoke(nameof(FinishCooking), cookTime);
    }

    private void FinishCooking()
    {
        Debug.Log("[Stove] Finished cooking");
        Destroy(currentVisual);
        isCooking = false;
        isCooked = true;

        currentVisual = Instantiate(cookedPrefab, cookPoint.position, Quaternion.identity);
        currentVisual.transform.SetParent(transform);
    }

    private void PickUpCookedIngredient()
    {
        if (currentInventory == null || currentVisual == null) return;

        Debug.Log("[Stove] Picked up cooked ingredient");
        currentInventory.PickUpIngredient(currentVisual.name, currentVisual);
        Destroy(currentVisual);
        isCooked = false;
    }
}
