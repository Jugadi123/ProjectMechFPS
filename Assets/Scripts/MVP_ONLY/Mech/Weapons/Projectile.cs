using UnityEngine;
using System.Collections.Generic;
public class Projectile : MonoBehaviour
{
    // global projectile variables
    private Rigidbody rb;
    private Collider projectileCollider;
    private Vector3 direction;
    private float projectileSpeed;
    private ProjectileType projectileType;
    private Vector3 projectileVelocity;
    public LineRenderer lineRenderer;
    private Vector3 projectileGravity;
    private int projectileBounces;

    public void Initialize(float mass, float speed, float gravity, ProjectileType type)
    {
        rb = GetComponent<Rigidbody>();
        projectileType = type;
        projectileSpeed = speed;
        rb.mass = mass;
        rb.useGravity = false;
        projectileGravity = new Vector3(0f, gravity, 0f);
        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        projectileCollider = GetComponent<Collider>();
        projectileCollider.isTrigger = false;
        // Store initial direction
        if (projectileType == ProjectileType.Grenade)
        {
            direction = transform.forward;
        }
        else if (projectileType == ProjectileType.Rocket)
        {
            direction = transform.up;
        }
    }


    private void Start()
    {
        // ignore the server's projectile. we dont want the client's predicted projectile to collide with the server's projectile
        int serverProjectileLayer = LayerMask.NameToLayer("Server_Projectile");
        Physics.IgnoreLayerCollision(gameObject.layer, serverProjectileLayer);

        if (projectileType == ProjectileType.Grenade)
        {
            // launch right away
            projectileVelocity = direction * projectileSpeed;
            rb.AddForce(projectileVelocity, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        HandleProjectiles();
    }

    private void HandleProjectiles()
    {
        if (projectileType == ProjectileType.Rocket)
        {
            projectileVelocity = direction * projectileSpeed;
            rb.AddForce(projectileVelocity, ForceMode.Impulse);
        }

        if (projectileType == ProjectileType.Grenade)
        {
            rb.AddForce(projectileGravity, ForceMode.Acceleration);
            ShowTrajectory(transform.position, rb.linearVelocity, projectileSpeed, projectileGravity.y, projectileType);
        }

    }


    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collision: {collision.transform.tag}");

        if (collision.transform.CompareTag("Player"))
        {
            Destroy(gameObject); // destroy the projectile
            return;
        }

        if (projectileType == ProjectileType.Grenade)
        {
            // handle bouncing
            projectileBounces++;
            if (projectileBounces >= 1)
            {
                
            }
        }
        else if (projectileType == ProjectileType.Rocket)
        {
            // handle collision
            Destroy(gameObject); // destroy the projectile
        }

    }


    public static List<Vector3> SimulateTrajectory(Vector3 startPos, Vector3 initialVelocity, float gravityY, float stepTime, int maxSteps, ProjectileType projectileType)
    {
        List<Vector3> points = new List<Vector3>();
        Vector3 position = startPos;
        Vector3 velocity = initialVelocity;
        Vector3 gravity = new Vector3(0f, gravityY, 0f);

        for (int i = 0; i < maxSteps; i++)
        {
            points.Add(position);

            velocity += gravity * stepTime;
            position += velocity * stepTime;

            // Optional: raycast to detect early impact
            if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, velocity.magnitude * stepTime, layerMask: ~LayerMask.GetMask("LocalPlayer")))
            {
                if (projectileType == ProjectileType.Grenade)
                {
                    Vector3 reflected = Vector3.Reflect(velocity, hit.normal);
                    velocity = reflected * 0.1f;
                }
            }
        }

        return points;
    }

    private void ShowTrajectory(Vector3 startPos, Vector3 direction, float speed, float gravityY, ProjectileType projectileType)
    {
        Vector3 velocity = direction.normalized * speed;

        List<Vector3> points = SimulateTrajectory(startPos, velocity, gravityY, 0.1f, 100, projectileType);

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

} 