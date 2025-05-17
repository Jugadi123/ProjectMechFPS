



// Since both primary and secondary weapons would have the same logic (firing, heat, spread, etc), 
// it would be best to have a single weapon handler that handles both weapons. 
// This would also allow us to easily change weapons without having to change the code in multiple places.


using System.Collections;
using UnityEngine;
using Unity.Netcode;
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


    // GameObject references
    private GameObject primaryWeaponInstance;
    private GameObject secondaryWeaponInstance;
    private GameObject primaryMuzzleFlash;
    private GameObject secondaryMuzzleFlash;

    

    // Input tracking
    private bool primaryFireInput = false;
    private bool secondaryFireInput = false;

    // tracking if we've fired a weapon for handling cooldowns and such later
    private bool hasFiredPrimary = false;
    private bool hasFiredSecondary = false;


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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref isPrimary);
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref range);
            serializer.SerializeValue(ref fireRateTicks);
            serializer.SerializeValue(ref isHitscan);
        }

        public static WeaponInfo FromWeapon(Weapon weapon)
        {
            return new WeaponInfo
            {
                isPrimary = weapon.IsPrimary,
                damage = weapon.damage,
                range = weapon.range,
                fireRateTicks = weapon.fireRateTicks,
                isHitscan = weapon.isHitscan
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

            // Destroy any existing weapon
            if (primaryWeaponInstance != null)
                Destroy(primaryWeaponInstance);

            // Instantiate placeholder weapon model (replace with actual model in the future)
            primaryWeaponInstance = primaryWeaponMount.gameObject; // Assuming the pipe is already attached to the mount

            // Set up muzzle flash
            if (primaryMuzzleFlash != null)
                Destroy(primaryMuzzleFlash);

            primaryMuzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, primaryWeaponInstance.transform);
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
            // Destroy existing weapon if any
            if (secondaryWeaponInstance != null)
                Destroy(secondaryWeaponInstance);

            // Instantiate placeholder weapon model (replace with actual model in the future)
            secondaryWeaponInstance = secondaryWeaponMount.gameObject; // Assuming the pipe is already attached to the mount

            // Set up muzzle flash
            if (secondaryMuzzleFlash != null)
                Destroy(secondaryMuzzleFlash);

            secondaryMuzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, secondaryWeaponInstance.transform);
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
    private void HandleServerHitscanServerRpc(ulong clientTick, WeaponInfo weaponData, Vector3 clientOrigin, Vector3 clientDirection, ulong clientId)
    {

        if (handleLagCompensation.TryLagCompensatedRaycast(clientId, clientTick, clientOrigin, clientDirection, weaponData.range, out RaycastHit hitInfo))
        {


            if (hitInfo.collider.CompareTag("Player"))
            {

                NetworkObject enemyPlayer = hitInfo.collider.GetComponent<NetworkObject>();


                enemyPlayer.GetComponent<HandleHealth>().TakeDamage(weaponData.damage, clientId);


                Debug.Log($"Hitscan from client {clientId} -> Hit enemy client {enemyPlayer.OwnerClientId}");

            }
        }
        
        // show all other clients the shot

        PlayFiringEffectsClientRpc(weaponData, clientOrigin, hitInfo.point, hitInfo.normal);


    }

    [ClientRpc]
    private void PlayFiringEffectsClientRpc(WeaponInfo weaponInfo, Vector3 origin, Vector3 hitPoint, Vector3 hitNormal, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return; // Don't run visuals on your own client again

        GameObject muzzle = weaponInfo.isPrimary ? primaryMuzzleFlash : secondaryMuzzleFlash;

        if (muzzle != null)
            StartCoroutine(PlayMuzzleFlash(muzzle));

        PlayFireSound(weaponInfo.isPrimary ? primaryWeaponData.fireSound : secondaryWeaponData.fireSound, weaponInfo.isPrimary);

        if (weaponInfo.isHitscan)
        {
            StartCoroutine(DrawTracer(muzzle.transform.position, hitPoint + hitNormal * 0.1f, Quaternion.LookRotation(hitPoint)));
        }

        // Optionally add bullet holes, impact FX if you trust the hit position (after lag compensation)
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
            bool predictedDidHit = Physics.Raycast(rayOrigin, rayDirection, out predictedHitInfo, weaponData.range, ~LayerMask.GetMask("Projectiles&Bullets", "LocalPlayer"));

            // Show immediate visuals
            HandleClientHitscanVisuals(weaponData, rayOrigin, rayDirection, predictedDidHit, predictedHitInfo);

            // Ask server to confirm and validate

            // Serizalize info before sending

            HandleServerHitscanServerRpc(currentTick, WeaponInfo.FromWeapon(weaponData), rayOrigin, rayDirection, LOCAL_CLIENT_ID);
        }
        else
        {
            // // fire from respective muzzle for projectile weapons
            // if (isPrimary)
            // {
            //     FireProjectileWeapon(weapon, primaryMuzzleFlash.transform.position, rayDirection);
            // }
            // else
            // {
            //     FireProjectileWeapon(weapon, secondaryMuzzleFlash.transform.position, rayDirection);
            // }
        }
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
                Debug.Log($"Hitscan from client {NetworkObject.OwnerClientId} -> Hit enemy client {enemyPlayer.OwnerClientId}");
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
                    Quaternion.LookRotation(hitInfo.point)
                ));
            }
        }
    }


    // projectile related logic
    private void FireProjectileWeapon(Weapon weapon, Vector3 muzzlePosition, Vector3 direction)
    {
        // Calculate spawn position with offset
        float spawnOffset = 0.5f;
        Vector3 spawnPosition = muzzlePosition + (direction.normalized * spawnOffset);

        // Get target point using raycast
        int mask = ~LayerMask.GetMask("Projectiles&Bullets");
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
        }

        // Spawn the projectile
        if (weapon.projectilePrefab != null)
        {
            GameObject projectile = Instantiate(
                weapon.projectilePrefab,
                spawnPosition,
                Quaternion.LookRotation(targetDirection) * Quaternion.Euler(90f, 0, 0)
            );

            // Configure the projectile
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(
                    targetDirection,
                    weapon.projectileSpeed,
                    weapon.damage,
                    weapon.explosionRadius,
                    weapon.projectileGravity
                );
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

        LOCAL_CLIENT_ID = NetworkManager.LocalClient.ClientId;
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
