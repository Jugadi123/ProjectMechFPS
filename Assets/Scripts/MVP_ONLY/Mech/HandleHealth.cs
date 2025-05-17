
using UnityEngine;
using Unity.Netcode;
public class HandleHealth : NetworkBehaviour
{
    private PlayerNetvars playerNetvars;
    private DeathManager deathManager;

    public override void OnNetworkSpawn()
    {
        playerNetvars = GetComponent<PlayerNetvars>();
        deathManager = GetComponent<DeathManager>();
    }

    public void TakeDamage(float damage, ulong clientIdOfAttacker)
    {
        // run only if player is alive and has health by server
        if (IsServer && playerNetvars.health.Value > 0)
        {
            playerNetvars.health.Value -= damage;
        }
    }
}