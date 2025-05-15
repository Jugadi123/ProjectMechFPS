
// basically a sample database for the game's weapons and loadouts.

using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_Weapon", menuName = "Weapons/Weaponfewfwefwe")]
public class ScriptableWeapon : ScriptableObject {
    public int id;
    public Sprite Icon2D;
    public string weaponName;
    public string description;
    public LoadoutStructureReference.WeaponHitType type;
    public LoadoutStructureReference.WeaponClass weaponClass;
    public int damage;
    public int fireRate;
    public float heatUsage;
}