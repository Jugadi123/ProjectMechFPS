using System.Collections;
using UnityEngine;

public class handleWeapons : MonoBehaviour
{
    [SerializeField] GameObject primaryMuzzleFlash;
    [SerializeField] GameObject secondaryMuzzleFlash;
    [SerializeField] AudioSource primaryWeaponSound;
    [SerializeField] AudioSource secondaryWeaponSound;
    [SerializeField] GameObject bulletHole;
    [SerializeField] GameObject bulletImpact;
    [SerializeField] GameObject Tracer;
    [SerializeField] GameObject tracerTrail;
    [SerializeField] Transform upperBody;
    [SerializeField] Transform primaryWeapon;
    [SerializeField] Transform secondaryWeapon;
    [SerializeField] Camera mainCamera;
    [SerializeField] Camera cockpitCamera;

    private bool primaryFire;
    private bool secondaryFire;
    [SerializeField] float primaryFireRate;
    [SerializeField] float secondaryFireRate;
    private float primaryFireCoolDown = 0f;
    private float secondaryFireCoolDown = 0f;
    private bool[] mouseClick;
    public int ammo;
    public bool hasEnoughAmmo = true;
    public bool startReloading = false;
    public float reloadTimer = 3.0f;
    public RaycastHit hitInfo;
    public RaycastHit hitInfoLOS;
    private Vector3 rayOrigin;
    private Vector3 rayDirection;
    private bool doPrimaryWeaponVFXNextFrame = false;
    private bool doSecondaryWeaponVFXNextFrame = false;

    void Start()
    {
        primaryMuzzleFlash.SetActive(false);
        secondaryMuzzleFlash.SetActive(false);
        ammo = 10000;
        primaryFireRate = 500f; // rounds per minute
        secondaryFireRate = 50f; // rounds per minute
    }

