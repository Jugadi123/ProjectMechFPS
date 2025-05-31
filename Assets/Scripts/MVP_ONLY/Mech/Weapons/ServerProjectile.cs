using UnityEngine;
using Unity.Netcode;

public class ServerProjectile : NetworkBehaviour
{
    private Rigidbody rb;
    private Collider projectileCollider;
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
        projectileCollider = GetComponent<Collider>();
        projectileCollider.isTrigger = false;

        if (projectileType == ProjectileType.Grenade)
        {
            direction = transform.forward;
        }
        else if (projectileType == ProjectileType.Rocket)
        {
            direction = transform.up;
        }
    }

    // only for clients unless running in HOST mode
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // "hide" the projectile for the firing client
            GetComponent<MeshRenderer>().enabled = false;

            
        }
    }

    private void Start()
    {

        if (!IsServer) return;

        // holy fuck it took me an entire day to figure this out
        // like bro
        // Why would we bounce a projectile off of a player in the first place?
        // If it's a direct hit on a player, then we should just destroy the projectile and deal damage to the player.
        // wtf ?????? 12:37 AM....
        // WE DIDN'T NEED TO IGNORE THE PLAYER COLLISIONS IN THE FIRST PLACE

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

    private void OnCollisionEnter(Collision collision)
    {

        if (!IsServer) return;

        if (projectileType == ProjectileType.Grenade)
        {
            if (collision.transform.CompareTag("Player"))
            {
                // get the player's id
                NetworkObject collidedPlayer = collision.collider.GetComponent<NetworkObject>();

                // get the id of the enemy player
                ulong collidedPlayerId = collidedPlayer.OwnerClientId;

                // if (collidedPlayerId == localClientId) return;

                // take damage
                collidedPlayer.GetComponent<HandleHealth>().TakeDamage(projectileDamage, localClientId);

                NetworkObject.Despawn(gameObject);
                return;
            }

            // handle grenade damage
            projectileBounces++;
            if (projectileBounces >= 1)
            {
              
            }
        }
        else if (projectileType == ProjectileType.Rocket)
        {
            if (collision.transform.CompareTag("Player"))
            {
                // get the player's id
                NetworkObject collidedPlayer = collision.collider.GetComponent<NetworkObject>();

                // get the id of the enemy player
                ulong collidedPlayerId = collidedPlayer.OwnerClientId;

                // take damage
                collidedPlayer.GetComponent<HandleHealth>().TakeDamage(projectileDamage, localClientId);



                Debug.Log("Collided with player " + collidedPlayerId);

                NetworkObject.Despawn(gameObject);
                return;
            }

            NetworkObject.Despawn(gameObject);
        }
    }
}
