// This script is going to handle all networked logic once a map or game has loaded. This is assuming a matchmaker has connected clients and has transferred it's clients to this server and scene (matchmanager). Yes we will be using seperate server for player auth, matchmaking, and gameplay.
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class MatchServer : NetworkBehaviour
{
    private static MatchServer Singleton;

    [SerializeField] GameObject playerPrefab;

    // [SerializeField] GameObject playerGhostPrefab;

    private int minPlayersNeededForMatch;

    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    public override void OnNetworkSpawn()
    {
        // is this the server?
        if (!IsServer) return;

        // Singleton handling
        if (Singleton != null && Singleton != this) {
            Destroy(gameObject); // Avoid duplicates
            return;
        }
        
        Singleton = this;
        DontDestroyOnLoad(Singleton);

        // some placeholder messages

        Debug.Log("Initialized Match Manager"); // display more details like map, number of players, game mode, match id, etc
        Debug.Log("Waiting for players to join.");

        // local var init
        minPlayersNeededForMatch = 1;


        // all event subs
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        // NetworkManager.NetworkTickSystem.Tick += UpdateTickClientRpc;

    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    // [ClientRpc]
    // private void UpdateTickClientRpc() {
    //     Debug.Log($"Server Tick: {NetworkManager.LocalTime.Tick}");
    // }


    [ClientRpc]
    private void SendMessageAllClientRpc(string message) {
        Debug.Log("Match Manager: " + message);
    }

    [ClientRpc]
    private void SendMessageTargetClientRpc(string message, ulong clientId, ClientRpcParams rpcParams = default) {  
        Debug.Log("Match Manager: " + message);
    }

    private void OnClientConnected(ulong clientId) {
        IReadOnlyList<NetworkClient> totalClients = NetworkManager.Singleton.ConnectedClientsList;
        SendMessageAllClientRpc($"Client {clientId} Connected. Total Clients: {totalClients.Count}");

        SpawnPlayer(clientId);

        // if enough players joined, start the match
        if (totalClients.Count >= minPlayersNeededForMatch) {
            // what will we do when we have enough players for a match? pre-match phase
        }
    }
    
    private void OnClientDisconnected(ulong clientId) {
        int totalClients = NetworkManager.Singleton.ConnectedClients.Count;
        SendMessageAllClientRpc($"Client {clientId} Disconnected. Total Clients: {totalClients}");
    }


    private void SpawnPlayer(ulong clientId) {

        IReadOnlyList<NetworkClient> allConnectedClients = NetworkManager.Singleton.ConnectedClientsList;
        Transform bestSpawn = GetBestSpawnForPlayer(allConnectedClients);

        Vector3 spawnLocation = bestSpawn.position;

        GameObject player = Instantiate(playerPrefab, spawnLocation, Quaternion.identity);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        // GameObject ghost = Instantiate(playerGhostPrefab, spawnLocation, Quaternion.identity);
        // ghost.GetComponent<NetworkObject>().Spawn();
    }

    
    private Transform GetBestSpawnForPlayer(IReadOnlyList<NetworkClient> playerList) {

        // if there's atleast one spawn point available
        if (!(spawnPoints.Count > 0)) {
            return null;
        }

        Transform bestSpawnFound = spawnPoints[(int)Random.Range(0, spawnPoints.Count)];
    
        return bestSpawnFound;
    }
}
