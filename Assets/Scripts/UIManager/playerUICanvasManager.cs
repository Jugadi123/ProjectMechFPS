using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
public class playerUICanvasManager : NetworkBehaviour {


    [SerializeField] Camera mainCamera;

    public PlayerNetvars playerNetvars;

    private WeaponHandler weaponHandler;
    private Transform healthBarContainer;
    private Transform healthBarBar;
    private RectTransform healthBarRect;
    private float healthBarRectMaxWidth;
    private Color healthBarRectDefaultColor;
    private Color colorBasedOnHealth;

    private Transform criticalHealthText;

    private float heatBarRectMaxHeight;
    private Transform weaponHeatContainer;

    private Transform weaponHeatContainerprimary;
    private Transform primaryHeatBar;
    private TextMeshProUGUI primaryHeatStatusText;
    private float primaryVisualHeat;

    private Transform weaponHeatContainerSecondary;
    private Transform secondaryHeatBar;
    private TextMeshProUGUI secondaryHeatStatusText;
    private float secondaryVisualHeat;

    private Color defaultHeatColor;

    private float maxHealth = 100f;
    private float visualHealth;
    // actual networked health
    public float realHealth;

    private HandleMovement handleMovement;
    [SerializeField] private Transform movementDebugText;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        Cursor.lockState = CursorLockMode.Locked;

        playerNetvars = GetComponentInParent<PlayerNetvars>();

        visualHealth = 100f;
        realHealth = playerNetvars.health.Value;

        healthBarContainer = transform.GetChild(0);

        healthBarBar = healthBarContainer.GetChild(0);
        healthBarRect = healthBarBar.GetComponent<RectTransform>();
        healthBarRectMaxWidth = healthBarRect.sizeDelta.x;
        healthBarRectDefaultColor = healthBarBar.GetComponent<Image>().color;

        criticalHealthText = transform.GetChild(1);
        criticalHealthText.gameObject.SetActive(false);

        weaponHeatContainer = transform.GetChild(3);

        weaponHeatContainerprimary = weaponHeatContainer.GetChild(0);
        primaryHeatBar = weaponHeatContainerprimary.GetChild(0);
        primaryHeatStatusText = weaponHeatContainerprimary.GetChild(1).GetComponent<TextMeshProUGUI>();
        heatBarRectMaxHeight = primaryHeatBar.GetComponent<RectTransform>().sizeDelta.y;


        weaponHeatContainerSecondary = weaponHeatContainer.GetChild(1);
        secondaryHeatBar = weaponHeatContainerSecondary.GetChild(0);
        secondaryHeatStatusText = weaponHeatContainerSecondary.GetChild(1).GetComponent<TextMeshProUGUI>();
        heatBarRectMaxHeight = secondaryHeatBar.GetComponent<RectTransform>().sizeDelta.y;
        defaultHeatColor = secondaryHeatBar.GetComponent<Image>().color;

        weaponHandler = GetComponentInParent<WeaponHandler>();


        handleMovement = GetComponentInParent<HandleMovement>();
    }


    void DisplayHeatUI() {
        // heat related vars
        float maxHeat = 100f;
        float primaryCurrentHeat = weaponHandler.primaryCurrentHeat;
        float secondaryCurrentHeat = weaponHandler.secondaryCurrentHeat;
        bool primaryOverheated = weaponHandler.IsPrimaryOverheated;
        bool secondaryOverheated = weaponHandler.IsSecondaryOverheated;

        // for later use
        // float primaryHeatCoolDown = handleWeapons.primaryHeatCoolDown;
        // float secondaryHeatCoolDown = handleWeapons.secondaryHeatCoolDown;


        // primary


        primaryVisualHeat = Mathf.Lerp(primaryVisualHeat, primaryCurrentHeat, Time.deltaTime * 10f);
        
        float primaryHeatPercentage = primaryVisualHeat / maxHeat;

        primaryHeatPercentage = Mathf.Clamp01(primaryHeatPercentage);

        primaryHeatBar.GetComponent<RectTransform>().sizeDelta = new Vector2(primaryHeatBar.GetComponent<RectTransform>().sizeDelta.x, primaryHeatPercentage * heatBarRectMaxHeight);

        if (primaryOverheated) {
            float pingPongAlpha = Mathf.PingPong(Time.time * 5f, 1);
            primaryHeatStatusText.text = "OVER HEATED";
            primaryHeatStatusText.color = new Color(1, 0.1556604f, 0.1987359f, pingPongAlpha);
            primaryHeatBar.GetComponent<Image>().color = new Color(primaryHeatBar.GetComponent<Image>().color.r, primaryHeatBar.GetComponent<Image>().color.g, primaryHeatBar.GetComponent<Image>().color.b, pingPongAlpha);
        }
        else {
            primaryHeatStatusText.text = "HEAT: " + Mathf.Floor(primaryCurrentHeat) + "%";
            primaryHeatStatusText.color = Color.Lerp(primaryHeatStatusText.color, Color.white, Time.deltaTime * 3f);
            primaryHeatBar.GetComponent<Image>().color = defaultHeatColor;
        }


        // secondary

        secondaryVisualHeat = Mathf.Lerp(secondaryVisualHeat, secondaryCurrentHeat, Time.deltaTime * 10f);
        
        float secondaryHeatPercentage = secondaryVisualHeat / maxHeat;

        secondaryHeatPercentage = Mathf.Clamp01(secondaryHeatPercentage);

        secondaryHeatBar.GetComponent<RectTransform>().sizeDelta = new Vector2(secondaryHeatBar.GetComponent<RectTransform>().sizeDelta.x, secondaryHeatPercentage * heatBarRectMaxHeight);

        if (secondaryOverheated) {
            float pingPongAlpha = Mathf.PingPong(Time.time * 5f, 1);
            secondaryHeatStatusText.text = "OVER HEATED";
            secondaryHeatStatusText.color = new Color(1, 0.1556604f, 0.1987359f, pingPongAlpha);
            secondaryHeatBar.GetComponent<Image>().color = new Color(secondaryHeatBar.GetComponent<Image>().color.r, secondaryHeatBar.GetComponent<Image>().color.g, secondaryHeatBar.GetComponent<Image>().color.b, pingPongAlpha);
        }
        else {
            secondaryHeatStatusText.text = "HEAT: " + Mathf.Floor(secondaryCurrentHeat) + "%";
            secondaryHeatStatusText.color = Color.Lerp(secondaryHeatStatusText.color, Color.white, Time.deltaTime * 3f);
            secondaryHeatBar.GetComponent<Image>().color = defaultHeatColor;
        }
    }

    private void DisplayHealthUI()
    {
        visualHealth = Mathf.Lerp(visualHealth, realHealth, Time.deltaTime * 10f);

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

    void Update()
    {
        if (!IsOwner) return;

        realHealth = playerNetvars.health.Value;
        DisplayHealthUI();
        DisplayHeatUI();
        movementDebugText.GetComponent<TextMeshProUGUI>().text = "Velocity: " + handleMovement.currentVelocity.ToString() + " | Speed: " + Mathf.Floor(handleMovement.currentVelocity.magnitude);
    }
}