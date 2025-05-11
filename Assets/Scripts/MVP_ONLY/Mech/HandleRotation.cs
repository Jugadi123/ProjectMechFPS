using UnityEngine;
using Unity.Netcode;
public class HandleRotation : NetworkBehaviour
{
    [SerializeField] Transform upperBody;
    public float mouseSensitivity;
    public float xRotation;
    public float yRotation;

    private struct MouseCommand {
        public ulong tick;
        public Vector2 input;
    }

    private const int MouseInputBufferSize = 60;
    private MouseCommand[] mouseInputBuffer = new MouseCommand[MouseInputBufferSize];
    [SerializeField] private float rotationReconcileThreshold = 2f;

    // for interpolation
    private Quaternion fromYaw;
    private Quaternion toYaw;
    private Quaternion fromPitch;
    private Quaternion toPitch;

    private float interpTimer;
    private float interpDuration;

    
    public override void OnNetworkSpawn()
    {
        xRotation = 0f;
        yRotation = 0f;
    }

    void Update()
    {
        if (IsOwner) {
            HandleMouseInput();
        }
        else if (!IsServer) {
            // interpolate other player's rotations
            InterpolateOtherRotations();
        }
    }

    private void InterpolateOtherRotations()
    {
        interpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(interpTimer / interpDuration);

        transform.rotation = Quaternion.Slerp(fromYaw, toYaw, t);
        upperBody.localRotation = Quaternion.Slerp(fromPitch, toPitch, t);
    }
    

    private void HandlePlayerRotation(float mouseX, float mouseY) {
        // clamp pitch
        xRotation += mouseY;
        xRotation = Mathf.Clamp(xRotation, -60f, 60f);

        // clamp yaw
        yRotation += mouseX;
        yRotation = (yRotation + 360f) % 360f;

        // apply rotation
        upperBody.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // pitch
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f); // yaw
    }

    void HandleMouseInput() {
        // get current tick
        ulong currentTick = NetworkTimer.Singleton.CurrentTick.Value;

        Vector2 mouseInput = new Vector2(Input.GetAxisRaw("Mouse X") * mouseSensitivity, Input.GetAxisRaw("Mouse Y") * mouseSensitivity);

        // apply client prediction
        HandlePlayerRotation(mouseInput.x, mouseInput.y);

        // new mouse command
        MouseCommand mouseCommand = new MouseCommand {
            tick = currentTick,
            input = mouseInput
        };

        // write to buffer
        mouseInputBuffer[currentTick % MouseInputBufferSize] = mouseCommand;

        // send to server
        SendMouseInputToServerRpc(currentTick, mouseInput, xRotation, yRotation);
    }


   [ServerRpc]
    private void SendMouseInputToServerRpc(ulong tick, Vector2 input, float clientXRotation, float clientYRotation)
    {
        // server applies input too
        HandlePlayerRotation(input.x, input.y);

        // compare rotation difference
        float xError = Mathf.Abs(xRotation - clientXRotation);
        float yError = Mathf.Abs(yRotation - clientYRotation);

        if (xError > rotationReconcileThreshold || yError > rotationReconcileThreshold)
        {
            ReconcileClientRpc(xRotation, yRotation, tick);
        }

        // Broadcast final rotation to other clients
        BroadcastRotationClientRpc(xRotation, yRotation);
    }


    [ClientRpc]
    private void ReconcileClientRpc(float correctedX, float correctedY, ulong correctedTick)
    {
        if (!IsOwner) return;

        // Snap (could lerp?) to server rotation
        xRotation = correctedX;
        yRotation = correctedY;

        // how does the block below work exactly?
        // Reapply buffered inputs from correctedTick to now
        ulong currentTick = NetworkTimer.Singleton.CurrentTick.Value;
        for (ulong t = correctedTick + 1; t <= currentTick; t++)
        {
            MouseCommand cmd = mouseInputBuffer[t % MouseInputBufferSize];
            if (cmd.tick == t)
            {
                HandlePlayerRotation(cmd.input.x, cmd.input.y);
            }
        }
    }


    [ClientRpc]
    private void BroadcastRotationClientRpc(float pitch, float yaw)
    {
        if (IsOwner) return;

        // Setup interpolation
        fromYaw = transform.rotation;
        toYaw = Quaternion.Euler(0f, yaw, 0f);

        fromPitch = upperBody.localRotation;
        toPitch = Quaternion.Euler(pitch, 0f, 0f);

        interpTimer = 0f;
        interpDuration = NetworkTimer.Singleton.GetTickInterval() * 2f; // 2-tick delay ????? how is this in the past?
    }
}
