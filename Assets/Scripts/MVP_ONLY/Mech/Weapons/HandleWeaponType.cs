using UnityEngine;
using Unity.Netcode;
public class HandleWeaponType : MonoBehaviour
{
    // This is the hitscan weapon handler
    // It will handle the logic for the hitscan weapon


    // Get weapon netvars from WeaponNetvars based on loadout selected
    // placeholder loadout

    private struct Loudout {
        Weapon primaryWeapon;
        Weapon secondaryWeapon;
    }

    public static void GetWeaponInput(bool primaryFireInput, bool secondaryFireInput) {
        
    }
}