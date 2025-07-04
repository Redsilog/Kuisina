using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float currentSpeed;

    public Transform cameraTransform;

    private Rigidbody rb;
    private Vector3 inputDirection;
    private float pitch;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        //body horizontal look
        transform.Rotate(Vector3.up * mouseX);

        //camera vertical look
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Movement
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDirection = (transform.forward * v + transform.right * h).normalized;

        //run
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
    }

    void FixedUpdate()
    {
        Vector3 move = inputDirection * currentSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }
}
