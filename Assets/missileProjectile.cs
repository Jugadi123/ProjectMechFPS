using UnityEngine;

public class missileProjectile : MonoBehaviour
{
    private Rigidbody rb;

    public float maxRange = 100f;
    private Vector3 origin;
    private Vector3 direction;
    
    public float missileSpeed = 5f;
    public Vector3 currentVelocity;

    // we will come back to destructive environment destruction later (using rigidbody physics to break or push things)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        origin = transform.position;
        direction = transform.up; // for some reason due to the mesh rotation being fucked

        Debug.DrawRay(origin, direction, Color.red, 5f);

    }

    void FixedUpdate()
    {
        rb.AddForce(direction * missileSpeed, ForceMode.Force);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with: " + collision.gameObject.name);
        Destroy(gameObject);
    }

}
