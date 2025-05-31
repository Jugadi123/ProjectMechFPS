// master script for player/mech
using UnityEngine;
using Unity.Netcode;

public class MechPlayerManager : NetworkBehaviour
{


    [SerializeField] private GameObject playerCockpitMesh;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera playerCockpitCamera;
    [SerializeField] private Canvas playerUICanvas;

    private void EnableLocalPlayerView()
    {
        playerUICanvas.enabled = true;
        playerCockpitMesh.SetActive(true);
        playerCamera.enabled = true;
        playerCockpitCamera.enabled = true;

        playerCamera.cullingMask = ~LayerMask.GetMask("CockpitView", "SpawnPoint Mask", "PlayerBody");
        playerCockpitCamera.cullingMask = LayerMask.GetMask("CockpitView");
    }

    private void DisableRemoteView()
    {
        playerUICanvas.enabled = false;
        playerCockpitMesh.SetActive(false);
        playerCamera.enabled = false;
        playerCockpitCamera.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
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

}