using Unity.Netcode;
using UnityEngine;

public class HandleMovement : NetworkBehaviour
{
    [Header("References")]
    private WeaponHandler weaponHandler;
    [SerializeField] private GameObject wishDirLinePrefab;
    [SerializeField] private GameObject velocityLinePrefab;
    [SerializeField] private GameObject rotationLinePrefab;
    [SerializeField] private Transform lowerBody;
    [SerializeField] private Camera mainCamera;

    private CharacterController characterController;
    private PlayerNetvars playerNetvars;
    private InputHandler inputHandler;
    private InputHandler.InputCommand currentInputCommand;

    private const float TICK_INTERVAL = NetworkTimer.TickInterval; // Fixed timestep value

    // Movement state
    public Vector3 wishDir;
    public Vector3 currentVelocity;
    private Vector3 horizontalVelocity;
    private float currentHorizontalSpeed;

    // Ground movement
    public bool WantsToMove = false;
    private bool IsSprinting = false;
    public bool IsOnGround = false;
    public bool IsJumping = false;
    private bool wasJumpHeldLastTick = false;

    // Double Jump state
    private bool hasDoubleJumped = false;
    private float doubleJumpCooldown = 0f;
    [SerializeField] private float doubleJumpForce = 15f;

    // Dashing
    public bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection;
    private float dashCooldownTimer = 0f;

    // Wall Jump state
    private bool isWallSliding = false;
    private bool canWallJump = false;
    private Vector3 wallNormal;
    [SerializeField] private float wallGrabDeceleration = 8f; // How quickly the mech slows down when grabbing wall
    [SerializeField] private float defaultWallJumpBoost = 10f; // Initial boost when jumping off wall

    // Wall jump camera effects
    public float cameraRotateZAngle;
    public bool rotateCameraZ;

