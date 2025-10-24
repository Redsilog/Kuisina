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

    // 🔒 Interaction lock: blocks movement & walk animation during interactions
    private bool isInteracting = false;

    private Stove currentStove;
    private ChoppingBoard currentBoard;
    private NPCInteractable currentNPC;
    private GameObject currentCookedFood;
    private PlayerInventory inventory;

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
        // 🔒 While interacting, stop motion & keep idle animation
        if (isInteracting)
        {
            rb.linearVelocity = Vector3.zero;
            animator.SetBool("IsMoving", false);
            return;
        }

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
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 10f * Time.fixedDeltaTime);

        animator.SetBool("IsMoving", true);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (inventory == null) return;

        // Talk to NPCs
        if (currentNPC != null)
        {
            LockInteraction();
            currentNPC.OnInteract(ctx);
            UnlockInteractionDelayed(0.5f);
            return;
        }

        // Pick up cooked food
        if (currentCookedFood != null)
        {
            LockInteraction();

            GameObject heldCopy = Instantiate(
                currentCookedFood,
                inventory.holdPoint.position,
                currentCookedFood.transform.rotation,
                inventory.holdPoint
            );

            heldCopy.transform.localPosition = Vector3.zero;
            heldCopy.transform.localRotation = Quaternion.identity;

            if (heldCopy.TryGetComponent<Rigidbody>(out var r)) Destroy(r);
            if (heldCopy.TryGetComponent<Collider>(out var c)) c.enabled = false;

            inventory.PickUpDish(heldCopy);
            Destroy(currentCookedFood);
            currentCookedFood = null;

            UnlockInteractionDelayed(0.3f);
            return;
        }

        // Place ingredient on stove
        if (currentStove != null && inventory.HasIngredient())
        {
            LockInteraction();

            currentStove.PlaceIngredient(inventory.heldIngredient, inventory.heldVisual);
            inventory.ClearHeldItemDirect();

            UnlockInteractionDelayed(0.3f);
            return;
        }

        // Use chopping board
        if (currentBoard != null)
        {
            LockInteraction();
            currentBoard.HandlePlayerInteract(inventory);

            // If chopping begins, ChoppingBoard will keep us locked (SetInteracting(true)).
            // If no long interaction started (e.g., quick pickup/place), auto-unlock shortly.
            UnlockInteractionDelayed(0.1f);
            return;
        }

        // Open/close fridge (FridgeUI already disables PlayerController while open)
        if (inventory.currentFridge != null)
        {
            LockInteraction();
            inventory.currentFridge.TryOpenOrCloseFridge(inventory);
            UnlockInteractionDelayed(0.3f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ChoppingBoard board))
            currentBoard = board;
        else if (other.TryGetComponent(out NPCInteractable npc))
            currentNPC = npc;
        else if (other.TryGetComponent(out Stove stove))
            currentStove = stove;
        else if (other.CompareTag("CookedFood"))
        {
            currentCookedFood = other.gameObject;
            Debug.Log("Cooked food in range");
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

        if (other.gameObject == currentCookedFood)
            currentCookedFood = null;
    }

    // 🔒 Public helpers for other systems (e.g., ChoppingBoard) to control the lock
    public void SetInteracting(bool value)
    {
        isInteracting = value;
        if (value)
        {
            // ensure idle while interacting
            if (rb != null) rb.linearVelocity = Vector3.zero;
            if (animator != null) animator.SetBool("IsMoving", false);
        }
    }

    public void LockInteraction() => SetInteracting(true);
    public void UnlockInteraction() => SetInteracting(false);

    private void UnlockInteractionDelayed(float delay)
    {
        CancelInvoke(nameof(UnlockInteraction));
        Invoke(nameof(UnlockInteraction), delay);
    }
}
