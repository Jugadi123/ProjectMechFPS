using UnityEngine;

public class missileProjectile : MonoBehaviour
{
    private Rigidbody rb;

    public float maxRange = 100f;
    private Vector3 origin;
    private Vector3 direction;
    
    public float missileSpeed = 5f;
    public Vector3 currentVelocity;
    public RaycastHit hitInfo;

    // some notes

    // we will come back to destructive environment destruction later (using rigidbody physics to break or push things)
    // we will also be using particle collision to do damage in the area nearby rather than using a seperate collider.

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 0.7f;
        origin = transform.position;
        direction = transform.up; // for some reason due to the mesh rotation being fucked
        // Debug.DrawRay(origin, direction * 1000f, Color.green, 3f);
    }

    void FixedUpdate()
    {
        rb.AddForce(direction * missileSpeed, ForceMode.Force);
    }


    void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.tag == "Player") {
            // damage logic
      
            
            Destroy(gameObject);
            return;
        }
        
        Destroy(gameObject);
    }

}
