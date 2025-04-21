
// singleton UI manager managing everything from events, panels, states, etc.
using System.Collections.Generic;
using UnityEngine;
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            // Debug.Log("Found instance duplicate! Destroying...");
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    // UI references
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject loadoutsPanel;
    [SerializeField] GameObject settingsPanel;

    // a global index which will keep track of the ui state basically. which panel is being shown right now and which ones to hide/
    public int currentPanelIndex;

    public GameObject[] menuPanels;

    void Start()
    {
        menuPanels = new GameObject[] {mainMenuPanel, loadoutsPanel, settingsPanel};
        currentPanelIndex = 0; // show the main menu by default
        ShowUpdatedPanel();
    }

    public void ShowUpdatedPanel() {
        for (int i = 0; i < menuPanels.Length; i++) {
            if (i == currentPanelIndex) {
                menuPanels[i].SetActive(true);
            }
            else {
                menuPanels[i].SetActive(false);
            }
        }
    }

    public void HideMainMenu() {
        foreach (GameObject panel in menuPanels) {
            panel.SetActive(false);
        }
    }

}
