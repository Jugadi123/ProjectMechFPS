using UnityEngine;
public class BaseWeapon : MonoBehaviour
{
    // a base weapon class that will be used to create all weapons in this game.
    // hitscan or projectile
    // will handle all logic for a weapon but seperated into various functions, components, etc.

    public enum WeaponSide {
        Primary,
        secondary
    }

    public enum WeaponType {
        Hitscan,
        Projectile
    }

    [SerializeField] Transform weaponTransform;
    public int weaponID;
    public string weaponName;
    public WeaponSide weaponSide;
    public WeaponType weaponType;
    public float weaponDamage;
    public float weaponSpread;
    public float weaponFireRate;
    public float weaponHeatUsage;

    public BaseWeapon(Transform weaponTransform, int weaponID, string weaponName, WeaponType weaponType, float weaponDamage, float weaponSpread, float weaponFireRate, float weaponHeatUsage, WeaponSide weaponSide)
    {
        this.weaponTransform = weaponTransform;
        this.weaponID = weaponID;
        this.weaponName = weaponName;
        this.weaponType = weaponType;
        this.weaponDamage = weaponDamage;
        this.weaponSpread = weaponSpread;
        this.weaponFireRate = weaponFireRate;
        this.weaponHeatUsage = weaponHeatUsage;
        this.weaponSide = weaponSide;
    }

}