using UnityEngine;
using Unity.Netcode;

public class HandleClientPrediction : NetworkBehaviour
{
    [SerializeField] private int fps;
    private const float TICK_INTERVAL = NetworkTimer.TickInterval;

    private InputHandler inputHandler;
    private HandleMovement handleMovement;

    public PlayerNetvars playerNetvars;
    public InputHandler.InputCommand currentClientInputCommand;

    [SerializeField] private float serverPositionTolerance = 0.5f; // more = less rubberbanding
    [SerializeField] private float reconciliationLerpSpeed = 10f;

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
        // TODO: Apply server-authoritative movement here based on input from command

        Vector3 placeholderClientPosition = Vector3.zero; // * get the real client reported position
        if (Vector3.Distance(transform.position, placeholderClientPosition) > serverPositionTolerance)
        {
            // Trigger a reconciliation if prediction error is too large
            ulong correctedTick = command.tick;
            ReconcilePlayerPositionClientRpc(transform.position, correctedTick);
        }

        UpdateStateClientRpc(transform.position);
    }


    [ClientRpc]
    private void ReconcilePlayerPositionClientRpc(Vector3 correctServerPosition, ulong correctedTick)
    {
        if (!IsOwner) return; // only localplayer can correct prediction

        // Smoothly correct position error
        transform.position = Vector3.Lerp(
            transform.position,
            correctServerPosition,
            TICK_INTERVAL * reconciliationLerpSpeed
        );

        // Reapply inputs that happened after the corrected tick
        ulong currentTick = NetworkTimer.Singleton.CurrentTick.Value;

        for (ulong t = correctedTick + 1; t <= currentTick; t++)
        {
            InputHandler.InputCommand inputCommand = inputHandler.GetInputBuffer()[t % InputHandler.BUFFER_SIZE];

            if (inputCommand.tick == t) // make sure tick is valid 
            {
                // TODO: Re-apply this input command
            }
        }
    }


    [ClientRpc] // update client's state
    private void UpdateStateClientRpc(Vector3 position)
    {
        if (IsOwner) return; // ignore localplayer
        // set interpolation state
        fromPosition = transform.position;
        toPosition = position;
        interpTimer = 0f;
        interpDuration = TICK_INTERVAL * 2f;
    }

    private void InterpolateOtherPlayers()
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
            InterpolateOtherPlayers();
        }
    }

    private void OnServerTick()
    {
        if (!IsOwner) return;

        // Apply client prediction here
        // handleMovement.ApplyMovement(currentClientInputCommand);

        // Send input and state to server for validation
        // SendInputToServerRpc(currentClientInputCommand);
    }
}
