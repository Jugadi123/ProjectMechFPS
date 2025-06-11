using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon")]
public class Weapon : ScriptableObject
{
    [Header("General Info")]
    public WeaponType weaponType;
    public string weaponName;
    public bool IsPrimary = true;
    public GameObject weaponModel;
    public GameObject muzzleFlashPrefab;
    public AudioClip fireSound;

    [Header("Performance Stats")]
    public int fireRateTicks = 10; // ticks between shots
    public float damage = 10f;
    public float range = 1000f;

    [Header("Heat Management")]
    public float heatPerShot = 5f;
    public float maxHeat = 100f;
    public float cooldownRate = 2f; // heat cool down when not firing
    public float overheatedCooldownTime = 3f;

    [Header("Recoil and Spread")]
    public float baseSpread = 0.2f;
    public float maxSpread = 2.0f;
    public float spreadIncreasePerShot = 0.1f;
    public float spreadRecoveryRate = 0.5f;

    [Header("Visual Effects")]
    public GameObject impactEffectPrefab;
    public GameObject bulletHolePrefab;
    public GameObject tracerPrefab;

    public bool isHitscan = true;
    [Header("Projectile Settings")]
    // Only needed for non-hitscan weapons
    public ProjectileType projectileType = ProjectileType.None;
    public GameObject projectilePrefab;
    public float projectileMass = 0.7f;
    public float projectileSpeed = 50f; // only for non-hitscan weapons
    public float projectileGravity = 9.81f; // 0 for no gravity
    public float explosionRadius = 0f; // 0 for no explosion
    [Header("Grenade Projectile Settings")]
    public float firstBounchExplosionExpiryTime;
    public float timeBeforeCanManuallyExplode;
    
    // For things like rocket jumping
    public float pushBackForce;
} 