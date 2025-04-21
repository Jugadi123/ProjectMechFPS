
// basically a sample database for the game's weapons and loadouts.

using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_Rifle", menuName = "Weapons/Rifle")]
public class LoadoutStructureReference : ScriptableObject {

    public enum WeaponHitType {
        HitScan,
        Projectile,
    }

    public enum WeaponClass {
        SMG,
        Shotgun,
        Rifle,
        Sniper,
        RocketLauncher,
        GrenadeLauncher,
    }

    public struct WeaponStats {
        public int id;
        public string name;
        // public Texture2D icon; too much data to store. Instead I'll use a seperate function to retrieve the icon from the weapon's id
        public string description;
        public WeaponHitType type; // hitscan or projectile
        public WeaponClass weaponClass; // SMG or Rifle, etc
        public int damage;
        public int fireRate;
        public float heatUsage;
    };

    public class LoadoutDetails {
        public int id;
        public string name;
        public WeaponStats primaryWeapon;
        public WeaponStats secondaryWeapon;
    }
}