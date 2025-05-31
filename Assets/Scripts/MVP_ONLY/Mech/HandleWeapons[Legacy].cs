using System.Collections;
using UnityEngine;
public class HandleWeapons : MonoBehaviour
{

    [SerializeField] GameObject primaryMuzzleFlash;
    [SerializeField] GameObject secondaryMuzzleFlash;
    [SerializeField] AudioSource primaryWeaponSound;
    [SerializeField] AudioSource secondaryWeaponSound;
    [SerializeField] GameObject bulletHole;
    [SerializeField] GameObject bulletImpact;
    [SerializeField] GameObject Tracer;
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
    public RaycastHit hitInfo;
    public RaycastHit hitInfoLOS;
    

    private Vector3 rayOrigin;
    private Vector3 rayDirection;
    private bool doPrimaryWeaponVFXNextFrame = false;
    private bool doSecondaryWeaponVFXNextFrame = false;


    [Header("Overheating Related")]
    public float maxHeat;
    public float primaryCurrentHeat;
    public float secondaryCurrentHeat;
    public bool PrimaryOverheated = false;
    public bool SecondaryOverheated = false;
    public float primaryHeatCoolDown;
    public float secondaryHeatCoolDown;

    public int shotID;

    public GameObject missilePrefab;
    public GameObject grenadePrefab;

    // temp variable
    public float currentWeaponSpread;
    public float maxSpread = 2.0f;


    void Start()
    {
        primaryHeatCoolDown = 3f;
        secondaryHeatCoolDown = 3f;
        primaryCurrentHeat = 0f;
        secondaryCurrentHeat = 0f;
        maxHeat = 100f;
        shotID = 0;
        primaryMuzzleFlash.SetActive(false);
        secondaryMuzzleFlash.SetActive(false);
        primaryFireRate = 500f; // rounds per minute
        secondaryFireRate = 50f; // rounds per minute
    }

    Vector3 SpreadDirection(Vector3 direction, float spread) // recoil basically
    {   
        // currentWeaponSpread = spread;
        Quaternion spreadAngle = Quaternion.Euler(Random.Range(-currentWeaponSpread, currentWeaponSpread), Random.Range(-currentWeaponSpread, currentWeaponSpread), 0f);
        return spreadAngle * direction;
    }

