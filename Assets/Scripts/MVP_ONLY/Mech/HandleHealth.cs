
using UnityEngine;
using Unity.Netcode;
public class HandleHealth : NetworkBehaviour
{
    public Canvas playerUICanvasManager;
    private PlayerNetvars playerNetvars;
    private DeathManager deathManager;
    private HandleMovement handleMovement;
    private WeaponHandler weaponHandler;
    private HandleRotation handleRotation;
    private CameraEffects cameraEffects;
    private HandleLagCompensation handleLagCompensation;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera cockpitCamera;

    private ulong attackerId;

    public override void OnNetworkSpawn()
    {
        playerNetvars = GetComponent<PlayerNetvars>();
        deathManager = GetComponent<DeathManager>();
        handleMovement = GetComponent<HandleMovement>();
        weaponHandler = GetComponent<WeaponHandler>();
        handleRotation = GetComponent<HandleRotation>();
        cameraEffects = GetComponent<CameraEffects>();
        handleLagCompensation = GetComponent<HandleLagCompensation>();
        playerNetvars.health.OnValueChanged += OnHealthChanged; // listen for health changes
    }


    private void OnHealthChanged(float oldHealth, float newHealth) // run death only once we die
    {
        if (!IsOwner) return;
        if (oldHealth > 0f && newHealth <= 0f) // only run death ONCE we die.
        {
            OnDeath();
            // StartCoroutine(RunDeathCam(5f));
        }
    }


    private void OnDeath()
    {
        // disable player ui
        playerUICanvasManager.enabled = false;
        // disable movement
        // handleMovement.enabled = false;
        // disable weapon
        weaponHandler.enabled = false;
        // disable mouse rotation
        // handleRotation.enabled = false;
        // disable camera effects
        cameraEffects.enabled = false;
        // disable lag compensation
        handleLagCompensation.enabled = false;
        // // disable health
        // this.enabled = false;
        // disable cockpit camera
        cockpitCamera.enabled = false;

        // request server to despawn us
        SendDespawnRequestServerRpc();
    }

    [ServerRpc]
    private void SendDespawnRequestServerRpc()
    {
        NetworkObject.Despawn(gameObject);
    }


    // only runs on the server
    internal void TakeDamage(float damage, ulong clientIdOfAttacker)
    {
        // run only if player is alive and has health by server
        if (!IsServer) return;

        if (playerNetvars.health.Value > 0)
        {
            attackerId = clientIdOfAttacker;
            playerNetvars.health.Value -= damage;
        }
    }
}