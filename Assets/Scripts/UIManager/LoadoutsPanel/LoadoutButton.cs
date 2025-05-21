
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System;
using UnityEngine.UI;

public class LoadoutButton : MonoBehaviour, IPointerClickHandler
{

    public ScriptableLoadoutCollection loadoutCollection;

    private GameObject detailsContainer;

    private GameManager gameManager;

    private ScriptableLoadout selectedLoadout;


    void Start()
    {
        gameManager = GameManager.instance;

  
        detailsContainer = transform.parent.parent.GetChild(2).gameObject;
    }

    public void OnPointerClick(PointerEventData clickEvent)
    {
        // show relevant details on the details panel depending on which loadout was selected

        int selectedLoadoutID = Int32.Parse(GetComponentInChildren<TextMeshProUGUI>().text.Split(" ")[1]);

        gameManager.selectedLoadout = loadoutCollection.loadouts[selectedLoadoutID - 1];

        selectedLoadout = gameManager.selectedLoadout;

        Transform primaryContainer = detailsContainer.transform.GetChild(1);
        Transform secondaryContainer = detailsContainer.transform.GetChild(2);

        // primary weapon
        GameObject weaponImageContainer = primaryContainer.GetChild(0).gameObject;
        GameObject weaponNameContainer = primaryContainer.GetChild(1).gameObject;
        GameObject weaponDescriptionContainer = primaryContainer.GetChild(2).gameObject;

        GameObject weaponStatsContainer = primaryContainer.GetChild(3).gameObject;
        GameObject weaponStatsDamageContainer = weaponStatsContainer.transform.GetChild(0).gameObject;
        GameObject weaponStatsFirerateContainer = weaponStatsContainer.transform.GetChild(1).gameObject;
        GameObject weaponStatsHeatusageContainer = weaponStatsContainer.transform.GetChild(2).gameObject;



        // uncomment to check errors

        // weaponImageContainer.GetComponent<Image>().sprite = selectedLoadout.primaryWeapon.Icon2D;
        // weaponNameContainer.GetComponent<TextMeshProUGUI>().text = selectedLoadout.primaryWeapon.weaponName;
        // weaponDescriptionContainer.GetComponent<TextMeshProUGUI>().text = selectedLoadout.primaryWeapon.description;

        // weaponStatsDamageContainer.GetComponent<TextMeshProUGUI>().text = "Damage: " + selectedLoadout.primaryWeapon.damage;
        // weaponStatsFirerateContainer.GetComponent<TextMeshProUGUI>().text = "Firerate: " + selectedLoadout.primaryWeapon.fireRate;
        // weaponStatsHeatusageContainer.GetComponent<TextMeshProUGUI>().text = "Heat Usage: " + selectedLoadout.primaryWeapon.heatUsage;


        // // secondary weapon
        // GameObject secondaryWeaponImageContainer = secondaryContainer.GetChild(0).gameObject;
        // GameObject secondaryWeaponNameContainer = secondaryContainer.GetChild(1).gameObject;
        // GameObject secondaryWeaponDescriptionContainer = secondaryContainer.GetChild(2).gameObject;

        // GameObject secondaryWeaponStatsContainer = secondaryContainer.GetChild(3).gameObject;
        // GameObject secondaryWeaponStatsDamageContainer = secondaryWeaponStatsContainer.transform.GetChild(0).gameObject;
        // GameObject secondaryWeaponStatsFirerateContainer = secondaryWeaponStatsContainer.transform.GetChild(1).gameObject;
        // GameObject secondaryWeaponStatsHeatusageContainer = secondaryWeaponStatsContainer.transform.GetChild(2).gameObject;


        // secondaryWeaponImageContainer.GetComponent<Image>().sprite = selectedLoadout.secondaryWeapon.Icon2D;
        // secondaryWeaponNameContainer.GetComponent<TextMeshProUGUI>().text = selectedLoadout.secondaryWeapon.weaponName;
        // secondaryWeaponDescriptionContainer.GetComponent<TextMeshProUGUI>().text = selectedLoadout.secondaryWeapon.description;

        // secondaryWeaponStatsDamageContainer.GetComponent<TextMeshProUGUI>().text = "Damage: " + selectedLoadout.secondaryWeapon.damage;
        // secondaryWeaponStatsFirerateContainer.GetComponent<TextMeshProUGUI>().text = "Firerate: " + selectedLoadout.secondaryWeapon.fireRate;
        // secondaryWeaponStatsHeatusageContainer.GetComponent<TextMeshProUGUI>().text = "Heat Usage: " + selectedLoadout.secondaryWeapon.heatUsage;




    }

}
