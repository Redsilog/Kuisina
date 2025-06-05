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
    private IngredientBoxSpawner boxSpawner;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Find the IngredientBoxSpawner script in the scene
        boxSpawner = Object.FindFirstObjectByType<IngredientBoxSpawner>();
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
        if (heldObject == null && Input.GetKeyDown(pickupKey))
        {
            TryPickup();
        }
        else if (heldObject != null && Input.GetKeyDown(dropKey))
        {
            DropObject();
        }
    }

    void TryPickup()
    {
        if (boxSpawner == null)
        {
            Debug.LogWarning("IngredientBoxSpawner not found!");
            return;
        }

        // Get the spawned object from the box
        heldObject = boxSpawner.GetSpawnedObject();

        if (heldObject != null)
        {
            // Parent the object to the player's camera for holding
            heldObject.transform.SetParent(playerCamera);

            // Position the object in front of the camera
            heldObject.transform.localPosition = new Vector3(0f, 0f, 1f); // Adjust if needed
            heldObject.transform.localRotation = Quaternion.identity;

            // Get the Rigidbody component to disable physics while holding it
            heldObjectRb = heldObject.GetComponent<Rigidbody>();

            if (heldObjectRb == null)
            {
                heldObjectRb = heldObject.AddComponent<Rigidbody>(); // Add Rigidbody if it doesn't exist
            }

            heldObjectRb.isKinematic = true;

            Debug.Log("Picked up " + heldObject.name);
        }
    }

    void DropObject()
    {
        if (heldObject != null)
        {
            // Detach the object from the camera and reset its physics
            heldObject.transform.SetParent(null);
            heldObjectRb.isKinematic = false;

            // Optionally, apply a force when dropping
            heldObjectRb.AddForce(playerCamera.forward * 2f, ForceMode.Impulse);

            heldObject = null;
            heldObjectRb = null;

            Debug.Log("Dropped object.");
        }
    }
}
