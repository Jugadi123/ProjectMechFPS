using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 origin;
    private Vector3 direction;
    private float damage;
    private float explosionRadius;
    private float gravity;
    
    // Projectile settings
    public float maxRange = 100f;
    public float projectileSpeed = 5f;
    public Vector3 currentVelocity;
    
    public void Initialize(Vector3 direction, float speed, float damage, float explosionRadius, float gravity)
    {
        this.direction = direction.normalized;
        this.damage = damage;
        this.explosionRadius = explosionRadius;
        this.projectileSpeed = speed;
        this.gravity = gravity;
        // Setup rigidbody
        rb = GetComponent<Rigidbody>();
        rb.mass = 0.7f;
        origin = transform.position;
        
        // Store initial direction
        this.direction = transform.up; // Using up direction due to mesh rotation
        
        // Destroy after 10 seconds in case it never hits anything
        Destroy(gameObject, 10f);
    }
    
    void FixedUpdate()
    {
        // Apply constant force in the direction

        Vector3 velocity = direction * projectileSpeed;
        velocity.y += gravity * NetworkTimer.Singleton.GetTickInterval(); // use tick interval
        rb.AddForce(velocity, ForceMode.Force);

    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.CompareTag("Player")) 
        {
            // TODO: Implement damage logic
            // DamageManager.ApplyDamage(collision.gameObject, damage);
        }
        
        // Handle explosion if needed
        if (explosionRadius > 0)
        {
            // TODO: Implement explosion logic
            // Spawn explosion effects
            // Apply area damage
        }
        
        Destroy(gameObject);
    }
} 