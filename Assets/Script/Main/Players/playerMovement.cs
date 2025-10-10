using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float dashForce = 10f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 1f;
    [SerializeField] Transform cameraTransform;

    private Rigidbody rb;
    private Animator animator;
    private Vector2 moveInput;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = -999f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Gamepad.current != null)
        {
            moveInput = Gamepad.current.leftStick.ReadValue();
        }
        else
        {
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        if (moveInput.magnitude < 0.1f)
            moveInput = Vector2.zero;

        bool dashPressed = false;

        if (Gamepad.current != null)
        {
            dashPressed = Gamepad.current.buttonSouth.wasPressedThisFrame;
        }
        else
        {
            dashPressed = Input.GetKeyDown(KeyCode.LeftShift);
        }

        if (dashPressed && !isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(Dash());
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
            return;

        Vector3 input = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        if (input.magnitude == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            animator.SetBool("IsMoving", false);
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

        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
        animator.SetBool("IsMoving", true);
    }

    private System.Collections.IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        //Dash animation dito okay
        animator.SetTrigger("Dash");

        Vector3 dashDir;
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            dashDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }
        else
        {
            dashDir = transform.forward;
        }

        rb.linearVelocity = dashDir * dashForce;

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }
}
