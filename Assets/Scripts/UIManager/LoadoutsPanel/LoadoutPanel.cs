using UnityEngine;
using TMPro;
public class LoadoutPanel : MonoBehaviour {

    // private UIManager mainMenuManager;
    public ScriptableLoadoutCollection loadoutCollection;
    public GameObject loadoutButtonsContainer;
    public GameObject loadoutButtonPrefab;
    public float loadoutButtonWidth = 240;
    public float loadoutButtonHeight = 64;
    public float loadoutButtonYGap = 10;
    
    public Vector2 defaultButtonPosition;

    void Start()
    {

        // Vector2 buttonsContainerPosition = loadoutButtonsContainer.GetComponent<RectTransform>().anchoredPosition;


        // Debug.Log(buttonsContainerPosition);

        defaultButtonPosition = new Vector2(4, - 20);
    

        // fill loadout UI
        foreach (ScriptableLoadout loadout in loadoutCollection.loadouts) {

            // handle button creation
            GameObject button = Instantiate(loadoutButtonPrefab, loadoutButtonsContainer.transform);

            button.GetComponent<RectTransform>().anchoredPosition = defaultButtonPosition;
            button.GetComponentInChildren<TextMeshProUGUI>().text = loadout.loadoutName;
            
            defaultButtonPosition.y -= loadoutButtonHeight + loadoutButtonYGap;
        }

        
    }      

}