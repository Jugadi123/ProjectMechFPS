
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class HandleLagCompensation : NetworkBehaviour
{

    private const int bufferSize = 120;
    private struct PlayerHistory
    {
        public ulong tick;
        public Vector3 position;
    }

    // client id, history array containing position, rotation, and tick, etc
    private Dictionary<ulong, PlayerHistory[]> playerHistories = new();


    private void FixedUpdate()
    {
        if (!IsServer) return;

        ulong currentServerTick = NetworkTimer.Singleton.CurrentTick.Value;

        // connected clients
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject netObj = client.PlayerObject;
            if (!netObj) continue;

            var transform = netObj.transform;
            ulong clientId = client.ClientId;

            // get history for client and if none, create one with default size
            if (!playerHistories.ContainsKey(clientId))
            {
                playerHistories[clientId] = new PlayerHistory[bufferSize];
            }

            int bufferIndex = (int)(currentServerTick % bufferSize);
            playerHistories[clientId][bufferIndex] = new PlayerHistory
            {
                tick = currentServerTick,
                position = transform.position
            };
        }
    }
    


    public bool TryLagCompensatedRaycast(ulong firingClientId, ulong firedTick, Vector3 rayOrigin, Vector3 rayDirection, float rayRange, out RaycastHit hitResult)
    {
        // Cache original positions and track who we rewound
        Dictionary<Transform, Vector3> originalPositions = new();
        List<Transform> rewoundPlayers = new();

        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null || clientId == firingClientId)
                continue; // skip shooter

            Transform playerTransform = playerObject.transform;
            originalPositions[playerTransform] = playerTransform.position;

            // Try to rewind to old position
            if (TryGetPlayerPositionAtTick(clientId, firedTick, out Vector3 rewindedPosition))
            {
                playerTransform.position = rewindedPosition;
                rewoundPlayers.Add(playerTransform);
            }
        }

        // Perform hit detection with rewound positions
        // if we ignore the localplayer then we can't detect any player hits (because they're all local on the server)
        // layers and tags are local not networked.
        int hitMask = ~LayerMask.GetMask("Projectiles_Client", "Projectiles_Server");
        bool didHit = Physics.Raycast(rayOrigin, rayDirection, out hitResult, rayRange, hitMask);

        // Restore all rewound players to original positions
        foreach (var transform in rewoundPlayers)
        {
            if (originalPositions.TryGetValue(transform, out Vector3 originalPos))
                transform.position = originalPos;
        }

        return didHit;
    }


    /// <summary>
    /// Retrieves a player’s stored position at a specific tick.
    /// </summary>
    private bool TryGetPlayerPositionAtTick(ulong clientId, ulong targetTick, out Vector3 position)
    {
        position = Vector3.zero;

        if (!playerHistories.TryGetValue(clientId, out PlayerHistory[] historyBuffer))
            return false;

        int bufferIndex = (int)(targetTick % bufferSize);
        if (historyBuffer[bufferIndex].tick == targetTick)
        {
            position = historyBuffer[bufferIndex].position;
            return true;
        }

        return false;
    }
}