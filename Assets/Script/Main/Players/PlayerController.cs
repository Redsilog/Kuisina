using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

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
    private NPCInteractable currentNPC;
    private GameObject currentCookedFood;
    private PlayerInventory inventory;
    private IngredientBox currentIngredientBox;
    
    private InGameRecipeBook currentRecipeBook;

    private List<Stove> nearbyStoves = new List<Stove>();

    // Interaction lock
    private bool isInteracting = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inventory = GetComponent<PlayerInventory>();

        if (rb != null)
            rb.linearDamping = 0f;
    }

    private void Update()
    {
        // Auto-unlock when fridge UI is closed
        if (isInteracting && inventory != null)
        {
            var fridge = inventory.currentFridge;
            bool fridgeOpen = fridge != null && fridge.IsFridgeUIOpenFor(inventory);
        }
    }

    private void FixedUpdate()
    {
        // Hard stop while interacting
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
        // Ignore movement updates while interacting (fridge open, chopping, etc.)
        if (isInteracting) return;
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            BeginInteraction();

            // --- HOLD interactions (e.g., chopping) ---
            if (currentBoard != null && inventory != null && !inventory.IsHoldingItem())
            {
                currentBoard.StartChop(inventory);
                return;
            }
        }
        else if (ctx.canceled)
        {
            // Release for hold interactions
            StopAllCoroutines();

            if (currentBoard != null)
                currentBoard.PauseChop();

            EndInteraction();
        }
        else if (ctx.performed)
        {
            // --- TAP interactions ---

            // 1) Serve NPC if holding cooked food
            if (currentNPC != null && currentCookedFood != null && inventory != null)
            {
                var npcOrder = currentNPC.npcOrder;
                if (npcOrder != null)
                {
                    npcOrder.DeliveredDish = currentCookedFood;
                    bool fulfilled = npcOrder.StartOrTryFulfill(inventory);
                    Debug.Log(fulfilled ? "Order fulfilled!" : "Dish doesn't match NPC order.");
                }
                currentCookedFood = null;
                EndInteraction();
                return;
            }

            // 2) NPC regular interaction
            if (currentNPC != null)
            {
                currentNPC.OnInteract(ctx);
                EndInteraction();
                return;
            }

            // 3) Pick up cooked food
            if (currentCookedFood != null && inventory != null)
            {
                Vector3 worldScale = currentCookedFood.transform.lossyScale;

                currentCookedFood.transform.SetParent(inventory.holdPoint, false);
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
                EndInteraction();
                return;
            }

            // 4) Stove interaction
            if (currentStove != null && inventory != null && inventory.HasIngredient())
            {
                if (inventory.heldIngredient == "trash bag")
                {
                    currentStove.ClearStove();
                }
                else if (inventory.heldIngredient != "rice")
                {
                    currentStove.PlaceIngredient(inventory.heldIngredient, inventory.heldVisual);
                    inventory.ClearHeldItemDirect();
                }
                EndInteraction();
                return;
            }

            // 5) Chopping board (tap)
            if (currentBoard != null && inventory != null)
            {
                currentBoard.HandlePlayerInteract(inventory);
                EndInteraction();
                return;
            }

            // 6) Fridge toggle
            if (inventory != null && inventory.currentFridge != null)
            {
                var fridge = inventory.currentFridge;

                // Open if closed; close if open. Movement remains locked while open.
                if (!fridge.IsFridgeUIOpenFor(inventory))
                {
                    fridge.TryOpenOrCloseFridge(inventory); // opens
                    // keep isInteracting true; Update() will auto-unlock when it closes
                }
                else
                {
                    fridge.TryOpenOrCloseFridge(inventory); // closes
                    EndInteraction();
                }
                return;
            }

            // 7) Ingredient box pickup
            if (currentIngredientBox != null && inventory != null && !inventory.IsHoldingItem())
            {
                inventory.PickUpIngredient(currentIngredientBox.ingredientName, currentIngredientBox.ingredientPrefab);
                ToggleHighlight(currentIngredientBox.gameObject, false);
                currentIngredientBox = null;
                EndInteraction();
                return;
            }

            if (currentRecipeBook != null)
            {
                currentRecipeBook.ToggleBookExternally();
                return;
            }

            // Nothing to do -> unlock
            EndInteraction();
        }
    }

    private void BeginInteraction()
    {
        isInteracting = true;

        // Hard reset movement immediately (fixes "IsMoving" stuck when holding W)
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector3.zero;
        animator.SetBool("IsMoving", false);
    }

    private void EndInteraction()
    {
        isInteracting = false;
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ChoppingBoard board))
        {
            currentBoard = board;
            ToggleHighlight(board.gameObject, true);
        }
        else if (other.TryGetComponent(out NPCInteractable npc))
        {
            currentNPC = npc;
        }

        if (other.TryGetComponent(out Stove stove))
        {
            if (!nearbyStoves.Contains(stove))
                nearbyStoves.Add(stove);

            ToggleHighlight(stove.gameObject, true);
            currentStove = GetClosestStove();
            return;
        }

        if (other.CompareTag("CookedFood"))
        {
            currentCookedFood = other.gameObject;
            ToggleHighlight(currentCookedFood, true);
            Debug.Log("Cooked food in range");
        }

        if (other.TryGetComponent<IngredientBox>(out var box))
        {
            currentIngredientBox = box;
            ToggleHighlight(box.gameObject, true);
            Debug.Log("Ingredient box in range");
        }
        if (other.TryGetComponent(out InGameRecipeBook book))
        {
            currentRecipeBook = book;
            ToggleHighlight(book.gameObject, true);
            Debug.Log("📖 Player near recipe book");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out ChoppingBoard board) && board == currentBoard)
        {
            ToggleHighlight(board.gameObject, false);
            currentBoard = null;
            return;
        }

        if (other.TryGetComponent(out NPCInteractable npc) && npc == currentNPC)
        {
            currentNPC = null;
            return;
        }

        if (other.TryGetComponent(out Stove stove))
        {
            nearbyStoves.Remove(stove);
            ToggleHighlight(stove.gameObject, false);
            currentStove = GetClosestStove();
            return;
        }

        if (other.gameObject == currentCookedFood)
        {
            ToggleHighlight(other.gameObject, false);
            currentCookedFood = null;
        }

        if (other.TryGetComponent<IngredientBox>(out var box) && currentIngredientBox == box)
        {
            currentIngredientBox = null;
            ToggleHighlight(box.gameObject, false);
        }
                if (other.TryGetComponent(out InGameRecipeBook book) && book == currentRecipeBook)
        {
            ToggleHighlight(book.gameObject, false);

            if (book != null)
            {
                var bookField = book.GetType().GetField("isBookOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bool isOpen = (bool)bookField.GetValue(book);
                if (isOpen)
                {
                    book.ToggleBookExternally();
                }
            }

            currentRecipeBook = null;
            Debug.Log("📕 Player left recipe book (auto closed)");
        }
    }

    private void ToggleHighlight(GameObject obj, bool state)
    {
        if (obj == null) return;

        var h = GetHighlighterForObject(obj);
        if (h != null)
            h.SetHighlight(state, gameObject.tag);
    }

    private OutlineHighlighter GetHighlighterForObject(GameObject obj)
    {
        if (obj == null) return null;

        var h = obj.GetComponent<OutlineHighlighter>();
        if (h != null) return h;

        Transform t = obj.transform;
        while (t.parent != null)
        {
            t = t.parent;
            h = t.GetComponent<OutlineHighlighter>();
            if (h != null) return h;
        }

        h = obj.GetComponentInChildren<OutlineHighlighter>();
        return h;
    }

    private Stove GetClosestStove()
    {
        if (nearbyStoves.Count == 0) return null;

        Stove closest = null;
        float minDist = float.MaxValue;

        for (int i = nearbyStoves.Count - 1; i >= 0; --i)
        {
            var s = nearbyStoves[i];
            if (s == null)
            {
                nearbyStoves.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(transform.position, s.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = s;
            }
        }

        return closest;
    }
}