    [Header("Wall Jump Settings")]
    [SerializeField] private float wallGrabMomentumDelay = 0.15f; // Time before deceleration starts after grabbing wall
    [SerializeField] private float wallJumpHorizontalBoost = 5f; // Additional horizontal boost when jumping off wall
    [SerializeField] private float wallJumpVerticalBoost = 2f; // Additional vertical boost when jumping off wall
    // Add this to the movement state variables
    private float wallGrabTimer = 0f;
    private bool isWallGrabDelayActive = false;
    public bool recentlyJumpedOffWall = false;
    private bool jumpJustPressed;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerNetvars = GetComponent<PlayerNetvars>();
        inputHandler = GetComponent<InputHandler>();
        weaponHandler = GetComponent<WeaponHandler>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
    }

    public void ResetMovementState()
    {
        // Reset movement-related variables
        wishDir = Vector3.zero;
        currentVelocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;
        currentHorizontalSpeed = 0f;

        // Reset state flags
        WantsToMove = false;
        IsSprinting = false;
        IsJumping = false;
        wasJumpHeldLastTick = false;

        // Reset double jump state
        hasDoubleJumped = false;
        doubleJumpCooldown = 0f;

        // Reset dash state
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;

        // Reset wall-related state
        isWallSliding = false;
        canWallJump = false;
        wallNormal = Vector3.zero;
        wallGrabTimer = 0f;
        isWallGrabDelayActive = false;
        rotateCameraZ = false;

        // Reset camera effects
        cameraRotateZAngle = 0f;

        // Make sure character controller is enabled
        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }
    


    public void RunExplosionForce(Vector3 explosionDirection, float explosionMagnitude)
    {
        // Preserve existing horizontal velocity (additive)
        Vector3 currentHorizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        // Apply force (scaled by time to account for tick rate)
        Vector3 impulse = explosionDirection * explosionMagnitude * TICK_INTERVAL;

        // Combine forces
        currentVelocity = new Vector3(
            currentHorizontalVel.x + impulse.x,
            currentVelocity.y + impulse.y,
            currentHorizontalVel.z + impulse.z
        );

        // Cancel other movement states
        isWallSliding = false;
        isDashing = false;
    }

    public void ApplyMovement(InputHandler.InputCommand inputCommand)
    {
        currentInputCommand = inputCommand;
        wishDir = (-transform.right * inputCommand.horizontalInput.x - transform.forward * inputCommand.horizontalInput.z).normalized;
        WantsToMove = wishDir.magnitude > 0.1f;
        IsSprinting = inputCommand.sprint;

        IsOnGround = characterController.isGrounded;
        horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        currentHorizontalSpeed = horizontalVelocity.magnitude;

        bool jumpHeld = currentInputCommand.jump;

        jumpJustPressed = jumpHeld && !wasJumpHeldLastTick;


        // Reset double jump state if on ground or not sliding on wall
        if (IsOnGround || recentlyJumpedOffWall)
        {
            hasDoubleJumped = false;
            doubleJumpCooldown = 0f;
            recentlyJumpedOffWall = false;
        }
        else if (doubleJumpCooldown > 0f)
        {
            doubleJumpCooldown = Mathf.Max(0f, doubleJumpCooldown - TICK_INTERVAL);
        }


        // Reset wall states if grounded
        if (IsOnGround)
        {
            isWallSliding = false;
            canWallJump = false;
            rotateCameraZ = false;
            wallGrabTimer = 0f;
            isWallGrabDelayActive = false;
        }

        bool dashHeld = inputCommand.dash; // DASH KEY (C)
        bool canDash = dashHeld && WantsToMove && !isDashing && dashCooldownTimer <= 0f;

        // DASH logic (press C while moving)
        if (canDash)
        {
            Dash(wishDir);
        }

        if (jumpJustPressed)
        {
            if (isWallSliding && canWallJump && !IsOnGround)
            {
                WallJump();
            }
            else if (IsOnGround)
            {
                IsJumping = true;
            }
        }

        // Update for next frame
        wasJumpHeldLastTick = jumpHeld;



        if (IsOnGround)
        {
            RunGroundLogic();
        }
        else if (isWallSliding)
        {
            RunWallSlideLogic();
        }
        else
        {
            RunAirborneLogic();
        }

        characterController.Move(currentVelocity * TICK_INTERVAL);

        if (!isWallSliding && !canWallJump)
        {
            // Velocity collision check
            Vector3 actualDisplacement = transform.position - previousPosition;
            Vector3 expectedDisplacement = currentVelocity * TICK_INTERVAL;

            // Compare direction and kill blocked axis
            Vector3 blocked = expectedDisplacement - actualDisplacement;

            if (Mathf.Abs(blocked.x) > 0.01f) currentVelocity.x = 0;
            if (Mathf.Abs(blocked.y) > 0.01f) currentVelocity.y = 0;
            if (Mathf.Abs(blocked.z) > 0.01f) currentVelocity.z = 0;
        }
    }

    // private void ActivateThrust()
    // {
    //     if (!IsOnGround && !IsJumping && !isThrusting && thrustCooldownTimer <= 0f)
    //     {
    //         isThrusting = true;
    //         thrustTimer = 0f;
            
    //         // Preserve horizontal velocity while adding vertical boost
    //         currentVelocity.y = thrustForce;
            
    //         // Optional: Add a small forward boost if moving
    //         // if (WantsToMove)
    //         // {
    //         //     Vector3 forwardBoost = wishDir.normalized * thrustForce * 0.3f;
    //         //     currentVelocity.x += forwardBoost.x;
    //         //     currentVelocity.z += forwardBoost.z;
    //         // }
    //     }
    // }


    // private void UpdateThrustState()
    // {
    //     // Update cooldown timer
    //     if (thrustCooldownTimer > 0f)
    //     {
    //         thrustCooldownTimer -= TICK_INTERVAL;
    //     }

    //     if (!isThrusting) return;

    //     thrustTimer += TICK_INTERVAL;

    //     // Apply continuous thrust force during the duration
    //     if (thrustTimer < thrustDuration)
    //     {
    //         currentVelocity.y += thrustForce * 0.1f * TICK_INTERVAL;
    //     }
    //     else
    //     {
    //         // End thrust
    //         isThrusting = false;
    //         thrustCooldownTimer = thrustCooldown;
    //     }
    // }


    private void RunWallSlideLogic()
    {
        // rotate camera towards wall
        rotateCameraZ = true;



        // Handle wall grab delay
        if (!isWallGrabDelayActive)
        {
            wallGrabTimer += TICK_INTERVAL;

            if (wallGrabTimer >= wallGrabMomentumDelay)
            {
                isWallGrabDelayActive = true;
                wallGrabTimer = 0f;
            }
            else
            {
                // During delay period, maintain velocity (no deceleration)
                canWallJump = true;
                return;
            }
        }

        // Reduce momentum gently (simulate sliding drag) after momentum window is gone
        currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, wallGrabDeceleration * TICK_INTERVAL);

        if (currentVelocity.magnitude <= 0f)
        {
            currentVelocity = Vector3.zero;
            isWallSliding = false;
            rotateCameraZ = false;
        }

        canWallJump = true; // can jump off the wall if player doesn't wanna fully stop and hang on the wall
    }

    private void WallJump()
    {
        // Reset wall grab timers
        recentlyJumpedOffWall = true;
        wallGrabTimer = 0f;
        isWallGrabDelayActive = false;


        // Get current horizontal velocity and speed
        Vector3 currentHorizontalVel = horizontalVelocity;
        
        // Jump direction logic
        Vector3 jumpDir;
        float jumpSpeed = defaultWallJumpBoost;

        if (WantsToMove)
        {
            // Case 1: Player has input direction (wishdir)
            float angleToWall = Vector3.Angle(wishDir, -wallNormal);

            if (angleToWall < 90f)
            {
                // Case 1a: Trying to jump into wall - just use the reflected velocity to never halt the momentum
                jumpDir = Vector3.Reflect(currentHorizontalVel.normalized, wallNormal).normalized;

                jumpSpeed = Mathf.Max(jumpSpeed, currentHorizontalSpeed * 1.4f);
            }
            else
            {
                // Case 1b: Trying to jump away from wall - use wishdir
                jumpDir = wishDir;

                // Boost speed based on current velocity
                jumpSpeed = Mathf.Max(jumpSpeed, currentHorizontalSpeed * 1.4f);
            }
        }
        else if (currentHorizontalSpeed > playerNetvars.maxWalkSpeed.Value)
        {
            // Case 2: No input but has momentum - bounce off wall while preserving speed based on reflected velocity
            jumpDir = Vector3.Reflect(currentHorizontalVel.normalized, wallNormal).normalized;

            jumpSpeed = Mathf.Max(jumpSpeed, currentHorizontalSpeed * 1.4f);
        }
        else
        {
            // Case 3: Default push off straight from wall
            jumpDir = wallNormal;
        }

        // Calculate final jump velocity
        Vector3 jumpVelocity = jumpDir * jumpSpeed;
        float wallJumpHeight = playerNetvars.jumpHeight.Value * 0.6f;

        // Apply velocity
        currentVelocity = new Vector3(jumpVelocity.x, wallJumpHeight, jumpVelocity.z);

        // Reset state
        isWallSliding = false;
        canWallJump = false;
        rotateCameraZ = false;
    }

    private void RunGroundLogic()
    {

        if (isDashing) return;
        

        if (WantsToMove)
        {
            float speed = IsSprinting ? playerNetvars.maxSprintSpeed.Value : playerNetvars.maxWalkSpeed.Value;
            Vector3 targetVelocity = wishDir * speed;
            targetVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, playerNetvars.acceleration.Value * TICK_INTERVAL);
        }
        else
        {
            Vector3 targetVelocity = new Vector3(0f, currentVelocity.y, 0f);
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, playerNetvars.friction.Value * TICK_INTERVAL);
        }

        if (IsJumping)
        {
            float jumpChainCapSpeed = playerNetvars.maxSprintSpeed.Value; // max allowed speed if we jump with the exception of our current momentum being greater than this value
            float allowedSpeed = currentHorizontalSpeed; // default speed is current speed (no change in velocity)
            Vector3 jumpDir = WantsToMove ? wishDir.normalized : horizontalVelocity.normalized; // default direction is current velocity if no input is detected. Otherwise use the wish dir

            if (currentHorizontalSpeed < jumpChainCapSpeed) // if our current speed is less than the maximum jump speed
            {
                float targetSpeed = currentHorizontalSpeed * playerNetvars.jumpVelocityMultiplier.Value; // Get the speed the player WILL be after the jump
                allowedSpeed = Mathf.Min(targetSpeed, jumpChainCapSpeed); // Cap the target speed to not exceed the maximum allowed speed by a jump
            }

            // Apply the boost
            Vector3 boost = jumpDir * allowedSpeed;
            currentVelocity.x = boost.x;
            currentVelocity.z = boost.z;

            // Set vertical velocity (keep it simple)
            currentVelocity.y = playerNetvars.jumpHeight.Value;

            IsJumping = false;
        }
        else
        {
            currentVelocity.y = playerNetvars.stickToGroundForce.Value;
        }
    }

    private void RunAirborneLogic()
    {
        if (isDashing) return;
       
        // Handle double jump
        if (jumpJustPressed && !hasDoubleJumped && !IsOnGround && !isWallSliding)
        {
            hasDoubleJumped = true;
            doubleJumpCooldown = 0.5f; // Cooldown before allowing another double jump
            currentVelocity.y = doubleJumpForce;
        }

        // Apply gravity
        currentVelocity.y += playerNetvars.gravity.Value * TICK_INTERVAL * 3f;

        Vector3 currentHorizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        
        if (WantsToMove)
        {
            // Calculate acceleration with TICK_INTERVAL
            Vector3 velocityChange = wishDir * (playerNetvars.airAcceleration.Value * TICK_INTERVAL);
            
            // Apply velocity change
            currentHorizontalVel += velocityChange;
            
            // Clamp to max air speed
            float currentSpeed = currentHorizontalVel.magnitude;
            if (currentSpeed > playerNetvars.maxAirSpeed.Value)
            {
                currentHorizontalVel = currentHorizontalVel.normalized * playerNetvars.maxAirSpeed.Value;
            }
        }
        // else - no input means maintain current velocity

        // Apply back to velocity (preserve Y)
        currentVelocity.x = currentHorizontalVel.x;
        currentVelocity.z = currentHorizontalVel.z;
    }

    private void UpdateDashState()
    {
        // Update dash cooldown timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= TICK_INTERVAL;
        }

        if (!isDashing) return;

        dashTimer += TICK_INTERVAL;

        if (dashTimer >= playerNetvars.dashDuration.Value)
        {
            isDashing = false;
            dashCooldownTimer = playerNetvars.dashCooldown.Value; // Set cooldown when dash ends
        }
    }

    public void Dash(Vector3 direction)
    {
        if (isDashing || dashCooldownTimer > 0f) return; // Prevent dashing while already dashing or on cooldown

        isDashing = true;
        dashTimer = 0f;
        dashDirection = direction.normalized;

        // preserve existing momentum
        Vector3 currentHorizontalVel = horizontalVelocity;
        float currentSpeed = currentHorizontalVel.magnitude;
        float dashSpeed = playerNetvars.dashSpeed.Value;

        // get the dash direction's magnitude based on if the player's current velocity is greater than the max air velocity using math.min
        float dashDirectionMagnitude = Mathf.Min(currentSpeed + dashSpeed, playerNetvars.maxAirSpeed.Value);

        // Set initial dash velocity (preserve existing vertical velocity)
        currentVelocity = new Vector3(
            dashDirection.x * dashDirectionMagnitude,
            currentVelocity.y, // Keep existing vertical velocity
            dashDirection.z * dashDirectionMagnitude
        );
    }

    public void DrawDebugVector(GameObject linePrefab, Vector3 origin, Vector3 direction, float length, Color color, bool render = true)
    {
        LineRenderer line = linePrefab.GetComponent<LineRenderer>();
        line.SetPosition(0, origin);
        line.SetPosition(1, origin + direction.normalized * length);
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.enabled = render;
    }

    private void Update()
    {
        if (!IsOwner) return;

        currentInputCommand = inputHandler.GetCurrentInputCommand();
    }

    private Vector3 previousPosition;

    private void FixedUpdate()
    {
        if (!IsOwner) return;


        // Skip movement if respawning
        if (GetComponent<MechPlayerManager>().IsRespawning)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        UpdateDashState();
        DetectWalls();

        // save last position before movement for collision detection
        previousPosition = transform.position;

        ApplyMovement(currentInputCommand);
    }

    [SerializeField] private float wallCheckDistance = 1.5f;
    [SerializeField] private float wallCheckRayOffset = 0.4f;
    [SerializeField] private float wallCheckRayAngle = 25f;
    [SerializeField] private LayerMask wallLayer;

    private void DetectWalls()
    {
        // Side wall checks (left/right)
        Vector3 rayLeftOrigin = transform.position + (transform.right * wallCheckRayOffset);
        Vector3 rayRightOrigin = transform.position + (-transform.right * wallCheckRayOffset);
        Vector3 rayBackOrigin = transform.position; // Back check origin (center of player)

        Vector3 rayLeftDirection = Quaternion.Euler(0, -wallCheckRayAngle, 0) * transform.right;
        Vector3 rayRightDirection = Quaternion.Euler(0, wallCheckRayAngle, 0) * -transform.right;
        Vector3 rayBackDirection = transform.forward; // backward

        RaycastHit hitLeft;
        RaycastHit hitRight;
        RaycastHit hitBack;

        bool wallOnLeft = Physics.Raycast(rayLeftOrigin, rayLeftDirection, out hitLeft, wallCheckDistance, wallLayer);
        bool wallOnRight = Physics.Raycast(rayRightOrigin, rayRightDirection, out hitRight, wallCheckDistance, wallLayer);
        bool wallOnBack = Physics.Raycast(rayBackOrigin, rayBackDirection, out hitBack, wallCheckDistance, wallLayer);

        // Debugging rays
        Debug.DrawRay(rayLeftOrigin, rayLeftDirection * wallCheckDistance, Color.blue);
        Debug.DrawRay(rayRightOrigin, rayRightDirection * wallCheckDistance, Color.blue);
        Debug.DrawRay(rayBackOrigin, rayBackDirection * wallCheckDistance, Color.red); // Back ray in red
        Debug.DrawRay(transform.position, wishDir * 1f, Color.green);

        if (IsOnGround) return;

        // Check for any wall collision (left, right, or Back)
        if (wallOnLeft || wallOnRight || wallOnBack)
        {
            // Determine which wall was hit
            RaycastHit hit = wallOnBack ? hitBack : (wallOnLeft ? hitLeft : hitRight);
            string structureTag = hit.transform.tag;

            if (structureTag != "GrabbleStructure") return;

            // Set camera tilt based on wall side (no tilt for Back walls)
            cameraRotateZAngle = wallOnLeft ? -60f : (wallOnRight ? 60f : 0f);

            Vector3 normal = hit.normal;

            // Check if approaching the wall at a valid angle
            float angleToWallFromWishDir = Mathf.Abs(Vector3.SignedAngle(wishDir.normalized, normal, Vector3.up));
            float angleToWallFromVelocity = Mathf.Abs(Vector3.SignedAngle(horizontalVelocity.normalized, normal, Vector3.up));

            bool validApproach = angleToWallFromWishDir > 90f || angleToWallFromVelocity > 90f;

            if (validApproach)
            {
                wallNormal = normal;
                isWallSliding = true;
            }
            else
            {
                isWallSliding = false;
                canWallJump = false;
                rotateCameraZ = false;
            }
        }
        else
        {
            isWallSliding = false;
            canWallJump = false;
            rotateCameraZ = false;
        }
    }
}