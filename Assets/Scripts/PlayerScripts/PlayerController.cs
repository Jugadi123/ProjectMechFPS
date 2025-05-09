using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class PlayerController : NetworkBehaviour {
    [SerializeField] GameObject GhostPrefab;
    private const int BUFFER_SIZE = 1024;

    private handleMovement handle_movement;

    public Vector3 input;

    public override void OnNetworkSpawn() {

    }

    public void Update()
    {

        if (IsOwner) {
            input = handle_movement.input;
        }

    }
}
