using UnityEngine;
using System.Collections;
using Unity.Netcode;
public class handleMovement : NetworkBehaviour
{
    [Header("Transform References")]
    public CameraShakes camera_Shakes;
    public CharacterController characterController;

    public enum MoveState {
        idle,
        walking,
        sprinting,
        dashing,
        thrusting,
        wallrunning,
        inAir,
    };

    public MoveState currentMoveState;

    [SerializeField] float currentPlayerSpeed;
    [SerializeField] float playerWalkSpeed;
    [SerializeField] float playerDashSpeed;
    [SerializeField] float playerSprintSpeed;
    [SerializeField] float playerAcceleration;
    [SerializeField] float playerDeceleration;
    [SerializeField] float gravity;

    public Vector3 input;
    public Vector3 horizontalMoveDirection;
    public Vector3 currentPlayerVelocity;
    public bool IsOnGround;
    public bool sprintButton;
    public bool sprintButtonDT;
    private float doubleTapTimer;
    public bool IsMovingHorizontally; 

    [Header("Thruster Settings")]
    [SerializeField] public float fuelMax; // Maximum thruster fuel
    [SerializeField] private float fuelRegen; // Fuel restored per second when not boosting
    [SerializeField] private float sprintingFuelUsage; // Fuel used per second while boosting
    [SerializeField] private float thrusterFuelUsage; // Fuel used per second while boosting
    public float currentFuelAmount;
    public bool IsThrusting;
    public float thrusterGravity;
    public float wallRunGravity;
    public bool IsDashing;
    public bool IsWalking;
    public bool runDashCooldown;
    public float dashCoolDown;
    public float dashTime = 0.2f;
    public bool canUseFuel;
    private bool waitingForRefuel;
    public float fuelRegenStartTimer;

    [Header("Dash Settings")]

    // dashing
    public bool storeDashDirection = true;
    public Vector3 DashDirection;
    public bool dashKey = false; 

    [Header("Wall Grab Settings")]
    // wall grabbing
    public bool isHangingOnWall = false;
    private RaycastHit wallGrabHit;

    [Header("Wall Run Settings")]
    // wall running

    private bool IsWallRunReady = true;
    private bool InWallRun = false;
    private Vector3 wallHitNormal;
    public Ray rayDirection;
    public float stickingForce;

    // for cooldowns
    public bool canWallRun = false;
    private bool pressedSpaceBar;
    public bool pressedOnce = true;

    public float doubleTapWindow = 0.3f;
    private bool isWaiting = false;
    private KeyCode lastKeyPressed;

    public float thrustersCooldown = 1f;
    public bool CanUseThrusters = true;


    [Header("Networked Stuff")]

