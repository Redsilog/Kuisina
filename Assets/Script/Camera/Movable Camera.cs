using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MovableCamera : MonoBehaviour
{
    [Header("Movement")]
    public float panSpeed = 20f;          // keyboard movement speed
    public float edgePanThreshold = 10f;  // pixels from screen edge to trigger edge panning
    public bool allowEdgePan = false;

    [Header("Mouse Drag Panning")]
    public bool allowMouseDrag = true;
    public int mouseDragButton = 1; // 0 = left, 1 = right, 2 = middle
    public float mouseDragSpeed = 0.5f; // how fast dragging pans the camera

    [Header("Zoom")]
    public float zoomSpeed = 50f;   // scroll wheel speed
    public float minY = 5f;         // minimum height (for perspective cameras)
    public float maxY = 80f;        // maximum height
    public float minOrthoSize = 2f; // min for orthographic cameras
    public float maxOrthoSize = 40f;// max for orthographic cameras

    [Header("Rotation")]
    public bool allowRotation = true;
    public float rotationSpeed = 100f; // degrees per second

    [Header("Smoothing")]
    public float smoothTime = 0.12f;

    Camera _cam;
    Vector3 _targetPosition;
    Vector3 _velocity;

    Vector3 _lastMousePosition;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _targetPosition = transform.position;
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleEdgePan();
        HandleMouseDrag();
        HandleZoom();
        HandleRotation();

        // Smoothly move to target position
        transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, smoothTime);
    }

    void HandleKeyboardMovement()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down

        if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f)) return;

        // Move relative to camera orientation (XZ plane)
        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 move = (right * h + forward * v) * panSpeed * Time.deltaTime;
        _targetPosition += move;
    }

    void HandleEdgePan()
    {
        if (!allowEdgePan) return;

        Vector3 move = Vector3.zero;
        Vector2 m = Input.mousePosition;

        if (m.x <= edgePanThreshold) move += -transform.right;
        else if (m.x >= Screen.width - edgePanThreshold) move += transform.right;

        if (m.y <= edgePanThreshold) move += -transform.forward;
        else if (m.y >= Screen.height - edgePanThreshold) move += transform.forward;

        if (move != Vector3.zero)
        {
            move.y = 0;
            move.Normalize();
            _targetPosition += move * panSpeed * Time.deltaTime;
        }
    }

    void HandleMouseDrag()
    {
        if (!allowMouseDrag) return;

        if (Input.GetMouseButtonDown(mouseDragButton))
        {
            _lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(mouseDragButton))
        {
            Vector3 delta = Input.mousePosition - _lastMousePosition;
            // Convert pixel delta to world movement based on camera
            // We'll pan along the camera's right and forward (XZ) directions
            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 move = (-right * delta.x + -forward * delta.y) * (mouseDragSpeed * Time.deltaTime);
            _targetPosition += move;

            _lastMousePosition = Input.mousePosition;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        if (_cam.orthographic)
        {
            float size = _cam.orthographicSize - scroll * zoomSpeed * Time.deltaTime;
            _cam.orthographicSize = Mathf.Clamp(size, minOrthoSize, maxOrthoSize);
        }
        else
        {
            // For perspective camera, move camera up/down along its local Y (or move along forward for angled cams)
            // We'll move the target position along camera's forward vector scaled by scroll.
            Vector3 dir = transform.forward;
            Vector3 zoomMove = dir * (scroll * zoomSpeed * Time.deltaTime * -1f);
            _targetPosition += zoomMove;

            // Clamp by Y (height) so camera doesn't go through ground or too high
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, minY, maxY);
        }
    }

    void HandleRotation()
    {
        if (!allowRotation) return;

        float r = 0f;
        if (Input.GetKey(KeyCode.Q)) r = -1f;
        else if (Input.GetKey(KeyCode.E)) r = 1f;

        if (Mathf.Approximately(r, 0f)) return;

        transform.RotateAround(transform.position, Vector3.up, r * rotationSpeed * Time.deltaTime);
        // After rotating, keep _targetPosition aligned with new rotation (no change needed unless you rotate around a point)
    }
}
