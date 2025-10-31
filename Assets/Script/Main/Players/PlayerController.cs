using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;

    private Animator animator;
    private Rigidbody rb;
    private Vector2 moveInput;

    // Interaction refs
    private Stove currentStove;
    private ChoppingBoard currentBoard;
    private NPCInteractable currentNPC;
    private GameObject currentCookedFood;
    private PlayerInventory inventory;

    // ===== Movement lock (prevents walking while interacting) =====
    int interactionLocks = 0;          // supports nested locks
    bool CanMove => interactionLocks <= 0;

    void LockMovement()
    {
        interactionLocks++;
        moveInput = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (animator != null) animator.SetBool("IsMoving", false);
    }

    void UnlockMovement()
    {
        interactionLocks = Mathf.Max(0, interactionLocks - 1);
        if (!CanMove && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    // =============================================================

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inventory = GetComponent<PlayerInventory>();

        if (rb != null) rb.linearDamping = 0f;
    }

    private void FixedUpdate()
    {
        if (!CanMove)
        {
            // stay frozen during interactions
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            if (animator) animator.SetBool("IsMoving", false);
            return;
        }

        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDir.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            if (animator) animator.SetBool("IsMoving", false);
            return;
        }

        Vector3 moveDir = (cameraTransform.forward * inputDir.z + cameraTransform.right * inputDir.x);
        moveDir.y = 0f;
        moveDir.Normalize();

        rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 10f * Time.fixedDeltaTime);

        if (animator) animator.SetBool("IsMoving", true);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = CanMove ? ctx.ReadValue<Vector2>() : Vector2.zero;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        // ---------- Press started ----------
        if (ctx.started)
        {
            // Hold-to-clear stove
            if (currentStove != null && !inventory.HasIngredient())
            {
                LockMovement();
                StartCoroutine(HoldToClearStove());
                return;
            }

            // Begin chopping
            if (currentBoard != null && inventory != null)
            {
                LockMovement();
                currentBoard.StartChop(inventory);
                return;
            }
        }

        // ---------- Released (cancel) ----------
        else if (ctx.canceled)
        {
            StopAllCoroutines(); // stop hold if any

            // Pause chopping and unlock
            if (currentBoard != null)
                currentBoard.PauseChop();

            UnlockMovement();
        }

        // ---------- Tap performed ----------
        else if (ctx.performed)
        {
            // Talk to NPC (lock here; NPC should call player.UnlockMovement() when done)
            if (currentNPC != null)
            {
                LockMovement();
                currentNPC.OnInteract(ctx);
                return;
            }

            // Pick up cooked food
            if (currentCookedFood != null && inventory != null)
            {
                Vector3 worldScale = currentCookedFood.transform.lossyScale;

                currentCookedFood.transform.SetParent(inventory.holdPoint, worldPositionStays: false);
                currentCookedFood.transform.localPosition = Vector3.zero;
                currentCookedFood.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                Vector3 parentScale = inventory.holdPoint.lossyScale;
                currentCookedFood.transform.localScale = new Vector3(
                    worldScale.x / parentScale.x,
                    worldScale.y / parentScale.y,
                    worldScale.z / parentScale.z
                );

                if (currentCookedFood.TryGetComponent<Collider>(out var col)) col.enabled = false;
                if (currentCookedFood.TryGetComponent<Rigidbody>(out var crb)) crb.isKinematic = true;

                inventory.PickUpDish(currentCookedFood);

                if (currentStove != null) currentStove.cookedFood = null;

                currentCookedFood = null;
                return;
            }

            // Place ingredient on stove
            if (currentStove != null && inventory != null && inventory.HasIngredient())
            {
                currentStove.PlaceIngredient(inventory.heldIngredient, inventory.heldVisual);
                inventory.ClearHeldItemDirect();
                return;
            }

            // Interact with chopping board (non-hold action)
            if (currentBoard != null && inventory != null)
            {
                currentBoard.HandlePlayerInteract(inventory);
                return;
            }

            // Open/Close fridge UI and lock while open
            if (inventory != null && inventory.currentFridge != null)
            {
                bool willOpen = !inventory.currentFridge.IsFridgeUIOpenFor(inventory);
                inventory.currentFridge.TryOpenOrCloseFridge(inventory);
                if (willOpen) LockMovement();
                else UnlockMovement();
            }
        }
    }

    private IEnumerator HoldToClearStove()
    {
        float holdTime = 1f;
        float elapsed = 0f;

        while (elapsed < holdTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentStove != null && !inventory.HasIngredient())
            currentStove.ClearStove();

        UnlockMovement();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ChoppingBoard board)) currentBoard = board;
        else if (other.TryGetComponent(out NPCInteractable npc)) currentNPC = npc;
        else if (other.TryGetComponent(out Stove stove)) currentStove = stove;
        else if (other.CompareTag("CookedFood"))
        {
            currentCookedFood = other.gameObject;
            Debug.Log("Cooked food in range");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out ChoppingBoard board) && board == currentBoard) currentBoard = null;
        if (other.TryGetComponent(out NPCInteractable npc) && npc == currentNPC) currentNPC = null;
        if (other.TryGetComponent(out Stove stove) && stove == currentStove) currentStove = null;
        if (other.gameObject == currentCookedFood) currentCookedFood = null;
    }

    // === Public hook for external scripts to end interactions ===
    public void EndInteractionFromExternal() => UnlockMovement();
}