    void Update() // really only for input because server tickrate would run the rest of the game logic
    {

        primaryFire = Input.GetKey(KeyCode.Mouse0);
        secondaryFire = Input.GetKey(KeyCode.Mouse1);

        if (doPrimaryWeaponVFXNextFrame) {

            doPrimaryWeaponVFXNextFrame = false;

            // only affects visual stuff
            StartCoroutine(FlashMuzzle(primaryMuzzleFlash));

            // RENDER TRACER
            // render tracers every 3 or so bullets for automatic guns for clarity and performance
            // also check if we were holding down the fire button or spamming it like a semi auto weapon. 
            // this ammo and keydown check is only applicable to AUTOMATIC weapons.
            if (shotID % 3 == 0 || Input.GetKeyDown(KeyCode.Mouse0)) {
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

        rayOrigin = mainCamera.transform.position;

        rayDirection = mainCamera.transform.forward;
    
        primaryFireCoolDown -= Time.fixedDeltaTime;
        secondaryFireCoolDown -= Time.fixedDeltaTime;


        // primary fire logic (if it was semi automatic)
        // always keep reducing heat if the gun isn't fully overheated
        if (!PrimaryOverheated) {
            primaryCurrentHeat -= Time.fixedDeltaTime * 2f;
        }

        // overheat cooldown management
        if (PrimaryOverheated) {
            primaryHeatCoolDown -= Time.fixedDeltaTime;
            if (primaryHeatCoolDown <= 0f) {
                PrimaryOverheated = false; // stop the cool down from continuing
                primaryCurrentHeat = 0f; // reset heat
                primaryHeatCoolDown = 3f; // reset the cooldown
            }
        }


        // secondary fire logic (if it was semi automatic)
        // always keep reducing heat if the gun isn't fully overheated
        if (!SecondaryOverheated) {
            secondaryCurrentHeat -= Time.fixedDeltaTime * 2f;
        }

        // overheat cooldown management
        if (SecondaryOverheated) {
            secondaryHeatCoolDown -= Time.fixedDeltaTime;
            if (secondaryHeatCoolDown <= 0f) {
                SecondaryOverheated = false; // stop the cool down from continuing
                secondaryCurrentHeat = 0f; // reset heat
                secondaryHeatCoolDown = 3f; // reset the cooldown
            }
        }

        // '~' meaning interact with everything but the specified layers
        int myMask = ~LayerMask.GetMask("Localplayer Mask");



        // primary fire logic
        if (primaryFire) {
            if (primaryFireCoolDown <= 0f)
            {

                if (PrimaryOverheated) {
                    return;
                }

                // increment heat if not overheated
                if (!PrimaryOverheated) {
                    primaryCurrentHeat += 0.03f * maxHeat;
                    if (primaryCurrentHeat >= maxHeat) {
                        PrimaryOverheated = true;
                    }
                }

                // increment shotID
                shotID++;

                // ACTUAL WEAPON RECOIL
                // any recoil logic here before a ray is casted
                rayDirection = SpreadDirection(rayDirection, 0.3f);

                // RAYCASTING LOGIC
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
                        // damage logic
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

        if (secondaryFire) {
            if (secondaryFireCoolDown <= 0f)
            {

                if (SecondaryOverheated) {
                    return;
                }

                // increment heat by 20% if not overheated
                if (!SecondaryOverheated) {
                    secondaryCurrentHeat += 0.10f * maxHeat;
                    if (secondaryCurrentHeat >= maxHeat) {
                        SecondaryOverheated = true;
                    }
                }

                // ----  ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- HITSCANS



                // // no recoil for this specific type of gun (sniper). Doesn't make sense. Sway isn't needed either. I just want the player to feel the gun being shot but their accuracy should be 100% unless it's an smg or something.
                // bool rayHit = Physics.Raycast(rayOrigin, rayDirection, out hitInfo, 1000f, myMask);
                // // convert a point into direction
         
                // // if we hit something
                // if (rayHit) {
                //     Vector3 hitPointDirection = hitInfo.point - rayOrigin;
                //     Debug.DrawRay(rayOrigin, hitPointDirection, Color.red, 10f);

                //     GameObject impact = Instantiate(bulletImpact, hitInfo.point, Quaternion.LookRotation(hitPointDirection));
                //     Destroy(impact, 2f);

                //     GameObject hole = Instantiate(bulletHole, (hitInfo.point + hitInfo.normal * 0.1f), Quaternion.LookRotation(-hitInfo.normal));

                //     Transform objectWeHit = hitInfo.collider.transform;
                //     hole.transform.SetParent(objectWeHit);
                //     Destroy(hole, 3f);


                //     string tag = objectWeHit.tag;
                //     if (tag == "Player") {
                //         enemy enemy = hitInfo.transform.GetComponent<enemy>();
                //         if (enemy != null) {
                //             enemy.Takedamage(35f);

                //             Debug.Log(enemy.health);
                //             if (!enemy.isAlive) {
                //                 // OnPlayeDeath
                //                 MatchManager.TriggerPlayerDeath(gameObject.name, objectWeHit.name);
                //             }
                //         }
                //     }

                // }
                // else {
                //     Debug.DrawRay(rayOrigin, rayDirection * 1000f, Color.blue, 3f);
                // }



                // ----  ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- HITSCANS



                // SPAWN MISSILE

                // int mask = ~LayerMask.GetMask("Projectiles&Bullets");
                // bool rayHit = Physics.Raycast(rayOrigin, rayDirection, out hitInfo, 1000f, mask);
                // // convert a point into direction
         
                // // if we hit something
                // if (rayHit) {
                //     Vector3 hitPointDirection = hitInfo.point - rayOrigin;
                //     Debug.DrawRay(rayOrigin, hitPointDirection, Color.red, 3f);

                //     float spawnOffset = 0.5f;
                //     SpawnProjectile(missilePrefab, secondaryMuzzleFlash.transform.position + (rayDirection.normalized * spawnOffset), hitInfo.point - secondaryMuzzleFlash.transform.position);

                // } else {
                //     Debug.DrawRay(rayOrigin, rayDirection * 1000f, Color.blue, 3f);
                // }



                // SPAWN GRENADE

                

                float spawnOffset = 0.5f;

                Vector3 grenadeStartPosition = secondaryMuzzleFlash.transform.position + (rayDirection * spawnOffset);

                int mask = ~LayerMask.GetMask("Projectiles&Bullets");
                bool rayHit = Physics.Raycast(rayOrigin, rayDirection, out hitInfo, 1000f, mask);

                Vector3 grenadeFacingDirection = Vector3.zero;
         
                // if we hit something
                if (rayHit) {

                    Vector3 direction = hitInfo.point - grenadeStartPosition;
                    grenadeFacingDirection = direction;

                    Debug.DrawRay(grenadeStartPosition, direction, Color.green, 5f);

                    Debug.DrawRay(rayOrigin, hitInfo.point - rayOrigin, Color.red, 5f);

                } else {
                    Debug.DrawRay(rayOrigin, rayDirection * 1000f, Color.blue, 5f);

                    Vector3 directionOffset = rayOrigin + rayDirection * 1000f;
                    grenadeFacingDirection = directionOffset - grenadeStartPosition;

                    Debug.DrawRay(grenadeStartPosition, grenadeFacingDirection, Color.green, 5f);
                }

                SpawnGrenade(grenadePrefab, grenadeStartPosition, grenadeFacingDirection);


                // handle fire rat
                secondaryFireCoolDown = 60f / secondaryFireRate;

                // do the visuals later for client
                doSecondaryWeaponVFXNextFrame = true;
            }
        }

        primaryCurrentHeat = Mathf.Clamp(primaryCurrentHeat, 0f, maxHeat);
        secondaryCurrentHeat = Mathf.Clamp(secondaryCurrentHeat, 0f, maxHeat);

    }


    void SpawnGrenade(GameObject prefab, Vector3 startPosition, Vector3 direction) {
        // GameObject projectile = Instantiate(prefab, startPosition, Quaternion.LookRotation(direction), transform); // version with the parent being assigned for custom projectile motion (getting the launch angle)
        GameObject projectile = Instantiate(prefab, startPosition, Quaternion.LookRotation(direction));
    }

    void SpawnProjectile(GameObject projectilePrefab, Vector3 position, Vector3 direction) {
        Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0, 0);
        GameObject projectile = Instantiate(projectilePrefab, position, rotation);
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
