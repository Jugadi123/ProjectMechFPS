using UnityEngine;
using Unity.Netcode;


public class HandleWeaponInput : NetworkBehaviour
{

    // Creating a new fresh weapon system starting with the input handling
    // Get Input

    private struct InputCommand
    {
        public ulong tick;
        public bool primaryFireInput;
        public bool secondaryFireInput;
    }

    private void Update()
    {
        if (IsOwner)
        {

            // create a new input command and collect input
            InputCommand inputCommand = new InputCommand
            {
                tick = NetworkTimer.Singleton.CurrentTick.Value,
                primaryFireInput = Input.GetKey(KeyCode.Mouse0),
                secondaryFireInput = Input.GetKey(KeyCode.Mouse1)
            };

            // this function gets the input and passes it on to the weapon handlers depending on the weapon type (primary or secondary, hitscan or projectile)
            HandleWeaponType.GetWeaponInput(inputCommand.primaryFireInput, inputCommand.secondaryFireInput);

            // Send input to server
            // SendInputToServerRpc(inputCommand);
        }
    }
}