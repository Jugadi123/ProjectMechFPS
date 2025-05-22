



// Since both primary and secondary weapons would have the same logic (firing, heat, spread, etc), 
// it would be best to have a single weapon handler that handles both weapons. 
// This would also allow us to easily change weapons without having to change the code in multiple places.


using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class WeaponHandler : NetworkBehaviour
{
    private ulong LOCAL_CLIENT_ID;

    private HandleLagCompensation handleLagCompensation;

    [SerializeField] private GameObject tempTracerPrefab;

    [SerializeField] private Transform primaryWeaponMount;
    [SerializeField] private Transform secondaryWeaponMount;
    [SerializeField] private Camera playerCamera;

    // Actual Weapon references
    public Weapon primaryWeaponData;
    public Weapon secondaryWeaponData;

    // Audio sources for weapon sounds
    private AudioSource primaryWeaponFireSound;
    private AudioSource secondaryWeaponFireSound;

    // Locally tracked variables for client-side prediction
    public float primaryCurrentHeat = 0f;
    public float secondaryCurrentHeat = 0f;
    public bool IsPrimaryOverheated = false;
    public bool IsSecondaryOverheated = false;
    public float primarySpread = 0f;
    public float secondarySpread = 0f;

    // Cooldown timers
    public float primaryOverheatCooldown = 0f;
    public float secondaryOverheatCooldown = 0f;

    private GameObject primaryMuzzleFlash;
    private GameObject secondaryMuzzleFlash;

    

    // Input tracking
    private bool primaryFireInput = false;
    private bool secondaryFireInput = false;

    // tracking if we've fired a weapon for handling cooldowns and such later
    private bool hasFiredPrimary = false;
    private bool hasFiredSecondary = false;


    // server's projectiles
    [SerializeField] private GameObject tempServerGrenadePrefab;
    [SerializeField] private GameObject tempServerMissilePrefab;


    // storing client's projectiles
    private Dictionary<ulong, GameObject> clientProjectiles = new Dictionary<ulong, GameObject>();

    // input buffer
    public struct WeaponFireInput
    {
        public ulong tick;
        public bool primaryFirePressed;
        public bool secondaryFirePressed;
    }

    private const int weaponInputBufferSize = 60;
    private WeaponFireInput[] weaponInputBuffer = new WeaponFireInput[weaponInputBufferSize];


    // Add these fields at the top of the class
    private ulong currentTick = 0;
    private int primaryLastFireTick = 0;
    private int secondaryLastFireTick = 0;
    private int primaryTicksBetweenShots = 0;
    private int secondaryTicksBetweenShots = 0;

    // weapondata serialization
    public struct WeaponInfo : INetworkSerializable
    {
        public bool isPrimary;
        public float damage;
        public float range;
        public float fireRateTicks;
        public bool isHitscan;
        public ProjectileType projectileType;
        public float projectileMass;
        public float projectileSpeed;
        public float projectileGravity;
        public float explosionRadius;
        public float firstBounchExplosionExpiryTime;
        public float timeBeforeCanManuallyExplode;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref isPrimary);
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref range);
            serializer.SerializeValue(ref fireRateTicks);
            serializer.SerializeValue(ref isHitscan);
            serializer.SerializeValue(ref projectileType);
            serializer.SerializeValue(ref projectileMass);
            serializer.SerializeValue(ref projectileSpeed);
            serializer.SerializeValue(ref projectileGravity);
            serializer.SerializeValue(ref explosionRadius);
            serializer.SerializeValue(ref firstBounchExplosionExpiryTime);
            serializer.SerializeValue(ref timeBeforeCanManuallyExplode);
        }

        public static WeaponInfo FromWeapon(Weapon weapon)
        {
            return new WeaponInfo
            {
                isPrimary = weapon.IsPrimary,
                damage = weapon.damage,
                range = weapon.range,
                fireRateTicks = weapon.fireRateTicks,
                isHitscan = weapon.isHitscan,
                projectileType = weapon.projectileType,
                projectileMass = weapon.projectileMass,
                projectileSpeed = weapon.projectileSpeed,
                projectileGravity = weapon.projectileGravity,
                explosionRadius = weapon.explosionRadius,
                firstBounchExplosionExpiryTime = weapon.firstBounchExplosionExpiryTime,
                timeBeforeCanManuallyExplode = weapon.timeBeforeCanManuallyExplode
            };
        }
    }


    private void SetupWeapon(Weapon weaponData)
    {
        // load weapon data about the weapon type from resources. Normally this would be done in a database or something

        // if weapondata is null, throw error and return
        if (weaponData == null)
        {
            Debug.LogError($"Failed to load weapon data for {weaponData}");
            return;
        }

        bool isPrimary = weaponData.IsPrimary;

        // Store reference to the weapon data
        if (isPrimary)
        {

            // Set up muzzle flash
            if (primaryMuzzleFlash != null)
                Destroy(primaryMuzzleFlash);

            primaryMuzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, primaryWeaponMount.transform);
            primaryMuzzleFlash.SetActive(false);

            // Reset heat, cooldown and spread
            primaryCurrentHeat = 0f;
            IsPrimaryOverheated = false;
            primarySpread = weaponData.baseSpread;

            // firerate in ticks
            primaryTicksBetweenShots = weaponData.fireRateTicks;



            // Check if the primary weapon mount already has an AudioSource
            primaryWeaponFireSound = primaryWeaponMount.gameObject.GetComponent<AudioSource>();
            if (primaryWeaponFireSound == null)
            {
                // If no AudioSource exists, add a new one
                primaryWeaponFireSound = primaryWeaponMount.gameObject.AddComponent<AudioSource>();
            }
            else
            {
                // If an AudioSource exists, ensure it's not used for other purposes
                // This can be done by checking if the clip is null or if it's not playing
                if (primaryWeaponFireSound.clip != null || primaryWeaponFireSound.isPlaying)
                {
                    Debug.LogWarning("Primary weapon mount's AudioSource is already in use. Consider reviewing its usage.");
                }
            }

        }
        else
        {

            // Set up muzzle flash
            if (secondaryMuzzleFlash != null)
                Destroy(secondaryMuzzleFlash);

            secondaryMuzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, secondaryWeaponMount.transform);
            secondaryMuzzleFlash.SetActive(false);

            // Reset heat and cooldown
            secondaryCurrentHeat = 0f;
            IsSecondaryOverheated = false;
            secondarySpread = weaponData.baseSpread;

            // firerate in ticks
            secondaryTicksBetweenShots = weaponData.fireRateTicks;


            // Check if the secondary weapon mount already has an AudioSource
            secondaryWeaponFireSound = secondaryWeaponMount.gameObject.GetComponent<AudioSource>();
            if (secondaryWeaponFireSound == null)
            {
                // If no AudioSource exists, add a new one
                secondaryWeaponFireSound = secondaryWeaponMount.gameObject.AddComponent<AudioSource>();
            }
            else
            {
                // If an AudioSource exists, ensure it's not used for other purposes
                if (secondaryWeaponFireSound.clip != null || secondaryWeaponFireSound.isPlaying)
                {
                    Debug.LogWarning("Secondary weapon mount's AudioSource is already in use. Consider reviewing its usage.");
                }
            }
        }
    }

    private void HandleWeaponFiring(ulong currentTick, bool primaryFireInput, bool secondaryFireInput)
    {
        // handle firerate
        if (primaryFireInput && !IsPrimaryOverheated)
        {
            if ((int)currentTick - primaryLastFireTick >= primaryTicksBetweenShots)
            {
                FireWeapon(currentTick, primaryWeaponData);
                primaryLastFireTick = (int)currentTick;
            }
        }

        // handle firerate
        if (secondaryFireInput && !IsSecondaryOverheated)
        {
            if ((int)currentTick - secondaryLastFireTick >= secondaryTicksBetweenShots)
            {
                FireWeapon(currentTick, secondaryWeaponData);
                secondaryLastFireTick = (int)currentTick;
            }
        }
    }

    [ServerRpc]
    private void HandleServerHitscanServerRpc(ulong clientTick, WeaponInfo weaponData, Vector3 clientOrigin, Vector3 clientDirection, ulong localClientId)
    {

        RaycastHit hitInfo;

        bool didHit = handleLagCompensation.TryLagCompensatedRaycast(localClientId, clientTick, clientOrigin, clientDirection, weaponData.range, out hitInfo);

        string decalIdOfHitObject = "";

        if (didHit)
        {
            if (hitInfo.collider.CompareTag("Player"))
            {

                // get enemy player
                NetworkObject enemyPlayer = hitInfo.collider.GetComponent<NetworkObject>();

                // get the id of the enemy player
                ulong enemyPlayerId = enemyPlayer.OwnerClientId;

                // take damage
                enemyPlayer.GetComponent<HandleHealth>().TakeDamage(weaponData.damage, localClientId);

                
                decalIdOfHitObject = "Player";
            }
            else
            {
                // we hit something else that isn't a player
                decalIdOfHitObject = hitInfo.transform.GetComponent<DecalAnchor>().decalId;
            }
        }

        
        PlayFiringEffectsClientRpc(didHit, weaponData, clientOrigin, clientDirection, hitInfo.point, hitInfo.normal, decalIdOfHitObject);
        
        // show all other clients the shot


    }



    [ServerRpc]
    private void HandleServerProjectileServerRpc(ulong clientTick, WeaponInfo weaponData, Vector3 clientOrigin, Vector3 clientDirection, ulong localClientId)
    {
 
        float tickInterval = NetworkTimer.Singleton.GetTickInterval();
        ulong currentServerTick = NetworkTimer.Singleton.CurrentTick.Value;
        ulong ticksPassed = currentServerTick - clientTick;
        float timePassed = ticksPassed * tickInterval;



        // spawn server's projectile

        // override client's projectile prefab with server's
        GameObject tempServerProjectilePrefab = null;
        // Vector3 predictedProjectileOrigin = clientOrigin + (clientDirection * weaponData.projectileSpeed * timePassed);
        Vector3 projectileOrigin = clientOrigin;
        Quaternion projectileRotation = Quaternion.LookRotation(clientDirection);

        // minor adjustment for rocket
        if (weaponData.projectileType == ProjectileType.Grenade)
        {
            
            tempServerProjectilePrefab = tempServerGrenadePrefab;
        }
        else if (weaponData.projectileType == ProjectileType.Rocket)
        {
            tempServerProjectilePrefab = tempServerMissilePrefab;
            projectileRotation *= Quaternion.Euler(90f, 0, 0);
        }

        GameObject projectile = Instantiate(tempServerProjectilePrefab, projectileOrigin, projectileRotation);
        projectile.GetComponent<NetworkObject>().SpawnWithOwnership(localClientId);
        projectile.GetComponent<ServerProjectile>().Initialize(localClientId, weaponData.projectileMass, weaponData.projectileSpeed, weaponData.damage, weaponData.explosionRadius, weaponData.projectileGravity, weaponData.projectileType);

        UpdateProjectileForClientRpc(clientTick, ticksPassed);
    }

    [ClientRpc]
    private void UpdateProjectileForClientRpc(ulong clientTick, ulong laggedTicks)
    {
        // only to show visuals on the client and NOT the actual projectile because that's already in since the server
        if (IsOwner)
        {
            // if (clientProjectiles.ContainsKey(clientTick))
            // {
            //     // destroy the client's projectile
            //     Destroy(clientProjectiles[clientTick]);
            //     // remove the projectile index from the dictionary
            //     clientProjectiles.Remove(clientTick);
            // }
            return;
        }
    }


    [ClientRpc]
    private void PlayFiringEffectsClientRpc(bool didHit, WeaponInfo weaponInfo, Vector3 origin, Vector3 direction, Vector3 hitPoint, Vector3 hitNormal, string decalIdOfHitObject, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return; // Don't run visuals on your own client again

        GameObject muzzle = weaponInfo.isPrimary ? primaryMuzzleFlash : secondaryMuzzleFlash;
        
        if (muzzle != null)
            StartCoroutine(PlayMuzzleFlash(muzzle));


        PlayFireSound(weaponInfo.isPrimary ? primaryWeaponData.fireSound : secondaryWeaponData.fireSound, weaponInfo.isPrimary);


        // if the client didn't hit anything
        if (!didHit)
        {
            if (weaponInfo.isHitscan)
            {
                Vector3 missPoint = origin + direction * weaponInfo.range;
                StartCoroutine(DrawTracer(muzzle.transform.position, missPoint, Quaternion.LookRotation(missPoint)));
            }
        }
        else
        {
            if (weaponInfo.isHitscan)
            {
                StartCoroutine(DrawTracer(muzzle.transform.position, (hitPoint + hitNormal * 0.1f), Quaternion.LookRotation(hitPoint)));

                if (decalIdOfHitObject != "" && decalIdOfHitObject.Length > 0)
                {
                    // spawn impact and bullet hole decals
                    if (primaryWeaponData.impactEffectPrefab != null)
                    {
                        GameObject impact = Instantiate(
                            primaryWeaponData.impactEffectPrefab,
                            (hitPoint),
                            Quaternion.LookRotation(hitNormal)
                        );
                        Destroy(impact, 2f);
                    }

                    if (primaryWeaponData.bulletHolePrefab != null)
                    {
                        GameObject bulletHole = Instantiate(
                            primaryWeaponData.bulletHolePrefab,
                            (hitPoint + hitNormal * 0.01f),
                            Quaternion.LookRotation(-hitNormal)
                        );

                        DecalAnchor[] anchors = GameObject.FindObjectsByType<DecalAnchor>(sortMode: FindObjectsSortMode.None);

                        foreach (DecalAnchor anchor in anchors)
                        {
                            if (anchor.decalId == decalIdOfHitObject)
                            {
                                bulletHole.transform.SetParent(anchor.transform);
                            }
                        }
                        Destroy(bulletHole, 5f);
                    }
                }
            }
            else
            {
                // projectile effects

            }
        } 
    }


    private void FireWeapon(ulong currentTick, Weapon weaponData)
    {
        if (weaponData == null) return;

        // is this weapon primary or secondary?
        bool isPrimary = weaponData.IsPrimary;

        // for state management
        if (isPrimary)
        {
            hasFiredPrimary = true;
        }
        else
        {
            hasFiredSecondary = true;
        }


        // Handle weapon-specific firing logic
        // This is where we get to weapon specific logic
        if (weaponData.isHitscan)
        {
            // Get firing origin and direction on the client
            Vector3 rayOrigin = playerCamera.transform.position;
            Vector3 rayDirection = CalculateSpreadDirection(playerCamera.transform.forward, isPrimary ? primarySpread : secondarySpread);

            // Client-side raycast for prediction
            RaycastHit predictedHitInfo;
            bool predictedDidHit = Physics.Raycast(rayOrigin, rayDirection, out predictedHitInfo, weaponData.range, ~LayerMask.GetMask("LocalPlayer"));

            // Show immediate visuals
            HandleClientHitscanVisuals(weaponData, rayOrigin, rayDirection, predictedDidHit, predictedHitInfo);

            // Ask server to confirm and validate

            // Serizalize info before sending

            HandleServerHitscanServerRpc(currentTick, WeaponInfo.FromWeapon(weaponData), rayOrigin, rayDirection, LOCAL_CLIENT_ID);
        }
        else
        {
            Vector3 muzzlePosition = isPrimary ? primaryMuzzleFlash.transform.position : secondaryMuzzleFlash.transform.position;
            Vector3 direction = playerCamera.transform.forward;

            // Calculate spawn position with offset
            float spawnOffset = 0.5f;
            Vector3 spawnPosition = muzzlePosition + (direction.normalized * spawnOffset);

            HandleClientProjectileVisuals(currentTick, weaponData, spawnPosition, direction);
        }
    }


    private void HandleClientProjectileVisuals(ulong currentTick, Weapon weaponData, Vector3 spawnPosition, Vector3 direction)
    {
        if (weaponData.projectilePrefab == null) return;

        // Get target point using raycast
        int mask = ~LayerMask.GetMask("LocalPlayer");
        RaycastHit hitInfo;
        Vector3 targetDirection;

        if (Physics.Raycast(playerCamera.transform.position, direction, out hitInfo, 1000f, mask))
        {
            // If we hit something, use the hit point
            targetDirection = hitInfo.point - spawnPosition;
            Debug.DrawRay(spawnPosition, targetDirection, Color.green, 5f);
            Debug.DrawRay(playerCamera.transform.position, hitInfo.point - playerCamera.transform.position, Color.red, 5f);
        }
        else
        {
            // If we didn't hit anything, use a point far away
            Vector3 directionOffset = playerCamera.transform.position + direction * 1000f;
            targetDirection = directionOffset - spawnPosition;
            Debug.DrawRay(spawnPosition, targetDirection, Color.green, 5f);
            Debug.DrawRay(playerCamera.transform.position, directionOffset - playerCamera.transform.position, Color.red, 5f);
        }


        if (weaponData.projectileType == ProjectileType.Grenade)
        {
            // spawn grenade
            clientProjectiles[currentTick] = Instantiate(weaponData.projectilePrefab, spawnPosition, Quaternion.LookRotation(targetDirection));
            clientProjectiles[currentTick].GetComponent<Projectile>().Initialize(weaponData.projectileMass, weaponData.projectileSpeed, weaponData.projectileGravity, weaponData.projectileType);
        }
        else if (weaponData.projectileType == ProjectileType.Rocket)
        {
            // spawn rocket
            clientProjectiles[currentTick] = Instantiate(weaponData.projectilePrefab, spawnPosition, Quaternion.LookRotation(targetDirection) * Quaternion.Euler(90f, 0, 0));
            clientProjectiles[currentTick].GetComponent<Projectile>().Initialize(weaponData.projectileMass, weaponData.projectileSpeed, weaponData.projectileGravity, weaponData.projectileType);
        }

        Debug.Log($"local player id {LOCAL_CLIENT_ID}");

        HandleServerProjectileServerRpc(currentTick, WeaponInfo.FromWeapon(weaponData), spawnPosition, targetDirection, LOCAL_CLIENT_ID);

    }


    private void HandleClientHitscanVisuals(Weapon weapon, Vector3 origin, Vector3 direction, bool didHit, RaycastHit hitInfo)
    {

        bool isPrimary = weapon.IsPrimary;
        GameObject muzzle = isPrimary ? primaryMuzzleFlash : secondaryMuzzleFlash;

        // Muzzle flash
        if (muzzle != null)
            StartCoroutine(PlayMuzzleFlash(muzzle));

        // Fire sound
        PlayFireSound(weapon.fireSound, isPrimary);


        // Impact visuals
        if (didHit)
        {
            // local info about the hit to test lag compensation
            if (hitInfo.collider.CompareTag("Player"))
            {
                NetworkObject enemyPlayer = hitInfo.collider.GetComponent<NetworkObject>();
            }

            if (weapon.impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(
                    weapon.impactEffectPrefab,
                    hitInfo.point,
                    Quaternion.LookRotation(hitInfo.normal)
                );
                Destroy(impact, 2f);
            }

            if (weapon.bulletHolePrefab != null)
            {
                GameObject bulletHole = Instantiate(
                    weapon.bulletHolePrefab,
                    hitInfo.point + hitInfo.normal * 0.01f,
                    Quaternion.LookRotation(-hitInfo.normal)
                );
                bulletHole.transform.SetParent(hitInfo.transform);
                Destroy(bulletHole, 5f);
            }

            if (weapon.tracerPrefab != null)
            {
                StartCoroutine(DrawTracer(
                    muzzle.transform.position,
                    hitInfo.point + hitInfo.normal * 0.1f,
                    Quaternion.LookRotation(hitInfo.point)
                ));
            }
        }
        else
        {
            if (weapon.tracerPrefab != null)
            {
                Vector3 missPoint = origin + direction * weapon.range;
                StartCoroutine(DrawTracer(
                    muzzle.transform.position,
                    missPoint,
                    Quaternion.LookRotation(missPoint)
                ));
            }
        }
    }


    private Vector3 CalculateSpreadDirection(Vector3 direction, float spread)
    {
        // Apply random spread to direction
        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0f
        );

        return randomRotation * direction;
    }

    private void UpdateWeaponState()
    {
        // use tick interval instead of delta time
        float tickInterval = NetworkTimer.Singleton.GetTickInterval();

        if (hasFiredPrimary)
        {
            // if firing

            // primary weapon heat and spread management
            if (!IsPrimaryOverheated)
            {
                // increase heat per shot and manage it until overheated
                primaryCurrentHeat += primaryWeaponData.heatPerShot;

                if (primaryCurrentHeat >= primaryWeaponData.maxHeat)
                {
                    primaryCurrentHeat = primaryWeaponData.maxHeat;
                    IsPrimaryOverheated = true;
                    primaryOverheatCooldown = primaryWeaponData.overheatedCooldownTime;
                }

                // Increase spread per shot and manage it
                primarySpread += primaryWeaponData.spreadIncreasePerShot;

                if (primarySpread >= primaryWeaponData.maxSpread)
                {
                    primarySpread = primaryWeaponData.maxSpread;
                }
            }
            hasFiredPrimary = false;
        }
        else
        {
            // if not firing
            if (IsPrimaryOverheated)
            {
                primaryOverheatCooldown -= tickInterval;
                if (primaryOverheatCooldown <= 0f)
                {
                    IsPrimaryOverheated = false;
                    primaryCurrentHeat = 0f;
                    primaryOverheatCooldown = 0f;
                }
            }
            else
            {
                // Cool the heat down
                primaryCurrentHeat -= primaryWeaponData.cooldownRate * tickInterval;
                if (primaryCurrentHeat <= 0f)
                {
                    primaryCurrentHeat = 0f;
                }

                // Recover spread
                primarySpread -= primaryWeaponData.spreadRecoveryRate * tickInterval;
                if (primarySpread <= 0f)
                {
                    primarySpread = 0f;
                }
            }
        }


        if (hasFiredSecondary)
        {
            // secondary weapon heat and spread management
            if (!IsSecondaryOverheated)
            {
                // increase heat per shot and manage it
                secondaryCurrentHeat += secondaryWeaponData.heatPerShot;

                if (secondaryCurrentHeat >= secondaryWeaponData.maxHeat)
                {
                    secondaryCurrentHeat = secondaryWeaponData.maxHeat;
                    IsSecondaryOverheated = true;
                    secondaryOverheatCooldown = secondaryWeaponData.overheatedCooldownTime;
                }

                // Increase spread per shot and manage it
                secondarySpread += secondaryWeaponData.spreadIncreasePerShot;

                if (secondarySpread >= secondaryWeaponData.maxSpread)
                {
                    secondarySpread = secondaryWeaponData.maxSpread;
                }
            }
            hasFiredSecondary = false;
        }
        else
        {
            // if not firing
            if (IsSecondaryOverheated)
            {
                secondaryOverheatCooldown -= tickInterval;
                if (secondaryOverheatCooldown <= 0f)
                {
                    IsSecondaryOverheated = false;
                    secondaryCurrentHeat = 0f;
                    secondaryOverheatCooldown = 0f;
                }
            }
            else
            {
                // Cool the heat down
                secondaryCurrentHeat -= secondaryWeaponData.cooldownRate * tickInterval;
                if (secondaryCurrentHeat <= 0f)
                {
                    secondaryCurrentHeat = 0f;
                }

                // Recover spread
                secondarySpread -= secondaryWeaponData.spreadRecoveryRate * tickInterval;
                if (secondarySpread <= 0f)
                {
                    secondarySpread = 0f;
                }
            }
        }
    }

    private IEnumerator PlayMuzzleFlash(GameObject muzzleFlash)
    {
        muzzleFlash.SetActive(true);
        // wait for one tick
        yield return new WaitForSeconds(0.05f);
        muzzleFlash.SetActive(false);
    }

    private void PlayFireSound(AudioClip clip, bool isPrimary)
    {
        if (clip != null)
        {
            if (isPrimary)
            {
                primaryWeaponFireSound.clip = clip;
                primaryWeaponFireSound.Play();
            }
            else
            {
                secondaryWeaponFireSound.clip = clip;
                secondaryWeaponFireSound.Play();
            }
        }
    }

    IEnumerator DrawTracer(Vector3 origin, Vector3 endPoint, Quaternion lookRotation)
    {
        if (tempTracerPrefab == null)
        {
            Debug.LogError("Tracer prefab not found on " + gameObject.name);
            yield break;
        }

        GameObject tracer = Instantiate(tempTracerPrefab, origin, lookRotation);

        while (Vector3.Distance(tracer.transform.position, endPoint) > 0.1f)
        {
            tracer.transform.position = Vector3.MoveTowards(tracer.transform.position, endPoint, Time.deltaTime * 50f);
            yield return null;
        }
 
        Destroy(tracer);
    }



    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        handleLagCompensation = GetComponent<HandleLagCompensation>();

        if (handleLagCompensation == null)
        {
            Debug.LogError("HandleLagCompensation component not found on " + gameObject.name);
        }

        // Initial weapon setup
        SetupWeapon(primaryWeaponData);
        SetupWeapon(secondaryWeaponData);

        if (!IsOwner) return;

        LOCAL_CLIENT_ID = OwnerClientId;
    }

    // gather input
    private void Update()
    {
        if (!IsOwner) return;

        // Collect input
        primaryFireInput = Input.GetKey(KeyCode.Mouse0);
        secondaryFireInput = Input.GetKey(KeyCode.Mouse1);
    }

    private void FixedUpdate()
    {

        if (!IsOwner) return;

        // Get current tick
        currentTick = NetworkTimer.Singleton.CurrentTick.Value;

        int bufferIndex = (int)currentTick % weaponInputBufferSize;

        weaponInputBuffer[bufferIndex] = new WeaponFireInput()
        {
            tick = currentTick,
            primaryFirePressed = primaryFireInput,
            secondaryFirePressed = secondaryFireInput
        };

        // Handle weapon firing and effects
        HandleWeaponFiring(currentTick, primaryFireInput, secondaryFireInput);

        // Update weapon state
        UpdateWeaponState();
    }

}
