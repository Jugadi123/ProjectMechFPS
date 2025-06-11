using UnityEngine;
using Unity.Netcode;

public class InputHandler : MonoBehaviour
{

    public struct InputCommand : INetworkSerializable
    {
        public ulong tick;
        public Vector2 mouseInput;
        public Vector3 horizontalInput;
        public bool jump;
        public bool sprint;
        public bool dash;
        public Vector3 clientVelocity; // Add current velocity
        public Vector3 clientPosition; // Add current position

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref mouseInput);
            serializer.SerializeValue(ref horizontalInput);
            serializer.SerializeValue(ref jump);
            serializer.SerializeValue(ref sprint);
            serializer.SerializeValue(ref dash);
            serializer.SerializeValue(ref clientVelocity);
            serializer.SerializeValue(ref clientPosition);
        }
    }

    public const int BUFFER_SIZE = 200; // approx 3.33 seconds
    private InputCommand[] clientInputBuffer = new InputCommand[BUFFER_SIZE];

    public InputCommand GetCurrentInputCommand()
    {
        // get current tick
        ulong currentTick = NetworkTimer.Singleton.CurrentTick.Value;
    
        // Ensure we don't overwrite unprocessed commands
        int bufferIndex = (int)(currentTick % BUFFER_SIZE);

        // collect input
        // Get raw mouse input
        Vector2 mouse = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        );
        Vector3 horizontalInput = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical")
        );
        bool jump = Input.GetKey(KeyCode.Space);
        bool sprintKey = Input.GetKey(KeyCode.LeftShift);
        bool dashKey = Input.GetKey(KeyCode.C);

        // Create command with current state
        InputCommand command = new InputCommand
        {
            tick = currentTick,
            mouseInput = mouse,
            horizontalInput = horizontalInput,
            jump = jump,
            sprint = sprintKey,
            dash = dashKey,
            clientVelocity = GetComponent<HandleMovement>().currentVelocity,
            clientPosition = transform.position
        };

        // Store in buffer
        clientInputBuffer[bufferIndex] = command;

        return command;
    }

    public InputCommand[] GetInputBuffer()
    {
        return clientInputBuffer;
    }

}