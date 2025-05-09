using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
// import multiplayer tools runtime
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;


public class PlayerInput : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 10f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _distanceTolerance = 0.5f;
    [SerializeField] private float _reconcileLerpSpeed = 10f;
    
    private CharacterController _controller;
    private Vector3 _velocity;
    private readonly Queue<(int tick, Vector3 moveInput, bool jump)> _inputBuffer = new();


    // Interpolation
    private Vector3 _targetPosition;
    private Vector3 _previousPosition;
    private float _interpolationTimer;
    private float _interpolationPeriod;
    

    NetworkSimulator networkSimulator;

    

    void Awake() {
        _controller = GetComponent<CharacterController>();   
    } 

    public override void OnNetworkSpawn()
    {

        if (IsOwner) {
            // NetworkSimulatorPresetAsset - used for the scriptable object
            // Referencing with scripts
            // NetworkSimulatorPresets - used for the preset
            // NetworkSimulatorPresets.HomeBroadband - used for the preset
            networkSimulator = FindFirstObjectByType<NetworkSimulator>();
        } 

        if (!IsOwner)
        {
            // Initialize positions for other clients
            _interpolationPeriod = NetworkTimer.Singleton.GetTickInterval();
            _targetPosition = transform.position;
            _previousPosition = transform.position;
        }
    }

    void Update()
    {
        if (IsOwner) {
            if (Input.GetKeyDown(KeyCode.F))
            {
                // trigger lag spike
                networkSimulator.TriggerLagSpike(System.TimeSpan.FromMilliseconds(500));
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                // Simulate network disconnection
                networkSimulator.Disconnect();
            }
        }
       

        HandleClientPrediction();
        InterpolateOtherClients();
    }

    void HandleClientPrediction() {
        if (!IsOwner) return;
        // Capture input
        Vector3 input = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0,
            Input.GetAxisRaw("Vertical")
        ).normalized;

        bool jump = Input.GetKeyDown(KeyCode.Space);
        int currentTick = NetworkTimer.Singleton.CurrentTick.Value;

        // Store and predict
        _inputBuffer.Enqueue((currentTick, input, jump));
        ApplyMovement(input, jump);
        SendInputToServerRpc(currentTick, input, jump, transform.position);
    }

    void InterpolateOtherClients()
    {
        if (IsOwner) return;

        if (_interpolationTimer < _interpolationPeriod)
        {
            _interpolationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_interpolationTimer / _interpolationPeriod);
            transform.position = Vector3.Lerp(_previousPosition, _targetPosition, t);
        }
    }

    void ApplyMovement(Vector3 moveInput, bool jump)
    {
        bool isGrounded = _controller.isGrounded;
        
        // Grounded logic
        if (isGrounded && _velocity.y < 0) 
            _velocity.y = -2f;
        
        // Jump
        if (jump && isGrounded)
            _velocity.y = _jumpForce;

        // Movement
        Vector3 move = transform.TransformDirection(moveInput) * _moveSpeed;
        _velocity.x = move.x;
        _velocity.z = move.z;
        _velocity.y += _gravity * NetworkTimer.Singleton.GetTickInterval();

        _controller.Move(_velocity * NetworkTimer.Singleton.GetTickInterval());
    }

    [ServerRpc]
    void SendInputToServerRpc(int tick, Vector3 input, bool jump, Vector3 clientPosition)
    {
        // Server-side movement
        ApplyMovement(input, jump);

        // Reconciliation check
        if (Vector3.Distance(transform.position, clientPosition) > _distanceTolerance)
        {
            CorrectPositionClientRpc(transform.position, tick);
        }

        // Update position to all other clients
        UpdatePositionClientRpc(transform.position);
    }

    // for other clients
    [ClientRpc]
    void UpdatePositionClientRpc(Vector3 newPosition)
    {
        if (!IsOwner) {
            _previousPosition = transform.position;
            _targetPosition = newPosition;
            _interpolationTimer = 0f;
        }
    }

    [ClientRpc]
    void CorrectPositionClientRpc(Vector3 serverPosition, int processedTick)
    {

        if (!IsOwner) return;

        // Smooth reconciliation
        transform.position = Vector3.Lerp(
            transform.position, 
            serverPosition, 
            NetworkTimer.Singleton.GetTickInterval() * _reconcileLerpSpeed
        );

        // Dequeue inputs up to this tick and replay buffered inputs
        while (_inputBuffer.Count > 0 && _inputBuffer.Peek().tick <= processedTick)
            _inputBuffer.Dequeue();
   
    }
}