    Vector3 SpreadDirection(Vector3 direction, float spread) // recoil basically
    {   
        Quaternion randomAngle = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f);
        Vector3 randomized = randomAngle * direction;
        return randomized;
    }

    void Update() // really only for input because server tickrate would run the rest of the game logic
    {
        mouseClick = new bool[] { Input.GetKey(KeyCode.Mouse0), Input.GetKey(KeyCode.Mouse1) };

        secondaryFire = mouseClick[0];
        primaryFire = mouseClick[1];

        if (doPrimaryWeaponVFXNextFrame) {

            doPrimaryWeaponVFXNextFrame = false;

            // only affects visual stuff
            StartCoroutine(FlashMuzzle(primaryMuzzleFlash));

            // RENDER TRACER
            // render tracers every 3 or so bullets for automatic guns for clarity and performance
            // also check if we were holding down the fire button or spamming it like a semi auto weapon. 
            // this ammo and keydown check is only applicable to AUTOMATIC weapons.
            if (ammo % 3 == 0 || Input.GetKeyDown(KeyCode.Mouse0)) {
                // render the visual tracer in the world (for sniper, lasers and other gun alike)
                StartCoroutine(RenderTracer("gun", primaryMuzzleFlash.transform.position, (hitInfo.point + hitInfo.normal * 0.1f), Quaternion.LookRotation(hitInfo.point - primaryMuzzleFlash.transform.position)));
            }

            // handle camera kickback
            cockpitCamera.transform.localRotation *= Quaternion.Euler(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
            mainCamera.transform.localRotation *= Quaternion.Euler(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);


            // handle sound
            if (primaryWeaponSound.isPlaying) {
                primaryWeaponSound.Stop();
            }
            primaryWeaponSound.Play();
        }

        if (doSecondaryWeaponVFXNextFrame) {

            doSecondaryWeaponVFXNextFrame = false;
            
            // only affects visual stuff
            StartCoroutine(FlashMuzzle(secondaryMuzzleFlash));

            // note: the end hit point of the tracer is the same used for both weapons (good or not good?)
            StartCoroutine(RenderTracer("gun", secondaryMuzzleFlash.transform.position, (hitInfo.point + hitInfo.normal * 0.1f), Quaternion.LookRotation(hitInfo.point - secondaryMuzzleFlash.transform.position)));

            // handle camera kickback
            cockpitCamera.transform.localRotation *= Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);
            mainCamera.transform.localRotation *= Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);

            // shot sound
            if (secondaryWeaponSound.isPlaying) {
                secondaryWeaponSound.Stop();
            }
            secondaryWeaponSound.Play();   
        }
    }

    void FixedUpdate()
    {

        hasEnoughAmmo = ammo > 0;

        if (!hasEnoughAmmo) {
            startReloading = true;
            Debug.Log("Reloading...hang on!");
        }

        if (startReloading) {
            reloadTimer -= Time.fixedDeltaTime;
            if (reloadTimer <= 0) {
                ammo = 30;
                startReloading = false;
                reloadTimer = 3.0f;
            }
        }

        rayOrigin = mainCamera.transform.position;

        rayDirection = mainCamera.transform.forward;


        // tracing, drawing line of sight (debugging purposes)
        // int myLOSMask = ~LayerMask.GetMask("Localplayer Mask", "SpawnPoint Mask");
        // bool rayLOSHit = Physics.Raycast(rayOrigin, rayDirection, out hitInfoLOS, 1000f, myLOSMask);
         
        // // if we hit something
        // if (rayLOSHit) {
        //     Debug.DrawRay(rayOrigin, hitInfoLOS.point - rayOrigin, Color.green);
        // }
        // else {
        //     Debug.DrawRay(rayOrigin, rayDirection * 1000f, Color.blue);
        // }




    
        primaryFireCoolDown -= Time.fixedDeltaTime;
        secondaryFireCoolDown -= Time.fixedDeltaTime;

        if (primaryFire) {
            if (primaryFireCoolDown <= 0f && hasEnoughAmmo)
            {
                ammo -= 1;

                // ACTUAL WEAPON RECOIL
                // any recoil logic here before a ray is casted
                rayDirection = SpreadDirection(rayDirection, 0.3f);

                // RAYCASTING LOGIC
                // '~' meaning interact with everything but the specified layers
                int myMask = ~LayerMask.GetMask("Localplayer Mask", "SpawnPoint Mask");
                bool rayHit = Physics.Raycast(rayOrigin, rayDirection, out hitInfo, 1000f, myMask);
         
                // if we hit something
                if (rayHit) {
                    Vector3 hitPointDirection = hitInfo.point - rayOrigin; // after any recoil
                    // debugging purposes
                    Debug.DrawRay(rayOrigin, hitPointDirection, Color.red, 10f);

                    GameObject impact = Instantiate(bulletImpact, hitInfo.point, Quaternion.LookRotation(hitPointDirection));
                    Destroy(impact, 2f);

                    GameObject hole = Instantiate(bulletHole, (hitInfo.point + hitInfo.normal * 0.1f), Quaternion.LookRotation(-hitInfo.normal));

                    Transform objectWeHit = hitInfo.collider.transform;
                    hole.transform.SetParent(objectWeHit);
                    Destroy(hole, 3f);

                    string tag = objectWeHit.tag;
                    if (tag == "Player") {
                        enemy enemy = hitInfo.transform.GetComponent<enemy>();
                        if (enemy != null) {
                            enemy.Takedamage(10f);
                            if (!enemy.isAlive) {
                                // OnPlayeDeath
                                MatchManager.TriggerPlayerDeath(gameObject.name, objectWeHit.name);
                            }
                        }
                    }
                }
                else {
                    // debugging purposes
                    Debug.DrawRay(rayOrigin, rayDirection * 1000f, Color.blue, 3f);
                }

                // handle fire rate
                primaryFireCoolDown = 60f / primaryFireRate;

                // do the visuals later for client
                doPrimaryWeaponVFXNextFrame = true;
            }
        }

        // semi auto logic
        if (secondaryFire) {
            if (hasEnoughAmmo && secondaryFireCoolDown <= 0f)
            {
                ammo -= 1;

                // no recoil for this specific type of gun (sniper). Doesn't make sense. Sway isn't needed either. I just want the player to feel the gun being shot but their accuracy should be 100% unless it's an smg or something.

                // raycasting logic
                // '~' meaning interact with everything but the specified layers
                int myMask = ~LayerMask.GetMask("Localplayer Mask");

                bool rayHit = Physics.Raycast(rayOrigin, rayDirection, out hitInfo, 1000f, myMask);
                // convert a point into direction
         
                // if we hit something
                if (rayHit) {
                    Vector3 hitPointDirection = hitInfo.point - rayOrigin;
                    Debug.DrawRay(rayOrigin, hitPointDirection, Color.red, 10f);

                    GameObject impact = Instantiate(bulletImpact, hitInfo.point, Quaternion.LookRotation(hitPointDirection));
                    Destroy(impact, 2f);

                    GameObject hole = Instantiate(bulletHole, (hitInfo.point + hitInfo.normal * 0.1f), Quaternion.LookRotation(-hitInfo.normal));

                    Transform objectWeHit = hitInfo.collider.transform;
                    hole.transform.SetParent(objectWeHit);
                    Destroy(hole, 3f);
                }
                else {
                    Debug.DrawRay(rayOrigin, rayDirection * 1000f, Color.blue, 3f);
                }
                
                // handle fire rat
                secondaryFireCoolDown = 60f / secondaryFireRate;

                // do the visuals later for client
                doSecondaryWeaponVFXNextFrame = true;
            }
        }
    }

    IEnumerator RenderTracer(string weaponType, Vector3 origin, Vector3 endPoint, Quaternion lookRotation)
    {
        GameObject tracer = Instantiate(Tracer, origin, lookRotation);
        tracer.transform.localScale = new Vector3(1f, 1f, 5f);

        while (Vector3.Distance(tracer.transform.position, endPoint) > 0.1f)
        {
            tracer.transform.position = Vector3.MoveTowards(tracer.transform.position, endPoint, Time.deltaTime * 50f);
            yield return null;
        }
 
        Destroy(tracer);
    }

    IEnumerator FlashMuzzle(GameObject muzzleFlash)
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        muzzleFlash.SetActive(false);
    }
}
