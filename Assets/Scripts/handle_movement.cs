using UnityEngine;
using System.Collections;
public class handle_movement : MonoBehaviour
{

    [Header("Transform References")]
    public CameraShake cameraShake;
    public Transform primaryWeapon;
    public Transform secondaryWeapon;
    public CharacterController characterController;
    public AudioSource dashSound;
    public AudioSource thrustersStartSound;
    public AudioSource thrustersMidSound;
    public AudioSource thrustersEndSound;


    public enum MoveState {
        idle,
        walking,
        sprinting,
        dashing,
        thrusting,
        wallrunning,
        inAir,
    };

    public  MoveState currentMoveState;

    [SerializeField] float currentPlayerSpeed;
    [SerializeField] float playerWalkSpeed;
    [SerializeField] float playerDashSpeed;
    [SerializeField] float playerSprintSpeed;
    [SerializeField]  float playerAcceleration;
    [SerializeField]  float playerDeceleration;
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
    public bool IsDashing;
    public bool IsWalking;
    public bool runDashCooldown;
    public float dashCoolDown;
    public float dashTime;
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
    public bool isWallRunning = false;
    private Vector3 wallHitNormal;
    private Vector3 wallRunDirection;
    private Vector3 directionToWall;
    private bool initialWallCheck = false;
    private Ray rayDirection;
    float stickingForce;


    // for cooldowns
    public bool canWallRun = false;
    private bool pressedSpaceBar;
    public bool pressedOnce = true;

    public float doubleTapWindow = 0.3f;
    private bool isWaiting = false;
    private KeyCode lastKeyPressed;

    // LEAVE THIS ALONE
    // for client side interpolation of movement since our movement logic runs at a fixed rate or server tickrate
    private Vector3 lastPlayerPosition;
    private Vector3 currentPlayerPosition;
    private float tickTimer = 0f;
    [Header("Interpolation Settings")]
    public bool interpolateClientSide = true;

    // optional client side interpolation of rotation.
    // LEAVE THIS ALONE







    public float thrustersCooldown = 1f;
    public bool CanUseThrusters = true;
    

    void Start()
    {

        currentMoveState = MoveState.idle;

        stickingForce = 0.5f;

        interpolateClientSide = true;
        
        cameraShake = GetComponent<CameraShake>();
        
        doubleTapTimer = 0.3f;
        
        playerAcceleration = 7f;
        playerDeceleration = 5f;

        currentPlayerSpeed = 0f;
        playerWalkSpeed = 6f;
        playerDashSpeed = 30f;
        playerSprintSpeed = 10f;

        gravity = -15f;
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
        dashTime = 0f;
        dashCoolDown = 1f;

        IsThrusting = false;
        IsOnGround = false;
        sprintButton = false;
        sprintButtonDT = false;
        waitingForRefuel = true;
        canUseFuel = false;
        runDashCooldown = false;
        IsDashing = false;
        IsMovingHorizontally = false;


        thrustersStartSound.loop = false;
        thrustersMidSound.loop = true;
        thrustersEndSound.loop = false;

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
        pressedSpaceBar = Input.GetKey(KeyCode.Space);
        input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Jump"));
        sprintButton = Input.GetKey(KeyCode.LeftShift);
        dashKey = Input.GetKey(KeyCode.C);
        IsThrusting = Input.GetKey(KeyCode.Space);
    }





    void HandleDashingAnimation()
    {
        if (IsDashing)
        {
            // rotate arms
            StartCoroutine(cameraShake.RotateCamera(
                new Vector3(90f, 0f, 0f), 
                new Vector3(90f, 0f, 20f * input.x), 
                0.5f, 
                primaryWeapon
            ));

            // rotate arms
            StartCoroutine(cameraShake.RotateCamera(
                new Vector3(90f, 0f, 0f), 
                new Vector3(90f, 0f, 20f * input.x),
                0.5f, 
                secondaryWeapon
            ));

            // play dash sound
            if (!dashSound.isPlaying)
            {
                dashSound.Play();
            }
        }
    }

    void InterpolateClientPosition() {
        if (interpolateClientSide) {
            tickTimer += Time.deltaTime;

            float t = tickTimer / Time.fixedDeltaTime;
            t = Mathf.Clamp01(t);

            Vector3 interpolatedPos = Vector3.Lerp(lastPlayerPosition, currentPlayerPosition, t);
            characterController.transform.position = interpolatedPos;
        }
    }

    void Update()
    {
        HandleInput();
        HandleDashingAnimation();
        InterpolateClientPosition();
    }

    void FixedUpdate()
    {
        tickTimer = 0f;
        HandleMovement();  
    }


    void HandleDashing() {
        // dashing logic
        // check conditions for a dash
        if (!IsDashing && !runDashCooldown && dashKey)
        {
            // setting IsDashing to true and limiting constant dashing
            IsDashing = true;
            DashDirection = new Vector3(horizontalMoveDirection.x * playerDashSpeed, 0f, horizontalMoveDirection.z * playerDashSpeed);
        }

        // if we start dashing
        if (IsDashing) {
            // main dash logic
            // stored dash direction or control it in air if we are moving horizontally
            currentPlayerVelocity = new Vector3(DashDirection.x, currentPlayerVelocity.y, DashDirection.z);


            // block sprinting and movement input while dashing
            sprintButton = false;
            
            // keep moving for dash time
            dashTime += Time.fixedDeltaTime;

            // once dash is done, run the cool down for it
            if (dashTime > 0.4f) {
                dashTime = 0f;
                IsDashing = false;
                // start the cooldown
                runDashCooldown = true;
            }
        }

        // Wait for cooldown to finish before dashing again
        if (runDashCooldown) {
            dashCoolDown -= Time.fixedDeltaTime;
            if (dashCoolDown <= 0) {
                dashCoolDown = 1f;
                runDashCooldown = false;
            }
        }
    }


