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

    private List<Stove> nearbyStoves = new List<Stove>();

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
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 10f * Time.fixedDeltaTime);

        animator.SetBool("IsMoving", true);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        // --- Hold down starts the timer for stove clearing ---
        if (ctx.started)
        {
            if (currentStove != null && !inventory.HasIngredient())
            {
                StartCoroutine(HoldToClearStove());
                return;
            }

            if (currentBoard != null && inventory != null)
                currentBoard.StartChop(inventory);
        }

        // --- Released ---
        else if (ctx.canceled)
        {
            StopAllCoroutines(); // cancel hold if released early

            if (currentBoard != null)
                currentBoard.PauseChop();
        }

        // --- Single tap (performed) ---
        else if (ctx.performed)
        {
            if (currentNPC != null)
            {
                currentNPC.OnInteract(ctx);
                return;
            }

            if (currentCookedFood != null && inventory != null)
            {
                Vector3 worldPos = currentCookedFood.transform.position;
                Quaternion worldRot = currentCookedFood.transform.rotation;
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
                if (currentCookedFood.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

                inventory.PickUpDish(currentCookedFood);

                if (currentStove != null)
                    currentStove.cookedFood = null;

                currentCookedFood = null;
                return;
            }

            if (currentStove != null && inventory != null && inventory.HasIngredient())
            {
                currentStove.PlaceIngredient(inventory.heldIngredient, inventory.heldVisual);
                inventory.ClearHeldItemDirect();
                return;
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
    }

    private IEnumerator HoldToClearStove()
    {
        float holdTime = 1f; // how long player must hold to clear
        float elapsed = 0f;

        while (elapsed < holdTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentStove != null && !inventory.HasIngredient())
        {
            currentStove.ClearStove();
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ChoppingBoard board))
        {
            currentBoard = board;
            ToggleHighlight(board.gameObject, true);
        }

        else if (other.TryGetComponent(out NPCInteractable npc))
            currentNPC = npc;

        if (other.TryGetComponent(out Stove stove))
        {
            if (!nearbyStoves.Contains(stove))
                nearbyStoves.Add(stove);

            // highlight the stove you just entered
            ToggleHighlight(stove.gameObject, true);

            // set currentStove to the closest available
            currentStove = GetClosestStove();
            return;
        }

        if (other.CompareTag("CookedFood"))
        {
            currentCookedFood = other.gameObject;
            ToggleHighlight(currentCookedFood, true);
            Debug.Log("Cooked food in range");
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
            // remove stove from nearby list
            nearbyStoves.Remove(stove);

            // turn off highlight for that stove
            ToggleHighlight(stove.gameObject, false);

            // update currentStove to the closest stove still nearby (or null)
            currentStove = GetClosestStove();
            return;
        }

        if (other.gameObject == currentCookedFood)
        {
            ToggleHighlight(other.gameObject, false);
            currentCookedFood = null;
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
        if (h != null) return h;

        return null;
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
