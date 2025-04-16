
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;


public class mainMenuUIManager : MonoBehaviour  {

    public static mainMenuUIManager instance;

    public enum UISTATE {
        Home,
        Settings,
        LoudoutSelect,
        InGame,
    }

    public UISTATE currentUIState;

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

    // home menu elements
    private bool UIGameStartButton;
    public bool UIGameSettingsButton;
    public bool UIGameLoadoutSelectButton;
    public bool UIGameQuitButton;
    public bool returnToMainMenuButton;

    // loadout menu elements

    Rect LoadoutMasterContainer;
    Rect LoadoutSelectContainer;
    Rect LoudoutDetailsContainer;
    Rect LoudoutUtilsInfoContainer;
    Rect LoudoutPrimaryWeaponInfoContainer;
    Rect LoudoutSecondaryWeaponInfoContainer;



    public int selectedLoudoutIndex;

    public enum WeaponHitType {
        HitScan,
        Projectile,
    }

    public enum WeaponClass {
        SMG,
        Shotgun,
        Rifle,
        Sniper,
        RocketLauncher,
        GrenadeLauncher,
    }


    public struct WeaponStats {
        public int id;
        public string name;
        // public Texture2D icon; too much data to store. Instead I'll use a seperate function to retrieve the icon from the weapon's id
        public string description;
        public WeaponHitType type; // hitscan or projectile
        public WeaponClass weaponClass; // SMG or Rifle, etc
        public int damage;
        public int fireRate;
        public float heatUsage;
    };

    public class LoadoutDetails {
        public int id;
        public string name;
        public WeaponStats primaryWeapon;
        public WeaponStats secondaryWeapon;
    }

    public WeaponStats[] AllGameWeapons = new WeaponStats[6] {
        new WeaponStats {
            id = 0, 
            name = "Rifle", 
            description = "A mid-range rifle OP af.", 
            type = WeaponHitType.HitScan, 
            weaponClass = WeaponClass.Rifle, 
            damage = 120, 
            fireRate = 350, 
            heatUsage = 26
        },

        new WeaponStats {
            id = 1, 
            name = "Rocketeer", 
            description = "Basic rocket launcher", 
            type = WeaponHitType.Projectile, 
            weaponClass = WeaponClass.RocketLauncher, 
            damage = 400,
            fireRate = 100, 
            heatUsage = 50
        },

        new WeaponStats {
            id = 2, 
            name = "SMG", 
            description = "Of course it's a fucking smg.", 
            type = WeaponHitType.HitScan, 
            weaponClass = WeaponClass.SMG, 
            damage = 80, 
            fireRate = 500, 
            heatUsage = 10
        },

        new WeaponStats {
            id = 3, 
            name = "Nade Launcher", 
            description = "Thing that shoots grenades out.", 
            type = WeaponHitType.Projectile, 
            weaponClass = WeaponClass.GrenadeLauncher, 
            damage = 100, 
            fireRate = 200, 
            heatUsage = 30
        },

        new WeaponStats {
            id = 4, 
            name = "Auto Shotty", 
            description = "A death dealing automatic shotgun best at close range battles.", 
            type = WeaponHitType.HitScan, 
            weaponClass = WeaponClass.Shotgun, 
            damage = 50, 
            fireRate = 300, 
            heatUsage = 20
        },

        new WeaponStats {
            id = 5, 
            name = "Long Range Sniper", 
            description = "A long range heavy sniper. AWP from CS:GO.", 
            type = WeaponHitType.HitScan,
            weaponClass = WeaponClass.Sniper, 
            damage = 200,
            fireRate = 100,
            heatUsage = 34
        },
    };


    public LoadoutDetails[] AllLoudouts = new LoadoutDetails[6];


    public Vector2 ScreenCenter;
    public GUIStyle globalButtonStyle;
    private Vector2 scrollPosition = Vector2.zero;

    public int ButtonHeight = 50;
    public int ButtonWidth = 100;
    public int ButtonSpacing = 10;

