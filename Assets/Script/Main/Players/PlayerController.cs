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
    private NPCInteractable currentNPC;

    // Inventory on this player
    private PlayerInventory inventory;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inventory = GetComponent<PlayerInventory>();

        // Rigidbody uses drag/velocity (not linearDamping/linearVelocity)
        if (rb != null) rb.linearDamping = 0f;
    }

    void FixedUpdate()
    {
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

    // SINGLE-BUTTON FLOW (Interact)
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        // 0) NPC has top priority (talking)
        if (currentNPC != null)
        {
            currentNPC.OnInteract(ctx);
            return;
        }

        // 1) Stations: choose the nearer between Stove and Board
        if (currentStove != null || currentBoard != null)
        {
            if (currentStove != null && currentBoard != null)
            {
                float dStove = (currentStove.transform.position - transform.position).sqrMagnitude;
                float dBoard = (currentBoard.transform.position - transform.position).sqrMagnitude;

                if (dStove <= dBoard) currentStove.OnInteract(ctx);
                else currentBoard.OnInteract(ctx);
                return;
            }

            if (currentStove != null) { currentStove.OnInteract(ctx); return; }
            if (currentBoard != null) { currentBoard.OnInteract(ctx); return; }
        }

        // 2) Nothing to forward to: handle tap-time actions
        if (!ctx.performed) return;

        // IngredientBox pickup (only if hands are empty)
        if (currentBox != null && inventory != null && !inventory.IsHoldingItem())
        {
            inventory.PickUpIngredient(currentBox.ingredientName, currentBox.ingredientPrefab);
            Debug.Log("[Player] Picked up ingredient from box: " + currentBox.ingredientName);
        }

        if (inventory != null && inventory.currentFridge != null)
        {
            if (!inventory.currentFridge.IsFridgeUIOpenFor(inventory))
            {
                inventory.currentFridge.TryOpenOrCloseFridge(inventory);
            }
            else
            {
                Debug.Log("UI already open — ignoring Interact input.");
            }
        }
    }

    // ===== Triggers =====
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IngredientBox box)) { currentBox = box; }
        if (other.TryGetComponent(out ChoppingBoard board)) { currentBoard = board; }
        if (other.TryGetComponent(out Stove stove)) { currentStove = stove; }
        if (other.TryGetComponent(out NPCInteractable npc)) { currentNPC = npc; }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IngredientBox box) && box == currentBox) currentBox = null;
        if (other.TryGetComponent(out ChoppingBoard board) && board == currentBoard) currentBoard = null;
        if (other.TryGetComponent(out Stove stove) && stove == currentStove) currentStove = null;
        if (other.TryGetComponent(out NPCInteractable npc) && npc == currentNPC) currentNPC = null;
    }
}
