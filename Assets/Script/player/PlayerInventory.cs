using UnityEngine;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    [Header("Movement + Camera")]
    // We'll grab this from your playerMovement script
    private Transform cameraTransform;

    [Header("Held Item State")]
    [Tooltip("Name of held ingredient (empty if none)")]
    public string heldIngredient = "";
    [Tooltip("Name of held dish (empty if none)")]
    public string heldDish = "";
    [Tooltip("Visual GameObject of whatever is held")]
    public GameObject heldVisual;
    [Tooltip("Transform under which held items are parented")]
    public Transform holdPoint;

    [Header("Interaction Prompt UI")]
    [Tooltip("How far you can look to interact")]
    public float interactionDistance = 2f;
    [Tooltip("Root panel GameObject for the prompt")]
    public GameObject interactionUI;
    [Tooltip("TMP text component showing the required key")]
    public TextMeshProUGUI keyText;
    [Tooltip("TMP text component showing the action description")]
    public TextMeshProUGUI descriptionText;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 1) Try to get camera from your playerMovement component
        var pm = GetComponent<playerMovement>();
        if (pm != null && pm.cameraTransform != null)
        {
            cameraTransform = pm.cameraTransform;
        }
        else if (Camera.main != null)
        {
            // 2) Fallback to Camera.main
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("PlayerInventory: No cameraTransform found on playerMovement and no MainCamera in scene.");
        }

        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    void Update()
    {
        HandleMovement();        // optional: if you still want to move here
        HandleInteractionRay();
    }

    private void HandleMovement()
    {
        // If you're using playerMovement for movement, you can remove this entirely.
        // Otherwise, keep your movement code here.
    }

    private void HandleInteractionRay()
    {
        if (cameraTransform == null)
            return; // no camera to cast from

        bool show = false;
        string desc = "";
        KeyCode key = KeyCode.E;

        // cast from center of your cameraTransform
        Ray ray = cameraTransform.GetComponent<Camera>()
                    .ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            var io = hit.collider.GetComponent<Interaction>();
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
            if (keyText != null) keyText.text = key.ToString();
            if (descriptionText != null) descriptionText.text = desc;
        }
    }

    public void PickUpIngredient(string ingredientName, GameObject prefab)
    {
        PlaceIngredient();
        heldDish = "";
        heldIngredient = ingredientName;
        heldVisual = Instantiate(prefab,
                                      holdPoint.position,
                                      Quaternion.identity,
                                      holdPoint);
    }

    public void PlaceIngredient()
    {
        heldIngredient = "";
        if (heldVisual != null)
        {
            Destroy(heldVisual);
            heldVisual = null;
        }
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

    public bool HasIngredient() => !string.IsNullOrEmpty(heldIngredient);
    public bool HasDish() => !string.IsNullOrEmpty(heldDish);
}
