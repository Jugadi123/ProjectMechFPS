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
            // GetComponent<MeshRenderer>().enabled = false;


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

        Vector3 explosionPos = transform.position;

        // Damage logic (existing, only affects direct hits)
        if (collision.transform.CompareTag("Player"))
        {
            NetworkObject player = collision.collider.GetComponent<NetworkObject>();
            player.GetComponent<HandleHealth>().TakeDamage(projectileDamage, localClientId);
        }

        // Explosion physics (new)
        if (projectileType == ProjectileType.Rocket)
        {
            // Get all players in blast radius (excluding yourself if desired)
            Collider[] hits = Physics.OverlapSphere(explosionPos, projectileExplosionRadius, LayerMask.GetMask("Player"));

            foreach (Collider hit in hits)
            {
                NetworkObject playerObj = hit.GetComponent<NetworkObject>();
                if (playerObj != null && playerObj.OwnerClientId != localClientId) // Optional: exclude self
                {
                    ApplyExplosionForceClientRpc(
                        playerObj.OwnerClientId,
                        explosionPos,
                        projectileExplosionRadius,
                        projectileDamage * 0.8f // Scale force separately from damage
                    );
                }
            }

            // Always apply self-knockback if within radius
            if (Vector3.Distance(explosionPos, NetworkManager.Singleton.ConnectedClients[localClientId].PlayerObject.transform.position) <= projectileExplosionRadius)
            {
                ApplyExplosionForceClientRpc(
                    localClientId,
                    explosionPos,
                    projectileExplosionRadius,
                    projectileDamage * 1.2f // Stronger self-knockback
                );
            }
        }

        NetworkObject.Despawn(gameObject);
    }


    [ClientRpc]
    private void ApplyExplosionForceClientRpc(ulong firingClientId, Vector3 explosionPos, float radius, float maxForce)
    {
        if (!IsOwner || OwnerClientId != firingClientId) return;

        Vector3 playerPos = transform.position;
        float distance = Vector3.Distance(explosionPos, playerPos);

        // Calculate spherical force direction (away from explosion center)
        Vector3 forceDir = (playerPos - explosionPos).normalized;
        
        // Inverse-square falloff (stronger near center)
        float forcePercent = 1 - Mathf.Clamp01(distance / radius);
        float actualForce = maxForce * forcePercent * forcePercent; // Quadratic falloff

        NetworkObject firingPlayer = NetworkManager.Singleton.ConnectedClients[firingClientId].PlayerObject;

        firingPlayer.GetComponent<HandleMovement>().RunExplosionForce(forceDir, actualForce);


        // DebugDrawExplosion(explosionPos, radius, 5f);
    }
    


    void DebugDrawExplosion(Vector3 center, float radius, float duration)
    {
        // Draw explosion radius
        Debug.DrawRay(center, Vector3.up * radius, Color.yellow, duration);
        Debug.DrawRay(center, Vector3.down * radius, Color.yellow, duration);
        Debug.DrawRay(center, Vector3.left * radius, Color.yellow, duration);
        Debug.DrawRay(center, Vector3.right * radius, Color.yellow, duration);
        Debug.DrawRay(center, Vector3.forward * radius, Color.yellow, duration);
        Debug.DrawRay(center, Vector3.back * radius, Color.yellow, duration);

        // Draw sample force directions (8 cardinal directions)
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)).normalized;
            Debug.DrawRay(center, dir * radius, Color.red, duration);
        }
    }
}
