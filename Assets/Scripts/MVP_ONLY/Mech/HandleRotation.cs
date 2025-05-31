using UnityEngine;
using Unity.Netcode;

public class HandleRotation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Transform upperBody; // Pitch rotation

    [Header("Rotation Settings")]
    public float mouseSensitivity = 0.5f;
    public float maxTurnSpeed = 400f;    // Degrees per second
    public float pitchMin = -60f;      // Minimum pitch angle
    public float pitchMax = 60f;       // Maximum pitch angle

    // Current rotation state
    private float clientCurrentPitch = 0f;
    private float clientCurrentYaw = 0f;
    private float serverCurrentPitch = 0f;
    private float serverCurrentYaw = 0f;

    // Network smoothing
    public float rotationReconcileThreshold = 5f;
    
    private InputHandler inputHandler;
    private InputHandler.InputCommand currentInputCommand;


    private void Awake()
    {
        inputHandler = GetComponent<InputHandler>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            currentInputCommand = inputHandler.GetCurrentInputCommand();
            HandleLocalRotation(currentInputCommand);
        }
        else
        {
            // todo interpolate remote client rotations
        }
    }

    private void HandleLocalRotation(InputHandler.InputCommand inputCommand)
    {
        float tickInterval = NetworkTimer.Singleton.GetTickInterval();

        // Calculate desired rotation changes
        float desiredYawDelta = inputCommand.mouseInput.x * mouseSensitivity * maxTurnSpeed * tickInterval;
        float desiredPitchDelta = inputCommand.mouseInput.y * mouseSensitivity * maxTurnSpeed * tickInterval;

        // Apply HARD turn rate limits
        float yawDelta = Mathf.Clamp(desiredYawDelta, -maxTurnSpeed * tickInterval, maxTurnSpeed * tickInterval);
        float pitchDelta = Mathf.Clamp(desiredPitchDelta, -maxTurnSpeed * tickInterval, maxTurnSpeed * tickInterval);

        // Apply rotation
        clientCurrentYaw += yawDelta;
        clientCurrentPitch += pitchDelta;

        // Clamp rotations
        clientCurrentYaw = Mathf.Repeat(clientCurrentYaw, 360f);
        clientCurrentPitch = Mathf.Clamp(clientCurrentPitch, pitchMin, pitchMax);

        // Apply locally
        upperBody.localRotation = Quaternion.Euler(clientCurrentPitch, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, clientCurrentYaw, 0f);


        ulong currentTick = inputCommand.tick;

        SendRotationToServerRpc(inputCommand.mouseInput, clientCurrentPitch, clientCurrentYaw, currentTick);

    }

    [ServerRpc]
    private void SendRotationToServerRpc(Vector2 clientInput, float clientPitch, float clientYaw, ulong clientTick)
    {
        // Apply rotation on server
        // Calculate desired rotation changes
        float tickInterval = NetworkTimer.Singleton.GetTickInterval();

        float desiredYawDelta = clientInput.x * mouseSensitivity * maxTurnSpeed * tickInterval;
        float desiredPitchDelta = clientInput.y * mouseSensitivity * maxTurnSpeed * tickInterval;

        // Apply HARD turn rate limits
        float yawDelta = Mathf.Clamp(desiredYawDelta, -maxTurnSpeed * tickInterval, maxTurnSpeed * tickInterval);
        float pitchDelta = Mathf.Clamp(desiredPitchDelta, -maxTurnSpeed * tickInterval, maxTurnSpeed * tickInterval);

        // Apply rotation
        serverCurrentYaw += yawDelta;
        serverCurrentPitch += pitchDelta;

        // Clamp rotations
        serverCurrentYaw = Mathf.Repeat(serverCurrentYaw, 360f);
        serverCurrentPitch = Mathf.Clamp(serverCurrentPitch, pitchMin, pitchMax);

        // Apply rotation
        upperBody.localRotation = Quaternion.Euler(serverCurrentPitch, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, serverCurrentYaw, 0f);

        // check for reconcilation
        bool NeedToReconcile = Mathf.Abs(clientPitch - serverCurrentPitch) > rotationReconcileThreshold || Mathf.Abs(Mathf.DeltaAngle(clientYaw, serverCurrentYaw)) > rotationReconcileThreshold;

        if (NeedToReconcile)
        {
            ulong correctedTick = clientTick;
            SendReconcileRequestToClientRpc(serverCurrentPitch, serverCurrentYaw, correctedTick);
        }
 
        // send rotation to other clients
        BroadcastRotationClientRpc(serverCurrentPitch, serverCurrentYaw);
    }


    [ClientRpc]
    private void SendReconcileRequestToClientRpc(float correctedPitch, float correctedYaw, ulong correctedTick)
    {
        if (!IsOwner) return; // only localplayer can correct prediction

        // snap to the corrected rotation
        // todo interpolate rotation instead of snapping it
        clientCurrentPitch = correctedPitch;
        clientCurrentYaw = correctedYaw;

        // Replay inputs that happened after the corrected tick
        ulong currentTick = NetworkTimer.Singleton.CurrentTick.Value;

        for (ulong t = correctedTick + 1; t <= currentTick; t++)
        {
            InputHandler.InputCommand inputCommand = inputHandler.GetInputBuffer()[t % InputHandler.BUFFER_SIZE];

            if (inputCommand.tick == t) // make sure tick is valid 
            {
                HandleLocalRotation(inputCommand);
            }
        }
    }

    [ClientRpc]
    private void BroadcastRotationClientRpc(float serverPitch, float serverYaw)
    {
        if (IsOwner) return; // ignore localplayer

        upperBody.localRotation = Quaternion.Euler(serverPitch, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, serverYaw, 0f);
    }
}