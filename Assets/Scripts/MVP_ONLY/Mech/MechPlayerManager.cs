// master script for player/mech
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class MechPlayerManager : NetworkBehaviour
{
    [SerializeField] Transform weaponsWrapper;
    [SerializeField] Transform lowerBody;
    [SerializeField] private GameObject playerCockpitMesh;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera playerCockpitCamera;
    [SerializeField] private Canvas playerUICanvas;

    private Vector3 savedScale;
    private Vector3 spawnPosition;

    public bool IsRespawning = false;


    private void EnableLocalPlayerView()
    {
        playerUICanvas.enabled = true;
        playerCockpitMesh.SetActive(true);
        playerCamera.enabled = true;
        playerCockpitCamera.enabled = true;


        int localWeaponsLayer = LayerMask.NameToLayer("Mech Weapons");
        int playerBodyLayer = LayerMask.NameToLayer("PlayerBody");
        int cockpitLayer = LayerMask.NameToLayer("CockpitView");


        playerCockpitMesh.gameObject.layer = cockpitLayer;
        lowerBody.gameObject.layer = playerBodyLayer;
        weaponsWrapper.gameObject.layer = localWeaponsLayer;

        // Transform[] allChildrenOfWeapons = weaponsWrapper.GetComponentsInChildren<Transform>(true);

        // foreach (Transform child in allChildrenOfWeapons)
        // {
        //     if (child != weaponsWrapper.transform) // skip the root itself because its layer has already been assigned above
        //         Debug.Log(child.gameObject.name);
        // }

        // direct children access for now
        foreach (Transform child in weaponsWrapper.transform)
        {
            child.gameObject.layer = localWeaponsLayer;
        }

        playerCamera.cullingMask = ~LayerMask.GetMask("CockpitView", "SpawnPoint Mask", "PlayerBody", "Mech Weapons");
        playerCockpitCamera.cullingMask = LayerMask.GetMask("CockpitView", "Mech Weapons");
    }

    private void DisableRemoteView()
    {
        playerUICanvas.enabled = false;
        playerCockpitMesh.SetActive(false);
        playerCamera.enabled = false;
        playerCockpitCamera.enabled = false;
    }

    private void HandleRespawning()
    {
        if (Input.GetKey(KeyCode.R) && !IsRespawning)
        {
            IsRespawning = true;
            Debug.Log("Requesting server to respawn...");
            SendRespawnRequestToServerRpc(OwnerClientId);
        }
    }


    [ServerRpc]
    private void SendRespawnRequestToServerRpc(ulong clientID)
    {
        if (clientID == OwnerClientId)
        {
            NetworkObject player = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
            Debug.Log($"Respawning {player.name}");


            // Reset movement state on server side first
            HandleMovement movement = player.GetComponent<HandleMovement>();
            if (movement != null)
            {
                movement.ResetMovementState();
            }

            TeleportClientRpc(spawnPosition, Quaternion.Euler(0, -55, 0));
        }

    }


    [ClientRpc]
    private void TeleportClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner) return; // Only update on owning client

        // Get the HandleMovement component
        HandleMovement movement = GetComponent<HandleMovement>();
        if (movement != null)
        {
            movement.ResetMovementState();
        }

        GetComponent<AnticipatedNetworkTransform>().Teleport(position, rotation, savedScale);

        GetComponent<CharacterController>().enabled = true;

        IsRespawning = false;
    }



    private void Awake()
    {
        savedScale = transform.localScale;
    }


    public override void OnNetworkSpawn()
    {
        NetworkTimer.OnTick += OnServerTick;

        // get spawn point at the start by the server to it can respawn player when it requests
        GameObject spawnObject = GameObject.FindWithTag("Respawn");

        if (spawnObject == null)
        {
            Debug.LogError("Couldn't find a spawn location.");
            return;
        }

        spawnPosition = spawnObject.transform.position;

        if (IsOwner)
        {
            gameObject.layer = LayerMask.NameToLayer("LocalPlayer");
            EnableLocalPlayerView();
        }
        else if (!IsServer)
        {
            gameObject.layer = LayerMask.NameToLayer("RemotePlayer");
            DisableRemoteView();
        }


        if (IsServer)
        {
            // server-side setup
            playerUICanvas.enabled = false;
            playerCockpitMesh.SetActive(false);
            playerCamera.enabled = false;
            playerCockpitCamera.enabled = false;
        }
    }


    public override void OnNetworkDespawn()
    {
        NetworkTimer.OnTick -= OnServerTick;
    }


    private void OnServerTick()
    {
        if (IsOwner)
        {
            HandleRespawning();
        }
    }

}