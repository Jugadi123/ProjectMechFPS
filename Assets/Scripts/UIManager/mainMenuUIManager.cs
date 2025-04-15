
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;

public class mainMenuUIManager : MonoBehaviour {

    public static mainMenuUIManager instance;

    public enum UISTATE {
        Home,
        Settings,
        LoudoutSelect,
        InGame,
    }

    private bool UIGameStartButton;
    public bool UIGameSettingsButton;
    public bool UIGameLoadoutSelectButton;
    public bool UIGameQuitButton;
    public bool returnToMainMenuButton;

    public UISTATE currentUIState;

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
    public Vector2 ScreenCenter;
    public GUIStyle globalButtonStyle;

    private Vector2 scrollPosition = Vector2.zero;


    private string[] ButtonLabels = new string[] {"Loadout 0", "Loadout 1", "Loadout 2", "Loadout 3", "Loadout 4", "Loadout 5", "Loadout 6", "Loadout 7"};
    public int ButtonHeight = 50;
    public int ButtonWidth = 100;
    public int ButtonSpacing = 10;
    


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
        if (GameManager.instance.CurrentGameState == GameManager.GameStates.MainMenu) {
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
            currentUIState = UISTATE.InGame;
        }
        else if (UIGameLoadoutSelectButton ) {
            currentUIState = UISTATE.LoudoutSelect;
        }
        else if (UIGameSettingsButton ) {
            currentUIState = UISTATE.Settings;
        }
        else if (UIGameQuitButton ) {
            // Application.Quit(0);
            EditorApplication.isPlaying = false;
        }
    
