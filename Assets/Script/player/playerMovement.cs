using UnityEngine;

public class playerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float runSpeed = 10f;
    float moveSpeed;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 100f;
    float xRotation = 0f;

    [Header("References")]
    public Transform cameraTransform; // drag your camera here or leave null to auto-assign

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // initialize pitch so we don’t snap
        xRotation = cameraTransform.localEulerAngles.x;

        // lock & hide
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // ----- MOUSE LOOK -----
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            // yaw player
            transform.Rotate(Vector3.up * mouseX, Space.Self);

            // pitch camera
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        // ----- CURSOR TOGGLE -----
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void FixedUpdate()
    {
        // ----- MOVEMENT -----
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // now that the player’s rotated, transform.forward is your forward
        Vector3 direction = (transform.forward * moveZ + transform.right * moveX).normalized;

        moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }
}
