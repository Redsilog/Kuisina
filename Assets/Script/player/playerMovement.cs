using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class playerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 100f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public Transform playerCamera;  // Assign the Camera transform (child of player)
    public float pickupRange = 3f;  // Range to pick up objects
    public KeyCode pickupKey = KeyCode.E;  // Key to pick up the object
    public KeyCode dropKey = KeyCode.R;   // Key to drop the object

    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;

    private GameObject heldObject = null;
    private Rigidbody heldObjectRb = null;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleObjectInteraction();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleObjectInteraction()
    {
        // If not holding an object and the player presses pickup key, try to pick up
        if (heldObject == null && Input.GetKeyDown(pickupKey))
        {
            TryPickup();
        }
        // If holding an object, pressing drop key will drop it
        else if (heldObject != null && Input.GetKeyDown(dropKey))
        {
            DropObject();
        }
    }

    void TryPickup()
    {
        RaycastHit hit;
        // Perform raycast to detect if player is close to an interactable object
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, pickupRange))
        {
            if (hit.collider.CompareTag("Interactable"))
            {
                heldObject = hit.collider.gameObject;
                heldObjectRb = heldObject.GetComponent<Rigidbody>();

                // If the object doesn't have a Rigidbody, add one
                if (heldObjectRb == null)
                {
                    heldObjectRb = heldObject.AddComponent<Rigidbody>();
                }

                // Disable physics and set it to kinematic while holding it
                heldObjectRb.isKinematic = true;

                // Parent the object to the player's camera for holding
                heldObject.transform.SetParent(playerCamera);

                // Position the object in front of the camera
                heldObject.transform.localPosition = new Vector3(0f, 0f, 1f); // Adjust as needed
                heldObject.transform.localRotation = Quaternion.identity;

                Debug.Log("Picked up " + heldObject.name);
            }
        }
    }

    void DropObject()
    {
        if (heldObject != null)
        {
            // Detach the object from the camera and reset its physics
            heldObject.transform.SetParent(null);
            heldObjectRb.isKinematic = false;

            // Apply force to the object to simulate a drop
            heldObjectRb.AddForce(playerCamera.forward * 2f, ForceMode.Impulse);

            heldObject = null;
            heldObjectRb = null;

            Debug.Log("Dropped object.");
        }
    }

    public void RemoveHeldObject(GameObject obj)
    {
        if (heldObject == obj)
        {
            heldObject = null;
            heldObjectRb = null;
            Debug.Log("Held object cleared by cooking station.");
        }
    }
    public GameObject GetHeldObject()
    {
        return heldObject;  // Returns the currently held object (if any)
    }
}
