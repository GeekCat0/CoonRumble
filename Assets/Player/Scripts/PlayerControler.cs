using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-1)] // Not mandatory but it's advised for this to run before most other scripts 
public class PlayerControler : MonoBehaviour
{
    [Header("Components")]  // Basic components to drag in the editor
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private LayerMask wallLayers;

    [Header("Movement Settings")] // Variables for on ground movement
    [SerializeField] private float runAcceleration = 0.25f;
    [SerializeField] private float maxRunSpeed = 4f;
    [SerializeField] private float drag = 0.1f;
    [SerializeField] private float airDrag = 0.1f;
    [SerializeField] private float slideDrag = 0.1f;
    [SerializeField] private float movingTreshold = 0.01f;

    [Header("Movement Settings")] // Variables for mid air movement/jumping
    [SerializeField] private float gravity = 25f;
    [SerializeField] private float jumpSpeed = 1.0f;
    [SerializeField] private float inAirAcceleration = 0.15f;
    [SerializeField] private float terminalVelocity = 50f;

    [Header("Dashing Settings")] // Variables for dashing
    [SerializeField] private float dashForce = 3.0f;
    [SerializeField] private float dashDuration = 2.0f;
    [SerializeField] private float dashCooldown = 2.0f;
    [SerializeField] private float maxDashSpeed = 8.0f;

    [Header("WallRun Settings")] // Variables for Wall running
    [SerializeField] private float wallCheckDistance = 1.0f;
    [SerializeField] private float wallRunClimbGravity = 1.0f;
    [SerializeField] private float wallRunFallingGravity = 1.0f;
    [SerializeField] private float forwardJumpForce = 3.0f;
    [SerializeField] private float sideJumpForce = 3.0f;
    [SerializeField] private float upJumpForce = 3.0f;
    [SerializeField] private float wallStartBoost = 1f;
    [SerializeField] private float maxWallRunSpeed = 4f;

    [Header("Camera Settings")] // Variables for camera control
    public float lookSenseH = 0.1f;
    public float lookSenseV = 0.1f;
    [SerializeField] private float lookLimitV = 89f;

    [Header("Crouch Settings")] // Variables for sliding
    [SerializeField] private float slideBoost = 1;
    [SerializeField] private float minSpeedForSlide = 0.5f;
    [SerializeField] private float crouchSpeed = 0.7f;
    [SerializeField] private float crouchingHeight = 0.2f;
    private bool sliding = false;

    // All private variables that we don't use or set outside of this class
    private PlayerLocomotionInput playerLocomotionInput;
    private PlayerState playerState;

    private Vector2 cameraRotation = Vector2.zero;
    private Vector2 playerTargetRotation = Vector2.zero;

    private float verticalVelocity = 0f;
    private float antiBump;
    private bool jumpedLastFrame = false;
    private float stepOffset;
    private float maxMovementSpeed = 4f;
    private bool dashOffCooldown = true;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private RaycastHit lastRunnedWall;
    private bool wallLeft = false;
    public bool wallRight { get; private set; } = false ; 
    private bool startedWallRun = false;

    private PlayerMovementState lastMovementState = PlayerMovementState.Falling;

