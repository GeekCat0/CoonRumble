using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
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
    [SerializeField] private float slideDrag = 0.1f;
    [SerializeField] private float movingTreshold = 0.01f;
    [SerializeField] private float maxSpeedBonus = 10f;

    [Header("Air Settings")] // Variables for mid air movement/jumping
    [SerializeField] private float gravity = 25f;
    [SerializeField] private float jumpSpeed = 1.0f;
    [SerializeField] private float inAirAcceleration = 0.15f;
    [SerializeField] private float terminalVelocity = 50f;
    [SerializeField] private float jumpLockDelay = 0.1f;

    [Header("Dashing Settings")] // Variables for dashing
    [SerializeField] private float dashForce = 3.0f;
    [SerializeField] private float dashDuration = 2.0f;
    [SerializeField] private float dashCooldown = 2.0f;
    [SerializeField] private float maxDashSpeed = 8.0f;

    [Header("WallRun Settings")] // Variables for Wall running
    [SerializeField] private float wallCheckDistance = 1.0f;
    [SerializeField] private float forwardJumpForce = 3.0f;
    [SerializeField] private float sideJumpForce = 3.0f;
    [SerializeField] private float upJumpForce = 3.0f;
    [SerializeField] private float wallStartBoost = 1f;
    [SerializeField] private float maxWallRunSpeed = 4f;
    [SerializeField] private float wallRunGravityUp = 1f;
    [SerializeField] private float wallRunGravityDown = 1f;

    [Header("Camera Settings")] // Variables for camera control
    public float lookSenseH = 0.1f;
    public float lookSenseV = 0.1f;
    [SerializeField] private float lookLimitV = 89f;

    [Header("Crouch Settings")] // Variables for sliding
    [SerializeField] private float slideBoost = 1;
    [SerializeField] private float minSpeedForSlide = 0.5f;
    [SerializeField] private float crouchSpeed = 0.7f;
    [SerializeField] private float crouchingHeight = 0.2f;
    [SerializeField] private float maxSlideSpeed = 1.0f;
    private bool sliding = false;

    // All private variables that we don't use or set outside of this class
    private PlayerLocomotionInput playerLocomotionInput;
    private PlayerState playerState;

    private Vector2 cameraRotation = Vector2.zero;
    private Vector2 playerTargetRotation = Vector2.zero;

    private float verticalVelocity = 0f;
    private float antiBump;
    private bool jumpedLastFrame = false;
    private bool canJump = false;
    private float jumpTimer = 0f;
    private float stepOffset;
    private float maxMovementSpeed = 4f;
    private bool dashOffCooldown = true;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private RaycastHit lastRunnedWall;
    private bool wallLeft = false;
    public bool wallRight { get; private set; } = false;
    private bool startedWallRun = false;
    private Transform objectToFollow;
    private float speedBonus = 0f;
    Vector3 velocityHolder = Vector3.zero;

    private PlayerMovementState lastMovementState = PlayerMovementState.Falling;


    private bool isGrounded = false;
    private Vector3 bonusForces = Vector3.zero;

    private void Awake() // Set variables that need it and lock the cursor
    {
        playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        playerState = GetComponent<PlayerState>();

        maxMovementSpeed = maxRunSpeed;
        antiBump = maxMovementSpeed;
        stepOffset = characterController.stepOffset;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isGrounded = false;
    }
    private void Update()
    {
        Setter();
        StateMachine();
        switch (playerState.CurrentPlayerMovementState)
        {
            case PlayerMovementState.WallRunning:
                WallRunning();
                break;
            case PlayerMovementState.Sliding:
                Sliding();
                break;
            case PlayerMovementState.Grinding:
                RailGrind();
                break;
            default:
                BasicMovement();
                break;
        }

        if (playerState.CurrentPlayerMovementState == PlayerMovementState.Sliding)
            transform.localScale = new Vector3(0.8f, crouchingHeight, 0.8f);
        else
            transform.localScale = new Vector3(1f, 1f, 1f);
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
        transform.rotation = Quaternion.Euler(0f, playerTargetRotation.x, 0f);
        playerCamera.transform.rotation = Quaternion.Euler(cameraRotation.y, cameraRotation.x, playerCamera.transform.localEulerAngles.z);
    }
    private void Setter()
    {
        isGrounded = IsGrounded();
    }
    private void StateMachine()
    {
        lastMovementState = playerState.CurrentPlayerMovementState;

        if (playerState.CurrentPlayerMovementState == PlayerMovementState.Grinding)
            return;

        // Check if there is a wall to the left or right of the player
        // If we found a wall then its information is saved in "rightWallHit" or "leftWallHit"
        wallRight = Physics.Raycast(characterController.transform.position, characterController.transform.right, out rightWallHit, wallCheckDistance, wallLayers);
        wallLeft = Physics.Raycast(characterController.transform.position, -characterController.transform.right, out leftWallHit, wallCheckDistance, wallLayers);

        lastRunnedWall = isGrounded ? new RaycastHit() : lastRunnedWall;
        bool canStartWallRun = ((wallRight && rightWallHit.colliderEntityId != lastRunnedWall.colliderEntityId) || (wallLeft && leftWallHit.colliderEntityId != lastRunnedWall.colliderEntityId));

        // Setting wall running state
        if ((wallRight || wallLeft) && playerLocomotionInput.MovementInput.y > 0 && !isGrounded)
        {
            if (!startedWallRun && canStartWallRun)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.WallRunning);
                startedWallRun = true;
                lastRunnedWall = wallRight ? rightWallHit : leftWallHit;
                if (verticalVelocity < wallStartBoost)
                    verticalVelocity = wallStartBoost;
            }
            return;
        }
        else if (startedWallRun)
        {
            playerState.SetPlayerMovementState(PlayerMovementState.Falling);
            startedWallRun = false;
        }


        // Control Ground State
        bool isMovementInput = playerLocomotionInput.MovementInput != Vector2.zero;
        bool isMovingLaterally = IsMovingLaterally();

        PlayerMovementState lateralState = isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;
        lateralState = playerLocomotionInput.SlideHeld && isGrounded ? PlayerMovementState.Sliding : lateralState;

        if ( isGrounded ) playerState.SetPlayerMovementState(lateralState);

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

    private void BasicMovement()
    {
        isGrounded = playerState.InGroundedState();
        float lateralAcceleration = runAcceleration;
        maxMovementSpeed = maxRunSpeed;

        if (!isGrounded)
        {
            maxMovementSpeed = maxRunSpeed * 2f;
            lateralAcceleration = inAirAcceleration;
        }

        if (playerState.CurrentPlayerActionState == PlayerActionState.Dashing)
            maxMovementSpeed = maxDashSpeed;

        HandleVerticalMovement();
        verticalVelocity -= gravity * Time.deltaTime;

        // Calculate velocity and movement direction based on camera 
        Vector3 cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
        Vector3 cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;
        Vector3 movementDirection = cameraRightXZ * playerLocomotionInput.MovementInput.x + cameraForwardXZ * playerLocomotionInput.MovementInput.y;
        Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime;


        Vector3 newVelocity = characterController.velocity + movementDelta;

        if (playerLocomotionInput.DashPressed && dashOffCooldown)
        {
            velocityHolder = characterController.velocity;
            StartCoroutine(Dash());
            newVelocity += movementDirection.normalized * dashForce;
            verticalVelocity = verticalVelocity + playerCamera.transform.forward.y * dashForce * 0.1f;
        }

        // Add drag to player
        Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;

        if (movementDirection.magnitude == 0)
            currentDrag = newVelocity.normalized * drag * 2 * Time.deltaTime;

        SpeedBonus();
        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), maxMovementSpeed + speedBonus);
        newVelocity.y += verticalVelocity;
        newVelocity += bonusForces;
        newVelocity = !isGrounded ? HandleSteepWalls(newVelocity, characterController.slopeLimit) : newVelocity;
        // Moves the character
        characterController.Move(newVelocity * Time.deltaTime);

        bonusForces = Vector3.zero;
    }
    public void Jump(float force)
    {
        playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
        verticalVelocity = Mathf.Sqrt(jumpSpeed * 3 * gravity * force);
        jumpedLastFrame = true;
    }
    private void HandleVerticalMovement()
    {
        if (isGrounded)
        {
            canJump = true;
            jumpTimer = 0f;
        }
        else if (jumpTimer >= jumpLockDelay)
            canJump = false;
        else
            jumpTimer += Time.deltaTime;

        // Here we add the max speed a player is able to move to the vertical velocity so that they stick to the ground while walking off steep slopes
        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -antiBump;

        // Handle jumping
        if (playerLocomotionInput.JumpPressed && canJump)
        {
            verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
            jumpedLastFrame = true;
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
    private void WallRunning()
    {
        maxMovementSpeed = maxWallRunSpeed;

        if (playerState.CurrentPlayerActionState == PlayerActionState.Dashing)
            maxMovementSpeed = maxDashSpeed;

        HandleVerticalMovement();

        if ( verticalVelocity >= 0f )
            verticalVelocity -= gravity * wallRunGravityUp * Time.deltaTime;
        else
            verticalVelocity -= gravity * wallRunGravityDown * Time.deltaTime;

        // Choose how fast the player can accelerate based on if they're on the ground or mid air
        float lateralAcceleration = runAcceleration;

        // Calculate velocity and movement direction based on camera 
        Vector3 cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
        Vector3 cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;
        Vector3 movementDirection = cameraRightXZ * playerLocomotionInput.MovementInput.x + cameraForwardXZ * playerLocomotionInput.MovementInput.y;
        Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime;

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

            //// Handles wall jumps
            if (playerLocomotionInput.JumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(upJumpForce * 3 * gravity);
                playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                startedWallRun = false;

                movementDelta += wallNormal * Mathf.Sqrt(sideJumpForce * 3 * gravity);
                movementDelta += wallForwardDirection * Mathf.Sqrt(forwardJumpForce * 3 * gravity);
            }
            else if (lastMovementState != PlayerMovementState.WallRunning)
                movementDelta = wallForwardDirection * runAcceleration * Time.deltaTime + wallNormal * -0.01f; // moves the player along a wall and applies a slight force to also stick to it better (only if it's right after starting to wallrun)
            else
                movementDelta = wallForwardDirection * runAcceleration; 
        }
        Vector3 newVelocity = characterController.velocity + movementDelta;

        if (playerLocomotionInput.DashPressed && dashOffCooldown)
        {
            velocityHolder = characterController.velocity;
            StartCoroutine(Dash());
            newVelocity = newVelocity + movementDelta * dashForce;
        }

        // Add drag to player
        Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;

        if (movementDirection.magnitude == 0)
            currentDrag = newVelocity.normalized * drag * 2 * Time.deltaTime;

        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), maxMovementSpeed + speedBonus);
        newVelocity.y += verticalVelocity;

        // Moves the character
        characterController.Move(newVelocity * Time.deltaTime);
    }
    private void Sliding()
    {
        isGrounded = playerState.InGroundedState();
        HandleVerticalMovement();
        verticalVelocity -= gravity * Time.deltaTime;
        // Choose how fast the player can accelerate based on if they're on the ground or mid air
        float lateralAcceleration = runAcceleration;

        // Calculate velocity and movement direction based on camera 
        Vector3 cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
        Vector3 cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;
        Vector3 movementDirection = cameraRightXZ * playerLocomotionInput.MovementInput.x + cameraForwardXZ * playerLocomotionInput.MovementInput.y;
        Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime;

        Vector3 newVelocity = characterController.velocity + movementDelta;

        // Add drag to player
        Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;

        if (newVelocity.magnitude < maxRunSpeed * minSpeedForSlide)
        {
            maxMovementSpeed = maxRunSpeed * crouchSpeed;
            newVelocity = characterController.velocity + movementDelta;

            if (movementDirection.magnitude == 0)
                currentDrag = newVelocity.normalized * drag * 2 * Time.deltaTime;
        }
        else
        {
            maxMovementSpeed = maxSlideSpeed;
            if (!sliding)
                newVelocity = newVelocity * slideBoost;
            sliding = true;
            currentDrag = newVelocity.normalized * slideDrag * Time.deltaTime;
            newVelocity = characterController.velocity;
        }

        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), maxMovementSpeed + speedBonus);
        newVelocity.y += verticalVelocity;
        if (isGrounded) newVelocity += HandleSteepWalls(newVelocity, 5);

        // Moves the character
        characterController.Move(newVelocity * Time.deltaTime);
    }
    private void RailGrind()
    {
        if (objectToFollow != null)
        {
            characterController.Move(objectToFollow.position - this.transform.position);
        }
    }
    public void SetObjectToFollow(Transform following)
    {
        objectToFollow = following;
    }
    private IEnumerator Dash()
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
    public void StopDash()
    {
        StopCoroutine(Dash());
        playerState.SetPlayerActionState(PlayerActionState.Idling);
        dashOffCooldown = true;
    }
    private void SpeedBonus()
    {
        if (characterController.velocity.magnitude >= (maxRunSpeed * 0.7f + speedBonus) && speedBonus < maxSpeedBonus)
        {
            speedBonus += 0.5f * Time.deltaTime;
        }
        else if (speedBonus > 0)
        {
            speedBonus -= 5 * Time.deltaTime;
        }
        if (speedBonus < 0)
            speedBonus = 0;
        if (speedBonus >= maxSpeedBonus)
            speedBonus = maxSpeedBonus;
    }
    public void GiveForceToPlayer(Vector3 force)
    {
        bonusForces = force; 
    }
    private Vector3 HandleSteepWalls(Vector3 velocity, float slopeLimit) // This is preventing the player from being able to climb too steep walls by jumping by making them slide off
    {
        // Here we check if the angle on which the player is standing is not too steep
        Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
        float angle = Vector3.Angle(normal, Vector3.up);

        // if normal check does not detect anything
        if (TryGetSteepWallNormal(slopeLimit, out Vector3 wallNormal))
        {
            normal = wallNormal;
            angle = Vector3.Angle(normal, Vector3.up);
        }

        bool validAngle = angle <= slopeLimit;

        // If the player just fell on a too steep wall, we change the velocity so that they are falling towards the direction the slope is pointing instead of going straight down
        if (!validAngle && verticalVelocity <= 0f && playerState.CurrentPlayerMovementState != PlayerMovementState.Sliding)
        {
            velocity = Vector3.ProjectOnPlane(velocity, normal);

            // Add a small outward force to prevent the CharacterController from sticking.
            Vector3 outwardDirection = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
            if (outwardDirection.sqrMagnitude > 0f)
            {
                float steepness = Mathf.InverseLerp(slopeLimit, 90f, angle);
                velocity += outwardDirection * gravity * Mathf.Lerp(0.5f, 1f, steepness) * Time.deltaTime;
            }
        }

        if (playerState.CurrentPlayerMovementState == PlayerMovementState.Sliding)
        {
            if (!validAngle)
                velocity = new Vector3(normal.x, -gravity, normal.z) * angle * Time.deltaTime * 2;
            else
                velocity = Vector3.zero;
        }
        return velocity;
    }

    private bool TryGetSteepWallNormal(float slopeLimit, out Vector3 normal)
    {
        normal = Vector3.zero;
        float steepestAngle = slopeLimit;

        if (wallRight)
        {
            float angle = Vector3.Angle(rightWallHit.normal, Vector3.up);
            if (angle > steepestAngle)
            {
                steepestAngle = angle;
                normal = rightWallHit.normal;
            }
        }

        if (wallLeft)
        {
            float angle = Vector3.Angle(leftWallHit.normal, Vector3.up);
            if (angle > steepestAngle)
            {
                steepestAngle = angle;
                normal = leftWallHit.normal;
            }
        }

        return normal.sqrMagnitude > 0f;
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

        if (TryGetSteepWallNormal(characterController.slopeLimit, out _))
            validAngle = false;

        return characterController.isGrounded && validAngle;
    }
    public Vector3 GetVelocity()
    {
        if (playerState.CurrentPlayerActionState == PlayerActionState.Dashing)
        {
            return velocityHolder;
        }
        return characterController.velocity;
    }
}
