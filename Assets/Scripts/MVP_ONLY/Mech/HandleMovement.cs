using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;

public class HandleMovement : NetworkBehaviour {


    [SerializeField] private int fps;

    [SerializeField] private CharacterController characterController;

    // IMPORT doubletap.cs
    [SerializeField] private DoubleTap doubleTapHandler;

    // player's current speed
    private float currentPlayerSpeed;
    // Player's current velocity
    public Vector3 currentPlayerVelocity;
    public Vector3 horizontalMoveDirection;
    public bool IsMovingHorizontally = false;

    // Getting the NetworkSimulator instance for simulating network conditions
    private NetworkSimulator networkSimulator;

    // Get netvars from PlayerNetvars
    public PlayerNetvars playerNetvars;

    // buffers
    private struct InputCommand
    {
        public ulong tick;
        public Vector3 input;
        public bool sprintKey;
        public bool dashKey;
    }

    private const int InputBufferSize = 60;
    private InputCommand[] inputBuffer = new InputCommand[InputBufferSize];

    [SerializeField] private float serverPositionTolerance = 0.5f;
    [SerializeField] private float reconciliationLerpSpeed = 10f;


    // for interpolation
    private Vector3 fromPosition;
    private Vector3 toPosition;
    private float interpTimer;
    private float interpDuration;


    // for dashing
    public bool IsDashing = false;
    private bool IsDashOnCooldown = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection = Vector3.zero;

