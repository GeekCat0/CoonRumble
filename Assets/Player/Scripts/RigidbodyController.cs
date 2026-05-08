using UnityEngine;

public class RigidbodyController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform orientation;

    private PlayerLocomotionInput playerLocomotionInput;
    private PlayerState playerState;
    private Rigidbody rb;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float groundDrag = 1f;

    [Header("Air Movement Settings")]
    [SerializeField] private float airDrag = 0f;
    [SerializeField] private float jumpForce = 1f;
    [SerializeField] private float airControl = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight = 1f;
    [SerializeField] private float playerWidth = 1f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private bool isGrounded = false;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle = 60f;
    private RaycastHit slopeHit;

    [Header("Camera Settings")] 
    public float lookSenseH = 0.1f;
    public float lookSenseV = 0.1f;
    [SerializeField] private float lookLimitV = 89f;

    private Vector2 cameraRotation = Vector2.zero;
    private Vector2 playerTargetRotation = Vector2.zero;

    private Vector3 moveDirection;

    void Awake()
    {
        playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        playerState = GetComponent<PlayerState>();

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Update()
    {
        PlayerStateHandler();
        HandleDrag();
        if (playerLocomotionInput.JumpPressed && isGrounded)
            Jump();
    }

    void FixedUpdate()
    {
        PlayerMovement();
    }

    private void LateUpdate()
    {
        CameraMovement();
    }
    private void CameraMovement()
    {
        cameraRotation.x += lookSenseH * playerLocomotionInput.LookInput.x;
        cameraRotation.y = Mathf.Clamp(cameraRotation.y - lookSenseV * playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);

        playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * playerLocomotionInput.LookInput.x;
        rb.MoveRotation(Quaternion.Euler(0f, playerTargetRotation.x, 0f));
        playerCamera.transform.rotation = Quaternion.Euler(cameraRotation.y, cameraRotation.x, 0f);
    }
    private void PlayerMovement()
    {
        moveDirection = orientation.forward * playerLocomotionInput.MovementInput.y + orientation.right * playerLocomotionInput.MovementInput.x;

        if ( OnSlope() )
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed, ForceMode.Force);

            if (rb.linearVelocity.y > 0f)
                rb.AddForce(Vector3.down * 10f, ForceMode.Force);
        }

        if (isGrounded) 
            rb.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * airControl, ForceMode.Force);

        rb.useGravity = !OnSlope();
    }
    private bool GroundCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - playerHeight, transform.position.z);

        bool grounded = Physics.CheckSphere(spherePosition, playerHeight, groundLayers, QueryTriggerInteraction.Ignore);

        return grounded;
    }
    private bool OnSlope()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - playerHeight, transform.position.z);

        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
    private void OnDrawGizmosSelected()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - playerHeight / 2, transform.position.z);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spherePosition, playerWidth);
    }
    private void HandleDrag()
    {
        isGrounded = GroundCheck();
        rb.linearDamping = isGrounded ? groundDrag : airDrag;
    }
    private void Jump()
    {
        isGrounded = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void PlayerStateHandler()
    {
        if (isGrounded && rb.linearVelocity.magnitude >= 1)
            playerState.SetPlayerMovementState(PlayerMovementState.Running);
        if (isGrounded && rb.linearVelocity.magnitude < 1)
            playerState.SetPlayerMovementState(PlayerMovementState.Idling);

        if (!isGrounded && rb.linearVelocity.y >= 0f)
            playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
        if (!isGrounded && rb.linearVelocity.y < 0f)
            playerState.SetPlayerMovementState(PlayerMovementState.Falling);
    }
}
