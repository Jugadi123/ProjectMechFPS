
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;

public class mainMenuUIManager : MonoBehaviour {

    public static mainMenuUIManager instance;

    public enum UISTATE {
        Home,
        Settings,
        WeaponSelect,
        clear
    }

    private bool UIGameStartButton;
    public bool UIGameSettingsButton;
    public bool UIGameLoadoutSelectButton;
    public bool UIGameQuitButton;
    public bool returnToMainMenuButton;

    public UISTATE currentUIState;

    // private static bool[] buttonsHierarchy = {
    //     false,
    //     false,
    //     false,
    //     false
    // };


    public int loadoutSelected;
    int numberOfAllowedLoadouts = 7;


    public class LoadoutDetails {
        public int id;
        public string name;
        public int[] weaponIDs; // array of weapon ids max of 2 (primary and secondary)
    }

    public List<LoadoutDetails> loudoutList = new List<LoadoutDetails>();

    public Texture2D smgIcon;
    public Texture2D shotgunIcon;
    public Texture2D rifleIcon;
    public Texture2D pistolIcon;
    public Texture2D sniperIcon;
    public Texture2D rocketLauncherIcon;


    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.Log("Found instance duplicate! Destroying...");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // check game state for main menu (if so, show all main menu UI (play, settings, maps select, inventory, etc))
        if (GameManager.Instance.CurrentGameState == GameManager.GameStates.MainMenu) {
            currentUIState = UISTATE.Home;
        }
    }

    void Start()
    {
        loadoutSelected = 0;
        // fill the list of loadouts
        for (int i = 0; i < numberOfAllowedLoadouts; i++) {
            loudoutList.Add(new LoadoutDetails{id = i, name = "Loadout " + i, weaponIDs = new int[]{Random.Range(0, 10), Random.Range(0, 10)}});
        }

    }



    void Update()
    {
        if (UIGameStartButton) {
            currentUIState = UISTATE.clear;
        }
        else if (UIGameLoadoutSelectButton ) {
            currentUIState = UISTATE.WeaponSelect;
        }
        else if (UIGameSettingsButton ) {
            currentUIState = UISTATE.Settings;
        }
        else if (UIGameQuitButton ) {
            // Application.Quit(0);
            currentUIState = UISTATE.clear;
            EditorApplication.isPlaying = false;
        }
        
        if (returnToMainMenuButton) {
            currentUIState = UISTATE.Home;
            returnToMainMenuButton = false;
        }
    }

    void OnGUI()
    {

        GUIStyle globalButtonStyle = new GUIStyle(GUI.skin.button);
        globalButtonStyle.normal.textColor = Color.white;
        globalButtonStyle.fontSize = 14;
        globalButtonStyle.alignment = TextAnchor.MiddleCenter;

        Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);

        // home UI
        if (currentUIState == UISTATE.Home) {

            UIGameStartButton = GUI.Button(new Rect(100, center.y - 200, 200, 50), "Start Game", globalButtonStyle);

        if (UIGameStartButton) {
            SceneManager.LoadSceneAsync("SampleScene");
        }

            UIGameLoadoutSelectButton = GUI.Button(new Rect(100, center.y - 100, 200, 50), "Select Loadout", globalButtonStyle);

            UIGameSettingsButton = GUI.Button(new Rect(100, center.y, 200, 50), "Settings", globalButtonStyle);

            UIGameQuitButton = GUI.Button(new Rect(100, center.y + 100, 200, 50), "Exit Game", globalButtonStyle);
        }

        // settings UI
        if (currentUIState == UISTATE.Settings) {
            // return to main menu button
            returnToMainMenuButton = GUI.Button(new Rect(100, center.y - 200, 100, 30), "< Back", globalButtonStyle);

            GUI.Box(new Rect(center.x - 200, center.y - 100, 400, 200), "Settings");
            // volume slider
            GUI.Label(new Rect(center.x - 100, center.y - 75, 200, 30), "Volume: " + Debugging.instance.defaultVolume);
          
            Debugging.instance.defaultVolume = GUI.HorizontalSlider(new Rect(center.x - 100, center.y - 50, 200, 30), Debugging.instance.defaultVolume, 0f, 100f);
            Debugging.instance.SetGameVolume();

            // fps slider
            GUI.Label(new Rect(center.x - 100, center.y - 25, 200, 30), "FPS: " + Debugging.instance.defaultFps);
            Debugging.instance.defaultFps = GUI.HorizontalSlider(new Rect(center.x - 100, center.y, 200, 30), Debugging.instance.defaultFps, 30.0f, 120.0f);
            Debugging.instance.defaultFps = Mathf.Round(Debugging.instance.defaultFps);
            Debugging.instance.SetGameFrameRate();

            // mouse sensitivity slider
            GUI.Label(new Rect(center.x - 100, center.y + 25, 200, 30), "Mouse Sensitivity: " + Debugging.instance.defaultMouseSensitivity);
            Debugging.instance.defaultMouseSensitivity = GUI.HorizontalSlider(new Rect(center.x - 100, center.y + 50, 200, 30), Debugging.instance.defaultMouseSensitivity, 0.0f, 50.0f);
        }

        // weapon select UI
        if (currentUIState == UISTATE.WeaponSelect) {
            returnToMainMenuButton = GUI.Button(new Rect(100, center.y - 200, 100, 30), "< Back", globalButtonStyle);

            
            Rect loadoutColumnRect = new Rect(center.x - Screen.width/3, center.y - 150, 150, 300);
            GUI.Box(loadoutColumnRect, "Your Loadouts");

            // loadout details rect
            Rect loadoutDetailsRect = new Rect(loadoutColumnRect.xMax + 5, loadoutColumnRect.y, 500, 300);
            GUI.Box(loadoutDetailsRect, "DETAILS");
            
            Rect loadoutButtonRect = new Rect(loadoutColumnRect.x, loadoutColumnRect.y + 5, loadoutColumnRect.width, 50);

            for (int i = 0; i < numberOfAllowedLoadouts; i++) {

                loadoutButtonRect.y += loadoutButtonRect.height + 1;

                // draw a thin line between loadouts
                if (i > 0) { // skip first one
                    Rect lineRect = new Rect(loadoutColumnRect.x, loadoutButtonRect.y, loadoutColumnRect.width, 1f);
                    GUI.DrawTexture(lineRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.white, 0, 0);
                }

                string loadoutId = "Loudout " + i;

                bool loudoutSelected = GUI.Button(loadoutButtonRect, loadoutId, globalButtonStyle);
                if (loudoutSelected) {
                    // get the loadout id from the button
                    loadoutSelected = int.Parse(loadoutId.Split(' ')[1]);
                }
            }


            // display an icon for both weapons (primary and secondary)
            // display weapon name below the icon
            // clicking the icon should open a window with the weapon stats

            // display the loadout details
            Rect primaryWeaponPreviewRect = new Rect(loadoutDetailsRect.x + 10, loadoutDetailsRect.y + 50, loadoutDetailsRect.width/2 - 10, 150);
            Rect secondaryWeaponPreviewRect = new Rect(loadoutDetailsRect.x + loadoutDetailsRect.width/2 + 10, loadoutDetailsRect.y + 50, loadoutDetailsRect.width/2 - 20, 150);

          
            GUI.Box(primaryWeaponPreviewRect, "Primary");
            GUI.Box(secondaryWeaponPreviewRect, "Secondary");
            GUI.DrawTexture(primaryWeaponPreviewRect, smgIcon, ScaleMode.ScaleToFit, true, 0, Color.white, 0, 0);
            GUI.DrawTexture(secondaryWeaponPreviewRect, sniperIcon, ScaleMode.ScaleToFit, true, 0, Color.white, 0, 0);
            GUI.Label(new Rect(primaryWeaponPreviewRect.x, primaryWeaponPreviewRect.y + 150, primaryWeaponPreviewRect.width, 20), "SMG 500");
            GUI.Label(new Rect(secondaryWeaponPreviewRect.x, secondaryWeaponPreviewRect.y + 150, secondaryWeaponPreviewRect.width, 20), "Sniper 200");
        

      


            // GUI.Label(new Rect(loadoutDetailsRect.x, loadoutDetailsRect.y + 20, loadoutDetailsRect.width, 20), "Name: " + loudoutList[loadoutSelected].name);

    
        }

    }
}