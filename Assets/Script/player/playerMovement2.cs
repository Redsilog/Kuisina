using UnityEngine;

public class playerMovement2 : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Transform cameraTransform;

    private Rigidbody rb;
    private Animator animator; // Reference to Animator

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>(); // Get the Animator component
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal_P2");
        float v = Input.GetAxisRaw("Vertical_P2");
        Vector3 input = new Vector3(h, 0, v).normalized;

        if (input.magnitude == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            animator.SetBool("IsMoving", false); // Not moving
            return;
        }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.z + camRight * input.x;
        rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);

        if (moveDir != Vector3.zero)
        {
            // Rotate the player to face the direction of movement
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);

            animator.SetBool("IsMoving", true); // Moving
        }
    }
}