    private NetworkVariable<Vector3> net_Velocity = new NetworkVariable<Vector3>(Vector3.zero, writePerm: NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn()
    {
        currentMoveState = MoveState.idle;

        stickingForce = 30f;
        
        camera_Shakes = GetComponent<CameraShakes>();
        
        doubleTapTimer = 0.3f;
        
        playerAcceleration = 7f;
        playerDeceleration = 5f;

        playerWalkSpeed = 5f;
        playerDashSpeed = 30f;
        playerSprintSpeed = 10f;
        currentPlayerSpeed = playerWalkSpeed;

        gravity = -9.81f;
        thrusterGravity = 20f;
        characterController = GetComponent<CharacterController>();
        characterController.minMoveDistance = 0;
        // fuel stuff
        fuelRegenStartTimer = 2f;
        fuelMax = 100f;
        fuelRegen = 20f;
        currentFuelAmount = fuelMax; // start with full fuel

        // for mechanics using fuel
        thrusterFuelUsage = 43f;
        sprintingFuelUsage = 10f;
        dashCoolDown = 0.5f;

        IsThrusting = false;
        IsOnGround = false;
        sprintButton = false;
        sprintButtonDT = false;
        waitingForRefuel = true;
        canUseFuel = false;
        runDashCooldown = false;
        IsDashing = false;
        IsMovingHorizontally = false;

    }

    public void CheckDoubleTap(KeyCode key, System.Action onDoubleTap)
    {
        if (Input.GetKeyDown(key))
        {
            if (isWaiting && key == lastKeyPressed)
            {
                onDoubleTap?.Invoke();
                isWaiting = false;
            }
            else
            {
                lastKeyPressed = key;
                StartCoroutine(DoubleTapTimeout(key));
            }
        }
    }

    void HandleInput()
    {
        if (IsOwner) {

            pressedSpaceBar = Input.GetKey(KeyCode.Space);
            input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Jump"));
            sprintButton = Input.GetKey(KeyCode.LeftShift);
            dashKey = Input.GetKey(KeyCode.C);
            IsThrusting = input.z > 0;

            SendInputToServerRpc(input);

        }

        
    }

    void Update()
    {
        HandleInput();
        HandleServerMovement();
        // HandleMovement();
    }


    [ServerRpc]
    void SendInputToServerRpc(Vector3 input) {
        Vector3 horizontalMoveDirectionNormalized = (-transform.right * input.x - transform.forward * input.y).normalized;

        bool IsMovingHorizontally = horizontalMoveDirectionNormalized.magnitude > 0.1f;

        net_Velocity.Value = new Vector3(horizontalMoveDirection.x * currentPlayerSpeed, currentPlayerVelocity.y, horizontalMoveDirection.z * currentPlayerSpeed);
        
    }


    private void HandleServerMovement() {

        if (IsOwner) {
            // currentPlayerVelocity = Vector3.Lerp(currentPlayerVelocity, net_Velocity.Value, Time.deltaTime * playerAcceleration);

            characterController.Move(net_Velocity.Value * Time.deltaTime);
        }
        else {

            transform.position = net_Velocity.Value;
            
        }
        

    }

    void HandleDashing() {
        // dashing logic
        // check conditions for a dash
        if (!IsDashing && !runDashCooldown && dashKey && IsMovingHorizontally)
        {
            // setting IsDashing to true and limiting constant dashing
            IsDashing = true;
            DashDirection = new Vector3(horizontalMoveDirection.x * playerDashSpeed, 0f, horizontalMoveDirection.z * playerDashSpeed);
        }

        // if we start dashing
        if (IsDashing) {
            // main dash logic
            // stored dash direction or control it in air if we are moving horizontally
            currentPlayerVelocity += new Vector3(DashDirection.x, 0f, DashDirection.z) * Time.deltaTime * 5f;

            // block sprinting and movement input while dashing
            sprintButton = false;
            
            // keep moving for dash time
            dashTime -= Time.deltaTime;
            
            // once dash is done, run the cool down for it
            if (dashTime < 0f) {
                dashTime = 0.2f;
                IsDashing = false;
                // start the cooldown
                runDashCooldown = true;
            }
        }

        // Wait for cooldown to finish before dashing again
        if (runDashCooldown) {
            dashCoolDown -= Time.deltaTime;
            if (dashCoolDown <= 0) {
                dashCoolDown = 0.5f;
                runDashCooldown = false;
            }
        }
    }


    void HandleHorizontalMovement()
    {
        horizontalMoveDirection = (-transform.right * input.x - transform.forward * input.y).normalized;
        IsMovingHorizontally = horizontalMoveDirection.magnitude > 0.1f;

    
        HandleDashing();

        if (IsDashing) {
            return;
        }


        // base movement
        if (!IsMovingHorizontally)
        {
            currentPlayerSpeed = Mathf.Lerp(currentPlayerSpeed, 0f, Time.deltaTime * 1.5f);
        }


        if (!sprintButton && IsMovingHorizontally)
        {
            currentPlayerSpeed = playerWalkSpeed;
        }

        if (canUseFuel && sprintButton && IsMovingHorizontally) {
            currentPlayerSpeed = playerSprintSpeed;
        }

        currentPlayerVelocity = Vector3.Lerp(currentPlayerVelocity, new Vector3(horizontalMoveDirection.x * currentPlayerSpeed, currentPlayerVelocity.y, horizontalMoveDirection.z * currentPlayerSpeed), Time.deltaTime * playerAcceleration);

    }


    void HandleVerticalMovement()
    {

        IsOnGround = characterController.isGrounded;

        if (canUseFuel && IsThrusting) {
            // currentPlayerVelocity.y = 0f;
            currentPlayerVelocity.y = Mathf.Lerp(currentPlayerVelocity.y, thrusterGravity, Time.deltaTime * playerAcceleration * 0.5f);
        }
        else {
            if (IsOnGround) {
                currentPlayerVelocity.y = -2f;
            }
            else {
                currentPlayerVelocity.y += gravity * Time.deltaTime * playerAcceleration * 0.6f;
            }
        }
        
    }


    void HandleFuelSystem() 
    {
        // fuel management
        currentFuelAmount = Mathf.Clamp(currentFuelAmount, 0f, fuelMax);

        // if the fuel is out and we are waiting to refill
        if (currentFuelAmount <= 0f && waitingForRefuel)
        {
            // start the regen timer
            fuelRegenStartTimer -= Time.deltaTime;

            // if the timer runs out
            if (fuelRegenStartTimer <= 0f)
            {
                // reset it
                fuelRegenStartTimer = 2f;

                // set waitingForRefuel to false so that we don't start the timer again
                waitingForRefuel = false;
            }
        }
        else {
            waitingForRefuel = true;
        }

        // Can use fuel if not waiting for a refuel (cooldown after fuel runs out)
        canUseFuel = fuelRegenStartTimer == 2f;

        // always regen if we can use fuel unless we are draining it with movement
        if (canUseFuel)
        {
            // sprinting/boosting logic
            if (sprintButton && !IsDashing && IsMovingHorizontally) {
                currentFuelAmount -= sprintingFuelUsage * Time.deltaTime;
            } 
            else if (IsThrusting) {
                currentFuelAmount -= thrusterFuelUsage * Time.deltaTime;
            }
            else {
                // normal fuel regen
                currentFuelAmount += fuelRegen * Time.deltaTime;
                // if fuel is more than max
                if (currentFuelAmount > fuelMax)
                {
                    // set it to max
                    currentFuelAmount = fuelMax;
                }
            }
        }
    }

    void HandleMovement()
    {
        // if (IsOwner) {
            HandleVerticalMovement();
            HandleHorizontalMovement();
            HandleFuelSystem();

            if (!IsMovingHorizontally && currentPlayerSpeed < 0.1f) {
                currentPlayerSpeed = 0f;
            }

            // apply movement finally
            characterController.Move(currentPlayerVelocity * Time.deltaTime);
        // }
    }



    IEnumerator DoubleTapTimeout(KeyCode key)
    {
        isWaiting = true;
        yield return new WaitForSeconds(doubleTapWindow);
        isWaiting = false;
    }

    IEnumerator JumpCooldown() {
        yield return new WaitForSeconds(0.5f);
        while (pressedSpaceBar) {
            yield return null;
        }
        pressedOnce = true;
    }

    IEnumerator RunWallRunCooldown() {
        InWallRun = false;
        IsWallRunReady = false;
        yield return new WaitForSeconds(0.8f);
        IsWallRunReady = true;
    }

}





// saved wall running function (currently inactive)
/*

void HandleWallRunning() {

    if (IsOnGround) {
        return;
    }


    // wall detection

    int wallRunLayerMask = LayerMask.GetMask("Wall");

    // im going to loop through 3 traces. One for the movedirection and one for right/left side
    // whichever trace hits a wall first, we will use that one
    // we will store that trace's normal

    for (int i = 0; i < 3; i++) {
        if (i == 0) {
            rayDirection = new Ray(transform.position, horizontalMoveDirection);
        }
        else if (i == 1) {
            rayDirection = new Ray(transform.position, -transform.right);
        }
        else if (i == 2) {
            rayDirection = new Ray(transform.position, transform.right);
        }

        if (IsWallRunReady && Physics.Raycast(rayDirection, out RaycastHit hitInfo, 1f, wallRunLayerMask)) {
            // let's say we start hit the wall and are in air
            // we can start wall running
            // Before we do that, let's store the normal of the wall we hit
            wallHitNormal = hitInfo.normal;
            InWallRun = true;
            break;
        }
        else {
            Debug.DrawRay(transform.position, rayDirection.direction, Color.white);
        }
    }

    if (InWallRun)  
    {
        Vector3 dirTowall = -wallHitNormal;

        Vector3 wallRunDirection = Vector3.Cross(wallHitNormal, Vector3.up).normalized;

        bool DetectWall = Physics.Raycast(transform.position, dirTowall, out RaycastHit hitinfo, 1f, wallRunLayerMask);

        if (DetectWall) {

            currentPlayerSpeed = Mathf.Lerp(currentPlayerSpeed, playerSprintSpeed * 1.5f, Time.deltaTime * playerAcceleration);

            // we are on the wall
            // let's stick the player to the wall
            // float distanceToWall = Vector3.Distance(hitinfo.point + (wallHitNormal * 0.1f), transform.position);

            float signedAngleDiff = Vector3.SignedAngle(horizontalMoveDirection, wallRunDirection, Vector3.up);

            signedAngleDiff *= -1;

            // get off the wall if player jumps with not mvi
            if (canUseFuel & IsThrusting) {
                StartCoroutine(RunWallRunCooldown());
                currentPlayerVelocity.x = horizontalMoveDirection.x * currentPlayerSpeed * 1.5f;
                currentPlayerVelocity.z = horizontalMoveDirection.z * currentPlayerSpeed * 1.5f;
                return;
            }


            if (IsMovingHorizontally) {
                gravity = wallRunGravity;
                // // get off the wall if the player isn't aligned with the wall
                // if (signedAngleDiff > 0f && signedAngleDiff > 45f && signedAngleDiff < 135f) {
                
                //     StartCoroutine(RunWallRunCooldown());
                //     return;
                // }

                // INSTEAD OF DOING THAT DUMB SHIT, DONT' CAP AN ANGLE. IT'S A MECH AFTER ALL.
            }
            else {
                gravity = -9.81f;
            }

            currentPlayerVelocity.x += dirTowall.x * stickingForce * Time.deltaTime;
            currentPlayerVelocity.z += dirTowall.z * stickingForce * Time.deltaTime;
            
        }
        else {
            InWallRun = false;
        }
    }
    else {
        gravity = -9.81f;
    }
}

*/
