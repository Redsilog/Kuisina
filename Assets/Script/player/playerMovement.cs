using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
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

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDirection = (transform.forward * v + transform.right * h).normalized;

        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector3 move = inputDirection * currentSpeed;
        rb.linearVelocity= new Vector3(move.x, rb.linearVelocity.y, move.z);
    }
}

