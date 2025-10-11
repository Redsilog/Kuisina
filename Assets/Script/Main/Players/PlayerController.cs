using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;

    private Animator animator;
    private Rigidbody rb;
    private Vector2 moveInput;

    private IngredientBox currentBox;
    private PlayerInventory inventory;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f;
        animator = GetComponent<Animator>();
        inventory = GetComponent<PlayerInventory>();
    }

    private void FixedUpdate()
    {
        Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        if (inputDir.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
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
        Debug.Log("Interact button works");
        Debug.Log($"currentBox: {currentBox}, inventory: {inventory}, hasIngredient: {inventory?.HasIngredient()}, hasDish: {inventory?.HasDish()}");
        if (currentBox != null && inventory != null && !inventory.HasIngredient() && !inventory.HasDish())
        {
            inventory.PickUpIngredient(currentBox.ingredientName, currentBox.ingredientPrefab);
            Debug.Log("Picked up ingredient: " + currentBox.ingredientName);
        }
                
        if (inventory.currentFridge != null)
        {
            inventory.currentFridge.TryOpenOrCloseFridge(inventory);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IngredientBox box))
        {
            currentBox = box;
            Debug.Log("Box Detected");
        }   
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IngredientBox box) && box == currentBox)
        {
            currentBox = null;
            Debug.Log("Outside Pickup");
        }    
    }
    
}
