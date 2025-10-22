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
    private Stove currentStove;

    private ChoppingBoard currentBoard;
    private PlayerInventory inventory;
    private NPCInteractable currentNPC;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inventory = GetComponent<PlayerInventory>();

        if (rb != null)
            rb.linearDamping = 0f;
    }

    private void FixedUpdate()
    {
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDir.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            animator.SetBool("IsMoving", false);
            return;
        }

        Vector3 moveDir = (cameraTransform.forward * inputDir.z + cameraTransform.right * inputDir.x);
        moveDir.y = 0f;
        moveDir.Normalize();

        rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(moveDir),
            10f * Time.fixedDeltaTime
        );

        animator.SetBool("IsMoving", true);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (currentNPC != null)
        {
            currentNPC.OnInteract(ctx);
            return;
        }

        if (currentStove != null && inventory != null && inventory.HasIngredient())
        {
            currentStove.PlaceIngredient(inventory.heldIngredient, inventory.heldVisual);
            inventory.ClearHeldItemDirect();
        }

        if (currentBoard != null && inventory != null)
        {
            currentBoard.HandlePlayerInteract(inventory);
            return;
        }

        if (inventory != null && inventory.currentFridge != null)
        {
            if (!inventory.currentFridge.IsFridgeUIOpenFor(inventory))
                inventory.currentFridge.TryOpenOrCloseFridge(inventory);
            else
                Debug.Log("UI already open — ignoring Interact input.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ChoppingBoard board))
        {
            currentBoard = board;
            Debug.Log("Board Detected");
        }
        else if (other.TryGetComponent(out NPCInteractable npc))
        {
            currentNPC = npc;
            Debug.Log("NPC Detected");
        }
        else if (other.TryGetComponent(out Stove stove))
        {
            currentStove = stove;
            Debug.Log("Stove detected");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out ChoppingBoard board) && board == currentBoard)
            currentBoard = null;

        if (other.TryGetComponent(out NPCInteractable npc) && npc == currentNPC)
            currentNPC = null;

        if (other.TryGetComponent(out Stove stove) && stove == currentStove)
            currentStove = null;
    }
}
