using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class playerUICanvasManager : MonoBehaviour {

    private handle_movement handle_Movement;
    private handleWeapons handleWeapons;
    private Transform healthBarContainer;
    private RectTransform healthBarContainerRect;
    private Transform healthBarBar;
    private RectTransform healthBarRect;
    private float healthBarRectMaxWidth;
    private Color healthBarRectDefaultColor;
    private Color colorBasedOnHealth;

    private Transform criticalHealthText;

    private float maxHealth = 100f;

    private bool isAlive;
    private float visualHealth;
    public float health;

    public float crosshairHeight;        
    public float crosshairWidth;            
    public float crosshairGap; 

    // private UIManager mainMenuManager;

    // public bool IsMenuOpen = false;
    // private bool MenuOpened = false;
    // private bool MenuClosed = false;


    void Awake()
    {
        visualHealth = 100f;
        health = 100f;
        isAlive = true;
    }

    void Start()
    {   
        healthBarContainer = transform.GetChild(0);
        healthBarContainerRect = healthBarContainer.GetComponent<RectTransform>();

        healthBarBar = healthBarContainer.GetChild(0);
        healthBarRect = healthBarBar.GetComponent<RectTransform>();
        healthBarRectMaxWidth = healthBarRect.sizeDelta.x;
        healthBarRectDefaultColor = healthBarBar.GetComponent<Image>().color;

        criticalHealthText = transform.GetChild(1);
        criticalHealthText.gameObject.SetActive(false);

        // mainMenuManager = UIManager.instance;
        handle_Movement = GetComponentInParent<handle_movement>();
        handleWeapons = GetComponentInParent<handleWeapons>();
    }


    // public void Takedamage(int damage) {
   
    //     if(health <= 0f) {
    //         // usually an even is triggered here for player death
    //         isAlive = false;
    //         // using set active instead of destroying the object because respawning will be a thing in our game. No point in destroying objects and recreating them all the time when there's respawning. Of course it depends on the game mode too but rn let's keep things SIMPLE.
    //         // gameObject.SetActive(false);
    //         return;
    //     }

    //     health -= damage;
    // }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Escape)) {
        //     IsMenuOpen = !IsMenuOpen;
        // }

        // if (IsMenuOpen) {
        //     Cursor.lockState = CursorLockMode.None;

        //     if (!MenuOpened) {
        //         Debug.Log("Menu just opened!");
        //         // always open the main menu first
        //         mainMenuManager.currentPanelIndex = 0; 
        //         mainMenuManager.ShowUpdatedPanel();
        //         MenuOpened = true;
        //     }
        //     MenuClosed = false;
        //     return;
        // }
        // else {

        //     if (!MenuClosed) {
        //         mainMenuManager.HideMainMenu();
        //         MenuClosed = true;
        //         Debug.Log("Menu just closed");
        //     }

        //     MenuOpened = false;
        //     Cursor.lockState = CursorLockMode.Locked;
        // }
        
        // if (Input.GetKeyDown(KeyCode.V)) {
        //     Takedamage(10);
        // }

        visualHealth = Mathf.Lerp(visualHealth, health, Time.deltaTime * 10f);

        Cursor.lockState = CursorLockMode.Locked;
        
        // using old health for lerping changes to health (visual aspect)
        float healthPercentage = visualHealth / maxHealth;

        healthBarRect.sizeDelta = new Vector2(healthPercentage * healthBarRectMaxWidth, healthBarRect.sizeDelta.y);

        // question: Why does time.deltatime not work for the pingpong function here? And will using time.time here be frame-independent?

        if (visualHealth < 25) {
            float pingPongAlpha = Mathf.PingPong(Time.time * 5f, 1);
            colorBasedOnHealth = new Color(1, 0.1556604f, 0.1987359f, pingPongAlpha);
            criticalHealthText.gameObject.SetActive(true);
            TextMeshProUGUI text = criticalHealthText.GetComponent<TextMeshProUGUI>();
            text.color = new Color(text.color.r, text.color.g, text.color.b, pingPongAlpha);
        }
        else {
            criticalHealthText.gameObject.SetActive(false);

            colorBasedOnHealth =  healthBarRectDefaultColor;
        }
        
        healthBarBar.GetComponent<Image>().color = Color.Lerp(healthBarBar.GetComponent<Image>().color, colorBasedOnHealth, Time.deltaTime * 3f);

    }
}