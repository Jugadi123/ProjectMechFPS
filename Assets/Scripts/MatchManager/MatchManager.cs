// This script is going to handle all networked logic once a map or game has loaded. This is assuming a matchmaker has connected clients and has transferred it's clients to this server and scene (matchmanager). Yes we will be using seperate server for player auth, matchmaking, and gameplay.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class MatchManager : NetworkBehaviour
{
    private static MatchManager Singleton;

    [SerializeField] GameObject playerPrefab;

    private int minPlayersNeededForMatch;

    public NetworkVariable<float> globalMatchCountDown = new NetworkVariable<float>();

    private float globalMatchStartTime = 5f;

    public NetworkVariable<bool> IsMatchLive = new NetworkVariable<bool>(false);

    [SerializeField] List<Transform> spawnPoints = new List<Transform>();


    public override void OnNetworkSpawn()
    {

        Application.targetFrameRate = 70;
        
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


        // netvar init
        globalMatchCountDown.Value = globalMatchStartTime;

        // local var init
        minPlayersNeededForMatch = 1;


        // all event subs
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        // NetworkManager.NetworkTickSystem.Tick += UpdateTickClientRpc;
        globalMatchCountDown.OnValueChanged += GlobalMatchCountDownClientRpc;

    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }


    [ClientRpc]
    private void GlobalMatchCountDownClientRpc(float previousValue, float newValue) {
        // any ui updates, etc
    }

    // [ClientRpc]
    // private void UpdateTickClientRpc() {
    //     Debug.Log($"Server Tick: {NetworkManager.LocalTime.Tick}");
    // }


    [ClientRpc]
    private void SendMessageClientRpc(string message) {
        Debug.Log("Match Manager: " + message);
    }

    private void OnClientConnected(ulong clientId) {
        int totalClients = NetworkManager.Singleton.ConnectedClients.Count;
        SendMessageClientRpc($"Client {clientId} Connected. Total Clients: {totalClients}");
        // if enough players joined, start the match
        if (totalClients >= minPlayersNeededForMatch) {
            StartCoroutine(HandleMatchCountDown());
        }
    }
    
    private void OnClientDisconnected(ulong clientId) {
        int totalClients = NetworkManager.Singleton.ConnectedClients.Count;
        SendMessageClientRpc($"Client {clientId} Disconnected. Total Clients: {totalClients}");
    }


    
    private Transform? GetBestSpawnForPlayer() {

        // if there's atleast one spawn point available
        if (!(spawnPoints.Count > 0)) {
            return null;
        }

        Transform bestSpawnFound = null;

        // foreach (Transform spawn in spawnPoints) {

        // }

        bestSpawnFound = spawnPoints[(int)Random.Range(0, spawnPoints.Count - 1)];

        return bestSpawnFound;
    }

    IEnumerator HandleMatchCountDown() {

        Debug.Log("Starting Match...");

        while ((ushort)globalMatchCountDown.Value > 0) {
            globalMatchCountDown.Value -= Time.fixedDeltaTime;
            Debug.Log("Match Count Down: " + (ushort)globalMatchCountDown.Value);
            yield return null;
        }

        // Ready to spawn all clients.
        Debug.Log("Match Started!");
        Debug.Log("Spawning Players!");

        // contains networkclient objects
        IReadOnlyList<NetworkClient> allConnectedClients = NetworkManager.ConnectedClientsList;

        // check if there is atleast one spawn point available
        foreach (NetworkClient client in allConnectedClients) {

            ulong clientId = client.ClientId;

            Transform? bestSpawn = GetBestSpawnForPlayer();

            if (bestSpawn is null) {
                continue;
            }

            Vector3 spawnLocation = bestSpawn.position;

            GameObject player = Instantiate(playerPrefab, spawnLocation, Quaternion.identity);

            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
    }
}