    public bool IsThrusting = false;
    public bool IsOnGround = false;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerNetvars = GetComponent<PlayerNetvars>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            networkSimulator = FindFirstObjectByType<NetworkSimulator>();
        }
    }


    // === DEBUG NETWORK TESTING ===
    private void SimulateNetworkConditionsForDebug()
    {
        if (IsOwner) {
            if (Input.GetKeyDown(KeyCode.F))
                networkSimulator?.TriggerLagSpike(System.TimeSpan.FromMilliseconds(500));

            if (Input.GetKeyDown(KeyCode.G))
                networkSimulator?.Disconnect();
        }
    }


    void HandleVerticalMovement(float inputZ)
    {
        float tickInterval = NetworkTimer.Singleton.GetTickInterval();

        IsThrusting = inputZ > 0;

        IsOnGround = characterController.isGrounded;

        if (IsThrusting) 
        {
            currentPlayerVelocity.y = Mathf.Lerp(currentPlayerVelocity.y, playerNetvars.thrusterForce.Value, tickInterval * playerNetvars.playerAcceleration.Value * 0.5f);
        }
        else 
        {
            if (IsOnGround) 
            {
                currentPlayerVelocity.y = -2f;
            }
            else 
            {
                currentPlayerVelocity.y += playerNetvars.gravity.Value * tickInterval * playerNetvars.playerAcceleration.Value * 0.6f;
            }
        }
    }

    private void HandleDashing(Vector3 inputDirection, bool IsMoving, bool dashKeyPressed)
    {
        float tickInterval = NetworkTimer.Singleton.GetTickInterval();
        float dashDuration = playerNetvars.dashTime.Value;
        float dashCooldown = playerNetvars.dashCoolDown.Value;
        float dashSpeed = playerNetvars.playerDashSpeed.Value;

        // === Start Dash ===
        if (!IsDashing && !IsDashOnCooldown && dashKeyPressed && IsMoving)
        {
            IsDashing = true;
            dashTimer = dashDuration;

            // dash direction is locked in (momentum-based)
            dashDirection = inputDirection.normalized * dashSpeed;
        }

        // === Execute Dash ===
        if (IsDashing)
        {
            currentPlayerVelocity = new Vector3(dashDirection.x, currentPlayerVelocity.y, dashDirection.z);

            dashTimer -= tickInterval;

            // End dash
            if (dashTimer <= 0f)
            {
                IsDashing = false;
                IsDashOnCooldown = true;
                dashCooldownTimer = dashCooldown;
            }
        }

        // === Cooldown timer ===
        if (IsDashOnCooldown)
        {
            dashCooldownTimer -= tickInterval;
            if (dashCooldownTimer <= 0f)
            {
                IsDashOnCooldown = false;
            }
        }
    }

    private void HandleHorizontalMovement(Vector3 input, bool sprintKey, bool dashKey)
    {
        float tickInterval = NetworkTimer.Singleton.GetTickInterval();

        // Convert input into world-space direction
        horizontalMoveDirection = (-transform.right * input.x - transform.forward * input.y).normalized;
        IsMovingHorizontally = horizontalMoveDirection.magnitude > 0.1f;

        // === DASHING ===
        HandleDashing(horizontalMoveDirection, IsMovingHorizontally, dashKey);

        // Determine movement speed
        float targetSpeed = 0f;

        if (!IsMovingHorizontally)
        {
            targetSpeed = 0f;
        }
        else if (!sprintKey)
        {
            targetSpeed = playerNetvars.playerWalkSpeed.Value;
        }
        else if (sprintKey)
        {
            targetSpeed = playerNetvars.playerSprintSpeed.Value;
        }

        // Smooth current speed toward target speed
        // float smoothSpeed = Mathf.Lerp(currentPlayerSpeed, targetSpeed, tickInterval * playerNetvars.playerAcceleration.Value);

        // Update velocity
        Vector3 newHorizontalVelocity = horizontalMoveDirection * targetSpeed;
        Vector3 targetHorizontalVelocity = new Vector3(newHorizontalVelocity.x, currentPlayerVelocity.y, newHorizontalVelocity.z);
        currentPlayerVelocity = Vector3.Lerp(currentPlayerVelocity, targetHorizontalVelocity, tickInterval * playerNetvars.playerAcceleration.Value);
    }

    private void MovePlayer(Vector3 currentVelocity) {
        float tickInterval = NetworkTimer.Singleton.GetTickInterval();
        characterController.Move(currentVelocity * tickInterval);
    }


    [ServerRpc]
    private void SendInputToServerRpc(ulong tick, Vector3 input, bool sprintKey, bool dashKey, Vector3 clientReportedPosition)
    {
        // Server auth movement
        HandleVerticalMovement(input.z);
        HandleHorizontalMovement(input, sprintKey, dashKey);
        MovePlayer(currentPlayerVelocity);

        if (Vector3.Distance(transform.position, clientReportedPosition) > serverPositionTolerance)
        {
            // Reconcile the position with the server
            ReconcilePlayerPositionClientRpc(transform.position, tick);
        }

        BroadcastPositionClientRpc(transform.position);
    }


    [ClientRpc]
    private void ReconcilePlayerPositionClientRpc(Vector3 correctServerPosition, ulong correctedTick)
    {
        // Update the last received server position for us (local player)
        if (IsOwner) {

            // Debug.Log($"[Reconciliation] Correcting position to {correctServerPosition} at tick {correctedTick}");

            // Smoothly move toward the corrected position
            transform.position = Vector3.Lerp(
                transform.position,
                correctServerPosition,
                NetworkTimer.Singleton.GetTickInterval() * reconciliationLerpSpeed
            );

            // Replay all unconfirmed inputs since corrected tick
            ulong currentTick = NetworkTimer.Singleton.CurrentTick.Value;
            for (ulong t = correctedTick + 1; t <= currentTick; t++)
            {
                InputCommand inputCommand = inputBuffer[t % InputBufferSize];
                if (inputCommand.tick == t) // Make sure it's valid
                {
                    HandleHorizontalMovement(inputCommand.input, inputCommand.sprintKey, inputCommand.dashKey);
                    HandleVerticalMovement(inputCommand.input.z);
                }
            }
        }
    }

    [ClientRpc]
    private void BroadcastPositionClientRpc(Vector3 position)
    {
        if (IsOwner) return;
        // this for other players
        fromPosition = transform.position;
        toPosition = position;
        interpTimer = 0f;
        interpDuration = NetworkTimer.Singleton.GetTickInterval() * 2f;
        
    }


    private void InterpolateOtherPlayers()
    {
        // Interpolation logic for remote players
        // This is where you would handle the interpolation of other players' positions
        // For example, using a Lerp function to smoothly transition between positions
        // based on the last known position/velocity and the current tick.
        if (interpTimer < interpDuration)
        {
            interpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(interpTimer / interpDuration);
            transform.position = Vector3.Lerp(fromPosition, toPosition, t);
        }
    }


    private Vector3 inputDirection;
    private bool sprintKey;
    private bool dashKey;

    void Update()
    {
        if (IsServer) {
            Application.targetFrameRate = 60;
        }
        else if (IsOwner) {
            Application.targetFrameRate = fps;
        }

        if (IsOwner)
        {
            // Gather input
            inputDirection = new Vector3(
                Input.GetAxisRaw("Horizontal"), 
                Input.GetAxisRaw("Vertical"), 
                Input.GetAxisRaw("Jump")
            );
            sprintKey = Input.GetKey(KeyCode.LeftShift);
            dashKey = Input.GetKey(KeyCode.C);

            // Simulate network conditions for debugging
            SimulateNetworkConditionsForDebug();
        }
        else if (!IsServer)
        {
            // Handle input for remote players
            InterpolateOtherPlayers();
        }
    }

    void FixedUpdate()
    {
        if (IsOwner)
        {
            // Client-side prediction and movement processing
            HandleVerticalMovement(inputDirection.z);
            HandleHorizontalMovement(inputDirection, sprintKey, dashKey);

            // Move the player
            MovePlayer(currentPlayerVelocity);

            // input buffering and sending rpc
            ulong currentTick = NetworkTimer.Singleton.CurrentTick.Value;

            inputBuffer[currentTick % InputBufferSize] = new InputCommand
            {
                tick = currentTick,
                input = inputDirection,
                sprintKey = sprintKey,
                dashKey = dashKey
            };

            SendInputToServerRpc(currentTick, inputDirection, sprintKey, dashKey, transform.position);
        }
    }

}