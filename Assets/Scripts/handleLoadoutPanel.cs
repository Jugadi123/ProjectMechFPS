using UnityEngine;
using TMPro;
using System;

public class handleLoadoutPanel : MonoBehaviour
{
    float buttonWidth = 240;
    float buttonHeight = 64;
    float buttonYSpacing = 10;

    public GameObject loadoutButtonPrefab;
    
    [SerializeField] GameObject loadoutButtonsContainer;

    private mainMenuUIManager mainMenuUI;

    private GameObject[] loadoutButtons = new GameObject[6];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainMenuUI = mainMenuUIManager.instance;

        mainMenuUI.selectedLoudoutIndex = 0;

        Vector2 buttonsDefaultPosition = new Vector2(0, 0);

        mainMenuUI = mainMenuUIManager.instance;
        for (int i = 0; i < mainMenuUI.AllLoudouts.Length; i++) {
            loadoutButtons[i] = Instantiate(loadoutButtonPrefab, loadoutButtonsContainer.transform);
            loadoutButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = mainMenuUI.AllLoudouts[i].name;
            buttonsDefaultPosition.y -= buttonHeight + buttonYSpacing;
            loadoutButtons[i].GetComponent<RectTransform>().anchoredPosition = buttonsDefaultPosition;
        }
    }


    // some events for the buttons

    // if invoked, we will update the entire loadout panel to reflect the current selected loadout
    // 1. Highlight the selected button using the selectedLoudoutIndex
    // 2. Update the loadout details panel to display the details of the selected loadout
    public event Action LoadoutPanelUpdated;

    public void UpdateLoadoutPanel() {
        LoadoutPanelUpdated?.Invoke();
    }

    void Update()
    {
        for (int i = 0; i < loadoutButtons.Length; i++) {
            Debug.Log(loadoutButtons[i].GetComponent<TextMeshProUGUI>().text);
        }
    }
}
