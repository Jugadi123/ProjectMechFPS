using UnityEngine;
using Unity.Netcode;

public class HandleClientPrediction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float serverPositionTolerance = 1f;
    [SerializeField] private float serverVelocityTolerance = 1f;
    [SerializeField] private float reconciliationLerpSpeed = 5f;

    private const float TICK_INTERVAL = NetworkTimer.TickInterval;

    // Dependencies
    private InputHandler inputHandler;
    private HandleMovement handleMovement;
    private PlayerNetvars playerNetvars;

    // State
    private InputHandler.InputCommand currentClientInputCommand;
    private Vector3 fromPosition;
    private Vector3 toPosition;
    private float interpTimer;
    private float interpDuration;

    private void Awake()
    {
        playerNetvars = GetComponent<PlayerNetvars>();
        inputHandler = GetComponent<InputHandler>();
        handleMovement = GetComponent<HandleMovement>();
    }

    public override void OnNetworkSpawn()
    {
        NetworkTimer.OnTick += OnServerTick;
    }

    public override void OnNetworkDespawn()
    {
        NetworkTimer.OnTick -= OnServerTick;
    }



    [ServerRpc]
    private void SendInputToServerRpc(InputHandler.InputCommand command)
    {
        // Always store client's reported state
        Vector3 clientPosition = command.clientPosition;
        Vector3 clientVelocity = command.clientVelocity;

        // Server applies movement
        handleMovement.ApplyMovement(command);

        // Calculate errors
        Vector3 serverPosition = transform.position;
        float positionError = Vector3.Distance(serverPosition, clientPosition);
        float velocityError = Vector3.Distance(handleMovement.currentVelocity, clientVelocity);

        // Always send corrections, but with different priorities
        if (positionError > serverPositionTolerance || velocityError > serverVelocityTolerance)
        {
            // Significant error - high priority correction
            ReconcilePlayerPositionClientRpc(transform.position, handleMovement.currentVelocity, command.tick);
        }
        else
        {
            // Small error - just update state
            UpdateStateClientRpc(transform.position, handleMovement.currentVelocity);
        }
        


        Debug.Log($"Pos Error: {positionError}, Vel Error: {velocityError}");
    }

    [ClientRpc]
    private void ReconcilePlayerPositionClientRpc(Vector3 correctServerPosition, Vector3 correctedServerVelocity, ulong correctedTick)
    {
        if (!IsOwner) return;

        // Snap to corrected position immediately (better than lerp for reconciliation)
        transform.position = correctServerPosition;
        handleMovement.currentVelocity = correctedServerVelocity;
        
        // Reapply all inputs since the corrected tick
        for (ulong t = correctedTick + 1; t <= NetworkTimer.Singleton.CurrentTick.Value; t++)
        {
            int bufferIndex = (int)(t % InputHandler.BUFFER_SIZE);
            InputHandler.InputCommand inputCommand = inputHandler.GetInputBuffer()[bufferIndex];
            
            if (inputCommand.tick == t)
            {
                handleMovement.ApplyMovement(inputCommand);
            }
        }
    }

    [ClientRpc]
    private void UpdateStateClientRpc(Vector3 position, Vector3 velocity)
    {
        if (IsOwner) return;

        // If we're already interpolating and get a new update,
        // start from current position rather than previous target
        fromPosition = transform.position;
        toPosition = position + velocity * TICK_INTERVAL * 2f;
        
        // Reset interpolation timer
        interpTimer = 0f;
        interpDuration = TICK_INTERVAL * 2f;
    }

    private void InterpolateRemotePlayers()
    {
        if (interpTimer < interpDuration)
        {
            interpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(interpTimer / interpDuration);
            transform.position = Vector3.Lerp(fromPosition, toPosition, t);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            // Gather input per tick in sync
            currentClientInputCommand = inputHandler.GetCurrentInputCommand();
        }
        else if (!IsServer)
        {
            // Remote client interpolation
            InterpolateRemotePlayers();
        }
    }

    private void OnServerTick()
    {
        if (!IsOwner) return;

        // Apply client prediction here
        handleMovement.ApplyMovement(currentClientInputCommand);

        // Send input and state to server for validation
        SendInputToServerRpc(currentClientInputCommand);
    }
}