        if (returnToMainMenuButton) {
            currentUIState = UISTATE.Home;
            returnToMainMenuButton = false;
        }
    }


    private void SelectDefaultLoadout(int index) {
        Debug.Log("Loadout " + index + " selected");
    }


    private void RenderLoadoutPanel() {

        Vector2 ScreenSize = new Vector2(Screen.width, Screen.height);
    

        Rect returnToMainMenuButtonRect = new Rect(ScreenSize.x - ScreenSize.x + 100, ScreenSize.y - ScreenSize.y + 50, 100, 50);
        returnToMainMenuButton = GUI.Button(returnToMainMenuButtonRect, "< Back", globalButtonStyle);


        Rect LoadoutMasterContainer = new Rect(returnToMainMenuButtonRect.xMin, returnToMainMenuButtonRect.yMax, ScreenSize.x/2 + 150, ScreenSize.y/2 + 200);
        GUI.Box(LoadoutMasterContainer, "LOADOUT MASTER WRAPPER");


        Rect LoadoutSelectContainer = new Rect(LoadoutMasterContainer.xMin, LoadoutMasterContainer.yMin + 50, LoadoutMasterContainer.width - LoadoutMasterContainer.width + 200, LoadoutMasterContainer.height - 50);
        GUI.Label(LoadoutSelectContainer, "");


        Rect LoudoutDetailsContainer = new Rect(LoadoutSelectContainer.xMax + 10, LoadoutSelectContainer.yMin, LoadoutMasterContainer.width - LoadoutSelectContainer.width - 10, LoadoutSelectContainer.height);
        // GUI.Box(LoudoutDetailsContainer, "DETAILS"); 


        Rect LoudoutUtilsInfoContainer  = new Rect(LoudoutDetailsContainer.xMin, LoudoutDetailsContainer.yMax - 150, LoudoutDetailsContainer.width, 150);
        GUI.Box(LoudoutUtilsInfoContainer, "Utils/Abilities");
        

        Rect LoudoutPrimaryWeaponInfoContainer  = new Rect(LoudoutDetailsContainer.xMin + 10, LoudoutDetailsContainer.yMin + 10, LoudoutDetailsContainer.width/2 - 10, LoudoutDetailsContainer.height - LoudoutUtilsInfoContainer.height - 10*2);
        GUI.Box(LoudoutPrimaryWeaponInfoContainer, "Primary Weapon");


        Rect LoudoutSecondaryWeaponInfoContainer  = new Rect(LoudoutPrimaryWeaponInfoContainer.xMax + 10, LoudoutPrimaryWeaponInfoContainer.yMin, LoudoutPrimaryWeaponInfoContainer.width - 10, LoudoutPrimaryWeaponInfoContainer.height);
        GUI.Box(LoudoutSecondaryWeaponInfoContainer, "Secondary Weapon");




        // Calculate total content height
        float contentHeight = ButtonLabels.Length * (ButtonHeight + ButtonSpacing);
        bool needsScrolling = contentHeight > LoadoutSelectContainer.height;
        
        // Begin the scroll view
        scrollPosition = GUI.BeginScrollView(
            LoadoutSelectContainer,
            scrollPosition,
            new Rect(0, 0, LoadoutSelectContainer.width - (needsScrolling ? 20 : 0), contentHeight),
            false,
            needsScrolling
        );
        
        // Draw buttons
        float yPos = 0;
        for (int i = 0; i < ButtonLabels.Length; i++)
        {
            Rect buttonRect = new Rect(
                0,
                yPos,
                LoadoutSelectContainer.width - (needsScrolling ? 20 : 0),
                ButtonHeight
            );
            
            if (GUI.Button(buttonRect, ButtonLabels[i]))
            {
                SelectDefaultLoadout(i);
            }
            
            yPos += ButtonHeight + ButtonSpacing;
        }
        
        // End the scroll view
        GUI.EndScrollView();
        
    }

    void OnGUI()
    {
        

        globalButtonStyle = new GUIStyle(GUI.skin.button);
        globalButtonStyle.normal.textColor = Color.white;
        globalButtonStyle.fontSize = 14;
        globalButtonStyle.alignment = TextAnchor.MiddleCenter;

        ScreenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        // home UI
        if (currentUIState == UISTATE.Home) {

            UIGameStartButton = GUI.Button(new Rect(100, ScreenCenter.y - 200, 200, 50), "Start Game", globalButtonStyle);

        if (UIGameStartButton) {
            SceneManager.LoadSceneAsync("GameScene");
        }

            UIGameLoadoutSelectButton = GUI.Button(new Rect(100, ScreenCenter.y - 100, 200, 50), "Select Loadout", globalButtonStyle);

            UIGameSettingsButton = GUI.Button(new Rect(100, ScreenCenter.y, 200, 50), "Settings", globalButtonStyle);

            UIGameQuitButton = GUI.Button(new Rect(100, ScreenCenter.y + 100, 200, 50), "Exit Game", globalButtonStyle);
        }

        // settings UI
        if (currentUIState == UISTATE.Settings) {
            // return to main menu button
            returnToMainMenuButton = GUI.Button(new Rect(100, ScreenCenter.y - 200, 100, 30), "< Back", globalButtonStyle);

            GUI.Box(new Rect(ScreenCenter.x - 200, ScreenCenter.y - 100, 400, 200), "Settings");
            // volume slider
            GUI.Label(new Rect(ScreenCenter.x - 100, ScreenCenter.y - 75, 200, 30), "Volume: " + Debugging.instance.defaultVolume);
          
            Debugging.instance.defaultVolume = GUI.HorizontalSlider(new Rect(ScreenCenter.x - 100, ScreenCenter.y - 50, 200, 30), Debugging.instance.defaultVolume, 0f, 100f);
            Debugging.instance.SetGameVolume();

            // fps slider
            GUI.Label(new Rect(ScreenCenter.x - 100, ScreenCenter.y - 25, 200, 30), "FPS: " + Debugging.instance.defaultFps);
            Debugging.instance.defaultFps = GUI.HorizontalSlider(new Rect(ScreenCenter.x - 100, ScreenCenter.y, 200, 30), Debugging.instance.defaultFps, 30.0f, 120.0f);
            Debugging.instance.defaultFps = Mathf.Round(Debugging.instance.defaultFps);
            Debugging.instance.SetGameFrameRate();

            // mouse sensitivity slider
            GUI.Label(new Rect(ScreenCenter.x - 100, ScreenCenter.y + 25, 200, 30), "Mouse Sensitivity: " + Debugging.instance.defaultMouseSensitivity);
            Debugging.instance.defaultMouseSensitivity = GUI.HorizontalSlider(new Rect(ScreenCenter.x - 100, ScreenCenter.y + 50, 200, 30), Debugging.instance.defaultMouseSensitivity, 0.0f, 50.0f);
        }

        // weapon select UI
        if (currentUIState == UISTATE.LoudoutSelect) {
            RenderLoadoutPanel();
        }

    }
}