    private void Awake() // Set variables that need it and lock the cursor
    {
        playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        playerState = GetComponent<PlayerState>();

        maxMovementSpeed = maxRunSpeed;
        antiBump = maxMovementSpeed;
        stepOffset = characterController.stepOffset;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()   // Method names self explanatory xd, call those every frame as they contain all logic
    {
        UpdateMovementState(); 
        HandleVerticalMovement();
        HandleLateralMovement();
    }

    private void UpdateMovementState() 
    {
        lastMovementState = playerState.CurrentPlayerMovementState;

        if (playerState.CurrentPlayerMovementState != PlayerMovementState.WallRunning && playerState.CurrentPlayerMovementState != PlayerMovementState.Grinding)
        {
            // Control Ground State
            bool isMovementInput = playerLocomotionInput.MovementInput != Vector2.zero;
            bool isMovingLaterally = IsMovingLaterally();
            bool isGrounded = IsGrounded();

            PlayerMovementState lateralState = isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;
            lateralState = playerLocomotionInput.SlideHeld && isGrounded ? PlayerMovementState.Sliding : lateralState;

            playerState.SetPlayerMovementState(lateralState);

            // Control Airborn State
            if ((!isGrounded || jumpedLastFrame) && characterController.velocity.y > 0f)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                jumpedLastFrame = false;
                characterController.stepOffset = 0f;        // <- Character controller step handling can be junky mid air so we turn it off until we're grounded again
            }
            else if ((!isGrounded || jumpedLastFrame) && characterController.velocity.y <= 0f)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                jumpedLastFrame = false;
                characterController.stepOffset = 0f;
            }
            else
            {
                characterController.stepOffset = stepOffset;
            }
        }
    }

    private void HandleVerticalMovement()
    {
        bool isGrounded = playerState.InGroundedState();

        // Add gravity, while wall running we add less
        if (playerState.CurrentPlayerMovementState == PlayerMovementState.WallRunning)
            verticalVelocity -= verticalVelocity < 0 ? gravity * wallRunClimbGravity * Time.deltaTime : gravity * wallRunFallingGravity * Time.deltaTime;
        else
            verticalVelocity -= gravity * Time.deltaTime;    

            // Here we add the max speed a player is able to move to the vertical velocity so that they stick to the ground while walking off steep slopes
        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -antiBump;

        // Handle jumping
        if (playerLocomotionInput.JumpPressed && isGrounded)
        {
            Jump(1);
        }

        // If the player has just went mid air this frame then we gotta cancel the anti-bump's effect
        if (playerState.IsStateGroundedState(lastMovementState) && !isGrounded)
        {
            verticalVelocity += antiBump;
        }

        // Make sure the player is not falling faster that their teminal velocity, we don't want them to reach the speed of light even if they're infinitely falling xd
        if (Mathf.Abs(verticalVelocity) > Mathf.Abs(terminalVelocity))
        {
            verticalVelocity = -1f * Mathf.Abs(terminalVelocity);
        }
    }

    private void HandleLateralMovement()
    {
        bool isGrounded = playerState.InGroundedState();

        // Choose how fast the player can accelerate based on if they're on the ground or mid air
        float lateralAcceleration = !isGrounded ? inAirAcceleration : runAcceleration;

        // Calculate velocity and movement direction based on camera 
        Vector3 cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
        Vector3 cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;
        Vector3 movementDirection = cameraRightXZ * playerLocomotionInput.MovementInput.x + cameraForwardXZ * playerLocomotionInput.MovementInput.y;
        Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime;
        movementDelta = HandleWallRunning(movementDelta);

        Vector3 newVelocity = playerLocomotionInput.SlideHeld ? characterController.velocity : characterController.velocity + movementDelta;

        // Handles dashing together with the method 
        if (playerLocomotionInput.DashPressed && dashOffCooldown)
        {
            StartCoroutine(HandleDashing());
            newVelocity = newVelocity + movementDelta * dashForce;
        }

        // Add drag to player
        Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
        transform.localScale = new Vector3(1,1,1);

        if (playerState.CurrentPlayerActionState != PlayerActionState.Dashing)
        {
            if (!isGrounded && playerState.CurrentPlayerMovementState != PlayerMovementState.WallRunning)
            {
                currentDrag = newVelocity.normalized * airDrag * Time.deltaTime;
                maxMovementSpeed = maxDashSpeed;
                sliding = false;
            }
            else if (playerLocomotionInput.SlideHeld)
            {
                if (newVelocity.magnitude < maxRunSpeed * minSpeedForSlide)
                {
                    maxMovementSpeed = maxRunSpeed * crouchSpeed;
                    newVelocity = characterController.velocity + movementDelta;
                }
                else
                {
                    maxMovementSpeed = maxDashSpeed;
                    if (!sliding)
                        newVelocity = newVelocity * slideBoost;
                    sliding = true;
                    playerState.SetPlayerMovementState(PlayerMovementState.Sliding);
                    currentDrag = newVelocity.normalized * slideDrag * Time.deltaTime;
                }
                transform.localScale = new Vector3(0.8f, crouchingHeight, 0.8f);
            }
            else if (playerState.CurrentPlayerMovementState == PlayerMovementState.WallRunning)
            {
                currentDrag = newVelocity.normalized * airDrag * Time.deltaTime;
                maxMovementSpeed = maxWallRunSpeed;
                sliding = false;
            }
            else
            {
                sliding = false;
                maxMovementSpeed = maxRunSpeed;
            }
        }

        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), maxMovementSpeed);
        newVelocity.y += verticalVelocity;
        newVelocity = !isGrounded ? HandleSteepWalls(newVelocity, characterController.slopeLimit) : newVelocity;
        newVelocity = playerLocomotionInput.SlideHeld ? (HandleSteepWalls(newVelocity, 15)) : newVelocity;
        // Moves the character
        characterController.Move(newVelocity * Time.deltaTime);
    }

    IEnumerator HandleDashing() // Mostly state logic and making sure the player is not stopped by their max run speed, time based so it's a Coroutine
    {
        dashOffCooldown = false;
        playerState.SetPlayerActionState(PlayerActionState.Dashing);
        maxMovementSpeed = maxDashSpeed;
        yield return new WaitForSeconds(dashDuration);
        maxMovementSpeed = maxRunSpeed;
        playerState.SetPlayerActionState(PlayerActionState.Idling);
        yield return new WaitForSeconds(dashCooldown);
        dashOffCooldown = true;
    }

    private Vector3 HandleWallRunning(Vector3 movementDelta)
    {
        bool isGrounded = IsGrounded();

        // Check if there is a wall to the left or right of the player
        // If we found a wall then its information is saved in "rightWallHit" or "leftWallHit"
        wallRight = Physics.Raycast(characterController.transform.position, characterController.transform.right, out rightWallHit, wallCheckDistance, wallLayers);
        wallLeft = Physics.Raycast(characterController.transform.position, -characterController.transform.right, out leftWallHit, wallCheckDistance, wallLayers);

        lastRunnedWall = isGrounded ? new RaycastHit() : lastRunnedWall;
        bool canStartWallRun = ((wallRight && rightWallHit.colliderEntityId != lastRunnedWall.colliderEntityId) || (wallLeft && leftWallHit.colliderEntityId != lastRunnedWall.colliderEntityId));

        // Setting wall running state
        if ((wallRight || wallLeft) && playerLocomotionInput.MovementInput.y > 0 && !isGrounded) 
        {
            if ( !startedWallRun && canStartWallRun)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.WallRunning);
                startedWallRun = true;
                if (verticalVelocity < wallStartBoost)
                    verticalVelocity = wallStartBoost;
            }
        }
        else if (startedWallRun)
        {
            playerState.SetPlayerMovementState(PlayerMovementState.Falling);
            startedWallRun = false;
        }

        // Wall runing logic
        if (playerState.CurrentPlayerMovementState == PlayerMovementState.WallRunning)
        {
            lastRunnedWall = wallRight ? rightWallHit : leftWallHit;

            // Checks from wchich side there is a wall and its direction
            Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
            Vector3 wallForwardDirection = Vector3.Cross(wallNormal, transform.up);

            // Choses the direction closest to the one player is facing
            if ((characterController.transform.forward - wallForwardDirection).magnitude > (characterController.transform.forward - -wallForwardDirection).magnitude)
                wallForwardDirection = -wallForwardDirection;

            // Handles wall jumps
            if (playerLocomotionInput.JumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(upJumpForce * 3 * gravity);
                playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                startedWallRun = false;

                movementDelta += wallNormal * Mathf.Sqrt(sideJumpForce * 3 * gravity);
                movementDelta += wallForwardDirection * Mathf.Sqrt(forwardJumpForce * 3 * gravity);
                return movementDelta;
            }
            movementDelta = wallForwardDirection * runAcceleration * Time.deltaTime + wallNormal * -1;
        }
        return movementDelta;
    }

    private void LateUpdate() // We want to control the player camera after movement logic so it's happening here
    {
        cameraRotation.x += lookSenseH * playerLocomotionInput.LookInput.x;
        cameraRotation.y = Mathf.Clamp(cameraRotation.y - lookSenseV * playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);

        playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * playerLocomotionInput.LookInput.x;
        transform.rotation = Quaternion.Euler(0f, playerTargetRotation.x, 0f);
        playerCamera.transform.rotation = Quaternion.Euler(cameraRotation.y, cameraRotation.x, playerCamera.transform.localEulerAngles.z);
    }

    private Vector3 HandleSteepWalls(Vector3 velocity, float slopeLimit) // This is preventing the player from being able to climb too steep walls by jumping by making them slide off
    {
        // Here we check if the angle on which the player is standing is not too steep
        Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
        float angle = Vector3.Angle(normal, Vector3.up);
        bool validAngle = angle <= slopeLimit;

        // If the player just fell on a too steep wall, we change the velocity so that they are falling towards the direction the slope is pointing instead of going straight down
        if (!validAngle && verticalVelocity <= 0f)
            velocity = Vector3.ProjectOnPlane(velocity, normal);

        return velocity;
    }
    private bool IsMovingLaterally() // Checks if the player is moving (mostly for state logic)
    {
        Vector3 lateralVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);

        return lateralVelocity.magnitude > movingTreshold;
    }

    private bool IsGrounded() // General grounded check, for best results we have different logic for checking if the player is still grounded while grounded and if they got on ground after being mid air
    {
        bool grounded = playerState.InGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();

        return grounded;
    }

    private bool IsGroundedWhileGrounded() // It creates a sphere at the characters feet and checks if it's colliding with the ground
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - characterController.radius, transform.position.z);

        bool grounded = Physics.CheckSphere(spherePosition, characterController.radius, groundLayers, QueryTriggerInteraction.Ignore);

        return grounded;
    }

    private bool IsGroundedWhileAirborne() // Same as the previous grounded check but here we also check if we're not falling onto a too steep wall, in that case we continue falling until hitting a valid angle
    {
        Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
        float angle = Vector3.Angle(normal, Vector3.up);
        bool validAngle = angle <= characterController.slopeLimit;

        return characterController.isGrounded && validAngle;
    }
    public void Jump(float force)
    {
        verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity * force);
        jumpedLastFrame = true;
    }
}