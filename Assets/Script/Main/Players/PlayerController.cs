using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;

    private Animator animator;
    private Rigidbody rb;
    private Vector2 moveInput;

    // Nearby interactables
    private IngredientBox currentBox;
    private ChoppingBoard currentBoard;
    private Stove currentStove;

    // Inventory on this player
    private PlayerInventory inventory;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inventory = GetComponent<PlayerInventory>();

        // Rigidbody APIs
        if (rb != null) rb.linearDamping = 0f;
    }

    private void FixedUpdate()
    {
        // moveInput.x = left/right, moveInput.y = forward/back
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

        if (inputDir.sqrMagnitude < 0.0001f)
        {
            if (rb != null)
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (animator) animator.SetBool("IsMoving", false);
            return;
        }

        // Camera-relative movement (ignore camera tilt)
        Vector3 fwd = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : Vector3.right;

        Vector3 moveDir = (fwd * inputDir.z + right * inputDir.x).normalized;

        if (rb != null)
            rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);

        if (moveDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 10f * Time.fixedDeltaTime);

        if (animator) animator.SetBool("IsMoving", true);
    }

    // ===== Input System callbacks =====

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    // SINGLE-BUTTON FLOW (Interact) for stations
    // Bind your Interact action to keyboard/gamepad as you like (E / A / Cross / etc.)
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        // Always forward to stations first so they can see started/performed/canceled
        if (currentBoard != null)
        {
            currentBoard.OnInteract(ctx);
            return;
        }

        if (currentStove != null)
        {
            // Your Stove already has OnInteract(InputAction.CallbackContext)
            currentStove.OnInteract(ctx);
            return;
        }

        // If no station in range, only do tap-time actions here (performed)
        if (!ctx.performed) return;

        // IngredientBox pickup (only if hands are empty)
        if (currentBox != null && inventory != null && !inventory.IsHoldingItem())
        {
            inventory.PickUpIngredient(currentBox.ingredientName, currentBox.ingredientPrefab);
            Debug.Log("[Player] Picked up ingredient from box: " + currentBox.ingredientName);
        }

        // Fridge toggle (if you use this)
        if (inventory != null && inventory.currentFridge != null)
        {
            inventory.currentFridge.TryOpenOrCloseFridge(inventory);
        }
    }

    // ===== Triggers to remember nearby things =====

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IngredientBox box))
        {
            currentBox = box;
            Debug.Log("[Player] Box in range");
        }

        if (other.TryGetComponent(out ChoppingBoard board))
        {
            currentBoard = board;
            Debug.Log("[Player] ChoppingBoard in range");
        }

        if (other.TryGetComponent(out Stove stove))
        {
            currentStove = stove;
            Debug.Log("[Player] Stove in range");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IngredientBox box) && box == currentBox)
        {
            currentBox = null;
            Debug.Log("[Player] Left box");
        }

        if (other.TryGetComponent(out ChoppingBoard board) && board == currentBoard)
        {
            currentBoard = null;
            Debug.Log("[Player] Left ChoppingBoard");
        }

        if (other.TryGetComponent(out Stove stove) && stove == currentStove)
        {
            currentStove = null;
            Debug.Log("[Player] Left Stove");
        }
    }
}