    void HandleWallRunning() {

        // if we are on the ground dont wall run
        if (IsOnGround) {
            return;
        }

        // let's trace a ray from the player to their move direction and see if we hit a wall
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
            initialWallCheck = Physics.Raycast(rayDirection, out RaycastHit hitInfo, 1f, wallRunLayerMask);
            if (initialWallCheck) {
                // let's say we start hit the wall and are in air
                // we can start wall running
                // Before we do that, let's store the normal of the wall we hit
                wallHitNormal = hitInfo.normal;
                isWallRunning = true;
                break;
            }
            else {
                Debug.DrawRay(transform.position, rayDirection.direction, Color.white);
            }
        }
    

        // Once we begin wall running
        if (isWallRunning) {

            // We get the normal of our wall and use that to move towards the wall
            // This is done by getting the inverse of the normal
            directionToWall = -wallHitNormal;

            // going to trace another ray to stick us to the wall and check for continous wall detection
            if (Physics.Raycast(transform.position, directionToWall, out RaycastHit newinfo, 1f, wallRunLayerMask)) {
            }
            else {
                isWallRunning = false;
                return;
            }

            // let's get the wall run direction to check for our player's alignment with the wall
            wallRunDirection = -Vector3.Cross(wallHitNormal, Vector3.up);


            // once we have the direction to the wall, we can move towards it and stick our player to it
            // im keeping the y axis independent to not interfere with the player's y axis movement (up and down)
            currentPlayerVelocity += new Vector3(
                directionToWall.x * stickingForce,
                0f,
                directionToWall.z * stickingForce
            ); // DELTA TIME MISSING


            if (IsMovingHorizontally) {
                gravity = 0f;
                
            }
            else {
                gravity = -15f;
            }


            // check for movedirection angle
            // signed to get side of the wall
            float angleDiff = Vector3.SignedAngle(wallRunDirection, horizontalMoveDirection, Vector3.up);

            // sitck the player only if they are aligned with the wall otherwise, let them fall
            if (angleDiff < 0f) {
                // Debug.Log("wall run");
                if (!(angleDiff > -45f || angleDiff < -135f)) {
                    isWallRunning = false;
                    // StartCoroutine(RunWallRunCooldown());
                    currentPlayerVelocity += new Vector3(
                        directionToWall.x * stickingForce * 10,
                        0f,
                        directionToWall.z * stickingForce * 10
                    ) * -1; // DELTA TIIME MISSING
                }
            }
            
        }
        else {
            gravity = -15f;
        }
    }


    void HandleHorizontalMovement()
    {
        horizontalMoveDirection = (-transform.right * input.x - transform.forward * input.y).normalized;
        IsMovingHorizontally = horizontalMoveDirection.magnitude > 0.1f;

        
        HandleWallRunning();

        HandleDashing();

        if (!IsDashing && !isWallRunning) {
            // the line below assumes no special movement is applied like dashing, wallrunning, etc
            currentPlayerVelocity = Vector3.Lerp(currentPlayerVelocity, new Vector3(horizontalMoveDirection.x * playerWalkSpeed, currentPlayerVelocity.y, horizontalMoveDirection.z * playerWalkSpeed), Time.fixedDeltaTime * playerAcceleration);
        }


    }


    void HandleVerticalMovement()
    {

        int groundLayerMask = LayerMask.GetMask("Map", "Wall");

        IsOnGround = Physics.Raycast(transform.position, -transform.up, out RaycastHit groundHit, 1f, groundLayerMask);

        // if not wall running, we can jump and gravity
        if (!isWallRunning) {
            if (canUseFuel && IsThrusting) {
                // currentPlayerVelocity.y = 0f;
                currentPlayerVelocity.y = thrusterGravity;
                // currentPlayerVelocity.y = Mathf.Lerp(currentPlayerVelocity.y, thrusterGravity, Time.fixedDeltaTime * playerAcceleration);
            }
            else {
                if (IsOnGround) {
                    currentPlayerVelocity.y = -2f;
                }
                else {
                    if (currentPlayerVelocity.y > -15f)
                    {
                        currentPlayerVelocity.y += gravity * Time.fixedDeltaTime * 2f;
                    }
                    else {
                        currentPlayerVelocity.y = -15f;
                    }
                }
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
            fuelRegenStartTimer -= Time.fixedDeltaTime;

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
            if (sprintButton && !IsDashing && (input.x != 0 || input.y != 0)) {
                currentFuelAmount -= sprintingFuelUsage * Time.fixedDeltaTime;
            } 
            else if (IsThrusting) {
                currentFuelAmount -= thrusterFuelUsage * Time.fixedDeltaTime;
            }
            else {
                // normal fuel regen
                currentFuelAmount += fuelRegen * Time.fixedDeltaTime;
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
        HandleVerticalMovement();
        HandleHorizontalMovement();
        HandleFuelSystem();


        // final interpolation

        // get last position of player (for clientside interpolation)
        lastPlayerPosition = currentPlayerPosition;

        // apply movement finally
        characterController.Move(currentPlayerVelocity * Time.fixedDeltaTime);

        // store current position of player (for clientside interpolation)
        currentPlayerPosition = transform.position;
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
        canWallRun = false;
        yield return new WaitForSeconds(1f);
        canWallRun = true;
    }

}
