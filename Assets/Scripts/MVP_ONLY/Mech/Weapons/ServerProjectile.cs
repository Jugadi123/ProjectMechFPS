using UnityEngine;
using Unity.Netcode;

public class ServerProjectile : NetworkBehaviour
{
    private Rigidbody rb;
    private Vector3 direction;
    private float projectileDamage;
    private float projectileExplosionRadius;
    private float projectileSpeed;
    private ProjectileType projectileType;
    private Vector3 projectileVelocity;
    private int projectileBounces;
    private Vector3 projectileGravity;
    private ulong localClientId;

    public void Initialize(ulong clientId, float mass, float speed, float damage, float explosionRadius, float gravity, ProjectileType type)
    {
        if (!IsServer) return;

        projectileType = type;
        projectileDamage = damage;
        projectileExplosionRadius = explosionRadius;
        projectileSpeed = speed;
        localClientId = clientId;

        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.useGravity = false;
        projectileGravity = new Vector3(0f, gravity, 0f);
        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (projectileType == ProjectileType.Grenade)
        {
            direction = transform.forward;
        }
        else if (projectileType == ProjectileType.Rocket)
        {
            direction = transform.up;
        }
    }
    

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // "hide" the projectile for the firing client

        }
    }

    private void Start()
    {
        if (!IsServer) return;

        if (projectileType == ProjectileType.Grenade)
        {
            projectileVelocity = direction * projectileSpeed;
            rb.AddForce(projectileVelocity, ForceMode.Impulse);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        HandleProjectiles();
    }

    private void HandleProjectiles()
    {
        if (projectileType == ProjectileType.Grenade)
        {
            rb.AddForce(projectileGravity, ForceMode.Acceleration);
        }
        else if (projectileType == ProjectileType.Rocket)
        {
            projectileVelocity = direction * projectileSpeed;
            rb.AddForce(projectileVelocity, ForceMode.Impulse);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {

        if (!IsServer) return;

        if (projectileType == ProjectileType.Grenade)
        {
            if (collision.transform.CompareTag("Player"))
            {
                // get the player's id
                NetworkObject collidedPlayer = collision.GetComponent<NetworkObject>();

                // get the id of the enemy player
                ulong collidedPlayerId = collidedPlayer.OwnerClientId;

                // take damage
                collidedPlayer.GetComponent<HandleHealth>().TakeDamage(projectileDamage, localClientId);

                NetworkObject.Despawn(gameObject);
                return;
            }

            // handle grenade damage
            projectileBounces++;
            if (projectileBounces >= 1)
            {
                // NetworkObject.DeferDespawn(2, true);
            }
        }
        else if (projectileType == ProjectileType.Rocket)
        {
            if (collision.transform.CompareTag("Player"))
            {
                // get the player's id
                NetworkObject collidedPlayer = collision.GetComponent<NetworkObject>();

                // get the id of the enemy player
                ulong collidedPlayerId = collidedPlayer.OwnerClientId;

                // take damage
                collidedPlayer.GetComponent<HandleHealth>().TakeDamage(projectileDamage, localClientId);

                NetworkObject.Despawn(gameObject);
                return;
            }

            NetworkObject.Despawn(gameObject);
        }
    }
}
