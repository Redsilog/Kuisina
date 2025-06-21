using UnityEngine;  

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float moveSpeed;

    public Transform cameraTransform;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        
        Cursor.lockState = CursorLockMode.Locked;
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 direction = (forward * moveZ + right * moveX).normalized;

        moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }
}