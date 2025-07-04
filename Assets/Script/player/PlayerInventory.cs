using UnityEngine;
using TMPro;

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

    [Header("Interaction UI")]
    [Tooltip("Maximum distance for interaction raycasts")]
    public float interactionDistance = 2f;
    [Tooltip("Panel root for the interaction prompt UI")]
    public GameObject interactionUI;
    [Tooltip("TMP text component showing the interaction key")]
    public TextMeshProUGUI keyText;
    [Tooltip("TMP text component showing the action description")]
    public TextMeshProUGUI descriptionText;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    void Update()
    {
        HandleInteractionRay();
    }

    private void HandleInteractionRay()
    {
        bool show = false;
        string desc = "";
        KeyCode key = KeyCode.E;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Interaction io = hit.collider.GetComponent<Interaction>();
            if (io != null)
            {
                desc = io.GetDescription();
                key = io.GetKey();
                if (!string.IsNullOrEmpty(desc))
                {
                    show = true;
                    if (Input.GetKeyDown(key))
                        io.Interact();
                }
            }
        }

        if (interactionUI != null)
            interactionUI.SetActive(show);

        if (show)
        {
            if (keyText != null)
                keyText.text = key.ToString();
            if (descriptionText != null)
                descriptionText.text = desc;
        }
    }

    /// <summary>
    /// Clears and destroys any held ingredient.
    /// </summary>
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
        // drop old ingredient if any
        PlaceIngredient();

        heldDish = "";
        heldIngredient = ingredientName;
        heldVisual = Instantiate(prefab, holdPoint.position, Quaternion.identity, holdPoint);
    }

    public void PickUpDish(string dishName, GameObject dishObject)
    {
        // drop old ingredient if any
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
