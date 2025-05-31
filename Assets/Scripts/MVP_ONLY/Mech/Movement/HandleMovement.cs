using UnityEngine;
using Unity.Netcode;

public class HandleMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject wishDirLinePrefab;
    [SerializeField] private GameObject velocityLinePrefab;
    [SerializeField] private GameObject rotationLinePrefab;
    [SerializeField] private Transform lowerBody;

    private CharacterController characterController;
    private PlayerNetvars playerNetvars;
    private InputHandler inputHandler;
    private InputHandler.InputCommand currentInputCommand;

    private const float TICK_INTERVAL = NetworkTimer.TickInterval;

    // Movement state
    public Vector3 wishDir;
    public Vector3 currentVelocity;
    private Vector3 horizontalVelocity;
    private float currentHorizontalSpeed;

    // Ground movement
    public bool WantsToMove = false;
    private bool IsSprinting = false;
    public bool IsOnGround = false;
    public bool allowJump = true; // tells us if the player is in air and has jumped
    private bool IsJumping = false;

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
    [SerializeField] private float defaultWallJumpBoost = 12f; // Initial boost when jumping off wall


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerNetvars = GetComponent<PlayerNetvars>();
        inputHandler = GetComponent<InputHandler>();
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

        // Reset wall states if grounded
        if (IsOnGround)
        {
            isWallSliding = false;
        }

        bool dashHeld = Input.GetKey(KeyCode.C); // DASH KEY (C)
        bool canDash = dashHeld && WantsToMove && !isDashing && dashCooldownTimer <= 0f;

        // DASH logic (press C while moving)
        if (canDash)
        {
            Dash(wishDir);
        }

        bool jumpHeld = currentInputCommand.jump;

        // Wall jump logic
        if (jumpHeld && isWallSliding && canWallJump)
        {
            WallJump();
        }
        // Normal jump logic
        else if (jumpHeld && IsOnGround)
        {
            if (allowJump)
            {
                IsJumping = true;
                allowJump = false;
            }
        }
        else if (IsOnGround)
        {
            // reset the jump only if we are on ground
            allowJump = true;
        }

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

        // Debug visuals
        // DrawDebugVector(wishDirLinePrefab, lowerBody.position - transform.forward * 0.5f, wishDir, 1f, Color.green);
        // DrawDebugVector(velocityLinePrefab, lowerBody.position - transform.forward * 0.5f, horizontalVelocity, currentHorizontalSpeed, Color.blue);
    }

    private void RunWallSlideLogic()
    {
        // Reduce momentum gently (simulate sliding drag) after momentum window is gone
        currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, wallGrabDeceleration * TICK_INTERVAL);

        if (currentVelocity.magnitude <= 0f)
        {
            currentVelocity = Vector3.zero;
            isWallSliding = false;
        }

        canWallJump = true; // can jump off the wall if player doesn't wanna fully stop and hang on the wall
    }


    private void WallJump()
    {
        // Preserve existing momentum when jumping off quickly
        float preservedSpeed = currentHorizontalSpeed;

        // Jump direction logic
        if (currentInputCommand.jump)
        {
            Vector3 jumpDir;
            Vector3 jumpVelocity;
            float jumpSpeed = defaultWallJumpBoost; // default wall jump speed

            if (WantsToMove)
            {
                float angleToWall = Mathf.Abs(Vector3.Angle(wishDir, -wallNormal));
                if (angleToWall < 90f)
                {
                    // Jumping into wall, bounce off with fixed force
                    jumpDir = wallNormal;
                }
                else
                {
                    // Jumping away, use current velocity to leap off if not less than the minimum jump velocity

                    jumpDir = wishDir.normalized;

                    if (preservedSpeed > defaultWallJumpBoost)
                    {
                        jumpSpeed = preservedSpeed * 1.2f; // add a little bit of boost on the existing momentum
                    }
                }
            }
            else
            {
                // Default push off
                jumpDir = wallNormal;
            }

            // reset state
            isWallSliding = false;
            canWallJump = false;

            // get final jump velocity
            jumpVelocity = new Vector3(jumpDir.x, 0f, jumpDir.z) * jumpSpeed;
            float wallJumpHeight = playerNetvars.jumpHeight.Value * 0.6f;
            currentVelocity = new Vector3(jumpVelocity.x, wallJumpHeight, jumpVelocity.z); // use a different value for the jump height off the wall
        }
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
            float jumpChainCapSpeed = 20f; // max allowed speed if we jump with the exception of our current momentum being greater than this value
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
        if (isDashing)
        {
            return;
        }

        // Apply gravity
        currentVelocity.y += playerNetvars.gravity.Value * TICK_INTERVAL * 3f;

        // Get current horizontal velocity
        Vector3 currentHorizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        float currentSpeed = currentHorizontalVel.magnitude;

        if (currentSpeed > 1f)
        {
            if (WantsToMove)
            {
                // Calculate angle between current velocity and wish direction
                float angle = Vector3.Angle(currentHorizontalVel, wishDir);

                float placeholderAirAcceleration = 0.8f;

                // If trying to change direction too aggressively, slow down
                if (angle > 120f)
                {
                    // Apply braking force for sharp turns
                    currentHorizontalVel = Vector3.Lerp(
                        currentHorizontalVel,
                        Vector3.zero,
                        placeholderAirAcceleration * TICK_INTERVAL * 2f
                    );
                }
                else
                {
                    // Directly lerp toward wish direction while maintaining speed
                    currentHorizontalVel = Vector3.Lerp(
                        currentHorizontalVel,
                        wishDir.normalized * currentSpeed,
                        placeholderAirAcceleration * TICK_INTERVAL
                    );
                }
            }
            else
            {
                // No input - maintain current velocity (only gravity affects it)
                // Optional: add slight deceleration if desired
                // currentHorizontalVel = Vector3.Lerp(
                //     currentHorizontalVel,
                //     Vector3.zero,
                //     placeholderAirAcceleration * TICK_INTERVAL * 0.4f
                // );
            }
        }
        else if (WantsToMove)
        {
            // If stationary but wanting to move, start moving in wish direction
            currentHorizontalVel = wishDir.normalized * playerNetvars.minAirSpeed.Value;
        }

        // Apply back to velocity
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

        // dash using the normal dash speed unless
        // the current horizontal velocity is more than the minimum air velocity
        // in that case, allow the player to dash with their momentum (allows chaining)
        float dashSpeed = playerNetvars.dashSpeed.Value;

        // Set initial dash velocity (preserve existing vertical velocity)
        currentVelocity = new Vector3(
            dashDirection.x * dashSpeed,
            currentVelocity.y, // Keep existing vertical velocity
            dashDirection.z * dashSpeed
        );
    }

    public void DrawDebugVector(GameObject linePrefab, Vector3 origin, Vector3 direction, float length, Color color, bool render = true)
    {
        if (!IsOwner) return;
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

        UpdateDashState();

        DetectWalls();

        // save last position before movement for collision detection
        previousPosition = transform.position;
        
        ApplyMovement(currentInputCommand);

    }


    [SerializeField] private float wallCheckDistance = 1.2f;
    [SerializeField] private LayerMask wallLayer;


    // wall detection
    private void DetectWalls()
    {
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 45f, 0) * (-transform.right * 1f), Color.blue);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, -45f, 0) * (transform.right * 1f), Color.blue);



        if (IsOnGround)
        {
            return;
        }

        Vector3 origin = transform.position;
        Vector3 left = Quaternion.Euler(0, -45f, 0) * transform.right;
        Vector3 right = Quaternion.Euler(0, 45f, 0) * -transform.right;

        RaycastHit hitLeft;
        RaycastHit hitRight;

        bool wallOnLeft = Physics.Raycast(origin, left, out hitLeft, wallCheckDistance, wallLayer);
        bool wallOnRight = Physics.Raycast(origin, right, out hitRight, wallCheckDistance, wallLayer);

        if (wallOnLeft || wallOnRight)
        {
            Vector3 normal = wallOnLeft ? hitLeft.normal : hitRight.normal; // get appropriate wall normal

            float approachAngleWishdir = Vector3.Angle(-wallNormal, wishDir.normalized);
            float approachAngleVelocity = Vector3.Angle(-wallNormal, horizontalVelocity.normalized);

            if (approachAngleWishdir < 100f || approachAngleVelocity < 100f)
            {
                // trigger wall jump state
                wallNormal = normal;
                isWallSliding = true;
            }
            else
            {
                isWallSliding = false;
                canWallJump = false;
            }
        }
        else
        {
            isWallSliding = false;
            canWallJump = false;
        }
    }
}