    void Start()
    {
        // fill the loadouts with random stuff 
        // normally the loadout data would be stored in a database and loaded from there.
        for (int i = 0; i < AllLoudouts.Length; i++) {
            // choose a random weapon for the primary weapon
            WeaponStats randomWeapon = AllGameWeapons[Random.Range(0, AllGameWeapons.Length)];
            // choose a random weapon for the secondary weapon
            WeaponStats randomWeapon2 = AllGameWeapons[Random.Range(0, AllGameWeapons.Length)];

            AllLoudouts[i] = new LoadoutDetails {
                id = i,
                name = "Loadout " + i,
                primaryWeapon = randomWeapon,
                secondaryWeapon = randomWeapon2
            };
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


    public void DisplayLoadoutDetails() {
        // get the weapon icon and info by it's id
        LoadoutDetails loadout = AllLoudouts[selectedLoudoutIndex];

        if (loadout == null) {
            return;
        }

    }

    public void SelectLoadout(int index) {
        selectedLoudoutIndex = index;
    }


    public void RenderLoadoutPanel() {

        Vector2 ScreenSize = new Vector2(Screen.width, Screen.height);
    

        Rect returnToMainMenuButtonRect = new Rect(ScreenSize.x - ScreenSize.x + 100, ScreenSize.y - ScreenSize.y + 50, 100, 50);
        returnToMainMenuButton = GUI.Button(returnToMainMenuButtonRect, "< Back", globalButtonStyle);


        LoadoutMasterContainer = new Rect(returnToMainMenuButtonRect.xMin, returnToMainMenuButtonRect.yMax, ScreenSize.x/2 + 150, ScreenSize.y/2 + 200);
        GUI.Box(LoadoutMasterContainer, "LOADOUT MASTER WRAPPER");


        LoadoutSelectContainer = new Rect(LoadoutMasterContainer.xMin, LoadoutMasterContainer.yMin + 50, LoadoutMasterContainer.width - LoadoutMasterContainer.width + 200, LoadoutMasterContainer.height - 50);
        GUI.Label(LoadoutSelectContainer, "");


        LoudoutDetailsContainer = new Rect(LoadoutSelectContainer.xMax + 10, LoadoutSelectContainer.yMin, LoadoutMasterContainer.width - LoadoutSelectContainer.width - 10, LoadoutSelectContainer.height);
        // GUI.Box(LoudoutDetailsContainer, "DETAILS"); 


        LoudoutUtilsInfoContainer  = new Rect(LoudoutDetailsContainer.xMin, LoudoutDetailsContainer.yMax - 150, LoudoutDetailsContainer.width, 150);
        GUI.Box(LoudoutUtilsInfoContainer, "Utils/Abilities");
        

        LoudoutPrimaryWeaponInfoContainer  = new Rect(LoudoutDetailsContainer.xMin + 10, LoudoutDetailsContainer.yMin + 10, LoudoutDetailsContainer.width/2 - 10, LoudoutDetailsContainer.height - LoudoutUtilsInfoContainer.height - 10*2);
        GUI.Box(LoudoutPrimaryWeaponInfoContainer, "Primary Weapon");


        LoudoutSecondaryWeaponInfoContainer  = new Rect(LoudoutPrimaryWeaponInfoContainer.xMax + 10, LoudoutPrimaryWeaponInfoContainer.yMin, LoudoutPrimaryWeaponInfoContainer.width - 10, LoudoutPrimaryWeaponInfoContainer.height);
        GUI.Box(LoudoutSecondaryWeaponInfoContainer, "Secondary Weapon");


        // Calculate total content height
        float contentHeight = AllLoudouts.Length * (ButtonHeight + ButtonSpacing);
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
        for (int i = 0; i < AllLoudouts.Length; i++)
        {
            Rect buttonRect = new Rect(
                0,
                yPos,
                LoadoutSelectContainer.width - (needsScrolling ? 20 : 0),
                ButtonHeight
            );


            if (AllLoudouts[i] != null) { // shit check. need to fix this

                if (GUI.Button(buttonRect, AllLoudouts[i].name))
                {
                    // set the selected loadout index
                    SelectLoadout(AllLoudouts[i].id);
                }

            }
            
            yPos += ButtonHeight + ButtonSpacing;
        }
        
        // End the scroll view
        GUI.EndScrollView();



        // Render the loadout details
        DisplayLoadoutDetails();
        
    }

    void RenderMainMenu() {
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

    void OnGUI()
    {
        // RenderMainMenu();
    }
}