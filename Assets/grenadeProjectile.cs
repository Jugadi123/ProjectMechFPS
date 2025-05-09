using UnityEngine;
using System.Collections;
public class grenadeProjectile : MonoBehaviour
{
    // difference between this and a missile is that this will bounce off of objects in the world and will expire (explode) after x amount of time. The expiration will trigger a particle explosion and from their it's knockbacks & AOE, etc.

    // however if it hits a player or another mech directly or indirectly, we will apply a lot of damage to the player.
    private Rigidbody rb;
    private float timeElapsedSinceLaunch;
    private float timeElapsedSinceFirstBounce;
    
    private bool StartExplosionTimer = false;
    private bool RightClick = false;

    public float launchSpeed;
    public float expiryTime;
    public float timeBeforeCanManuallyExplode;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * launchSpeed, ForceMode.Impulse);
        timeElapsedSinceFirstBounce = 0f;
        timeElapsedSinceLaunch = 0f;
    }

    void OnCollisionEnter(Collision collision)
    {
        // detect first hit

        if (collision.transform.tag == "Player") {
            // damage logic


            Destroy(gameObject);
            return;
        }

        if (!StartExplosionTimer) {
            StartExplosionTimer = true;
        }
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(1)) {
            RightClick = true;
        }
    }

    void FixedUpdate()
    {

        // // check for another right click after x time to explode the nade mid air
        // // waiting because we don't want to instantly destroy the grenade (i.e if someone would like to spam their nades)


        timeElapsedSinceLaunch += Time.fixedDeltaTime;
        if (timeElapsedSinceLaunch > timeBeforeCanManuallyExplode) {
            if (RightClick) {
                timeElapsedSinceLaunch = 0f;
                Destroy(gameObject);
                return;
            }
        }

        // checking for any collisions or mid air explosions, etc (server tick rate)
        if (StartExplosionTimer) {
            timeElapsedSinceFirstBounce += Time.fixedDeltaTime;
            if (timeElapsedSinceFirstBounce > expiryTime) {
                Destroy(gameObject);
                timeElapsedSinceFirstBounce = 0f;
                StartExplosionTimer = false;
            }
            else {
                // Debug.Log(timeElapsedSinceFirstBounce);
            }
        }
    }
}
