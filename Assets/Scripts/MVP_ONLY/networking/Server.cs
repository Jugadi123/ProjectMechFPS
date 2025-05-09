
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

// in game server when everyone has connected to a lobby
public class Server : NetworkBehaviour {

    void Awake()
    {
    }
    
    public override void OnNetworkSpawn() {
        NetworkTimer.Singleton.onTickUpdate += HandleTickUpdate;
    }

    public override void OnNetworkDespawn()
    {
        NetworkTimer.Singleton.onTickUpdate -= HandleTickUpdate;
    }

    private void HandleTickUpdate(ulong currentTick) {

        IReadOnlyDictionary<ulong, NetworkClient> allConnectedClients = NetworkManager.Singleton.ConnectedClients;

        if (allConnectedClients.Count == 0) return;

        Vector3 clientPosition = allConnectedClients[0].PlayerObject.transform.position;

        Debug.Log(clientPosition);

        transform.position = Vector3.Lerp(transform.position, clientPosition, NetworkTimer.Singleton.tickInterval * 2f);

    }


}

