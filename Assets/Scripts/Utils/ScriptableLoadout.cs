
// basically a sample database for the game's weapons and loadouts.

using UnityEngine;

[CreateAssetMenu(fileName = "Loadout", menuName = "Loadouts/Loadout")]
public class ScriptableLoadout : ScriptableObject {
    public int id;
    public string loadoutName;
    // public ScriptableWeapon primaryWeapon;
    // public ScriptableWeapon secondaryWeapon;
}