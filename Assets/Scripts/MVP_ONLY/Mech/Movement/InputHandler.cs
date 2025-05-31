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
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref mouseInput);
            serializer.SerializeValue(ref horizontalInput);
            serializer.SerializeValue(ref jump);
            serializer.SerializeValue(ref sprint);
            serializer.SerializeValue(ref dash);
        }
    }

    public const int BUFFER_SIZE = 200; // approx 3.33 seconds
    private InputCommand[] clientInputBuffer = new InputCommand[BUFFER_SIZE];

    public InputCommand GetCurrentInputCommand()
    {
        // get current tick
        ulong currentTick = NetworkTimer.Singleton.CurrentTick.Value;

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

        // write to buffer and return command
        int bufferIndex = (int)currentTick % BUFFER_SIZE;
        return clientInputBuffer[bufferIndex] = new InputCommand
        {
            tick = currentTick,
            mouseInput = mouse,
            horizontalInput = horizontalInput,
            jump = jump,
            sprint = sprintKey,
            dash = dashKey
        };
    }

    public InputCommand[] GetInputBuffer()
    {
        return clientInputBuffer;
    }

}