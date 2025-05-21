
using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class DeathManager : NetworkBehaviour
{

    // get my health netvar and listen for value changes
    private PlayerNetvars playerNetvars;
    public Canvas playerUICanvasManager;
    private HandleMovement handleMovement;
    private WeaponHandler weaponHandler;
    private HandleRotation handleRotation;
    private CameraEffects cameraEffects;
    private HandleLagCompensation handleLagCompensation;
    private HandleHealth handleHealth;
    public ulong attackerId;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera cockpitCamera;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerNetvars = GetComponent<PlayerNetvars>();
        handleMovement = GetComponent<HandleMovement>();
        weaponHandler = GetComponent<WeaponHandler>();
        handleRotation = GetComponent<HandleRotation>();
        cameraEffects = GetComponent<CameraEffects>();
        handleLagCompensation = GetComponent<HandleLagCompensation>();
        handleHealth = GetComponent<HandleHealth>();

        playerNetvars.health.OnValueChanged += OnHealthChanged; // listen for health changes
    }

    public void SetAttackerId(ulong receivedAttackerId)
    {
        attackerId = receivedAttackerId;
        Debug.Log($"Attacker Id: {attackerId}");
    }


    private void OnHealthChanged(float oldHealth, float newHealth) // run death only once we die
    {
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
        // disable health
        handleHealth.enabled = false;
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


    private IEnumerator RunDeathCam(float duration)
    {
        // Unparent the camera so it's not affected by the ragdoll
        playerCamera.transform.SetParent(null);

        // Offset values
        Vector3 offset = transform.forward * 3f + transform.up * 3f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (transform == null)
                yield break; // stop if player is destroyed

            // Calculate desired position in world space
            Vector3 targetPosition = transform.position + offset;

            // Move camera to the offset position
            playerCamera.transform.position = targetPosition;

            // Make camera look at the player's current position
            Vector3 lookDirection = transform.position - playerCamera.transform.position;
            playerCamera.transform.rotation = Quaternion.LookRotation(lookDirection);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}