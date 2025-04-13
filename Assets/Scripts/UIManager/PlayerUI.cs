using System;
using Unity.Mathematics;
using UnityEngine;
class PlayerUI : MonoBehaviour {

    [SerializeField] Camera cockpitCamera;

    [SerializeField] Texture2D crosshairTexture;
    private handle_movement handle_Movement;

    private float roundTime;

    private float oldHealth;
    public float health;
    private float newHealth;
    public bool isAlive;


    private string weaponAmmoText;

    private float fuelPercentage;
    private string fuelText;

    private float healthPercentage;
    private string healthText;

    public float crosshairHeight;        
    public float crosshairWidth;            
    public float crosshairGap; 

    [SerializeField] Transform enemy;

    [SerializeField] handleWeapons weaponInfo;

    [SerializeField] int fps;


    void Awake()
    {
        health = 100f;
        newHealth = health;
        isAlive = true;

    }

    void Start()
    {
        handle_Movement = GetComponent<handle_movement>();
        weaponInfo = GetComponent<handleWeapons>();
        crosshairHeight = 1f;        
        crosshairWidth = 10f;            
        crosshairGap = 10f; 
    }


    public void Takedamage(int damage) {
        health -= damage;
        if(health <= 0f) {
            // usually an even is triggered here for player death
            isAlive = false;
            // using set active instead of destroying the object because respawning will be a thing in our game. No point in destroying objects and recreating them all the time when there's respawning. Of course it depends on the game mode too but rn let's keep things SIMPLE.
            gameObject.SetActive(false);
        }
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) {
            oldHealth = health;
            Takedamage(10);
        }

        weaponAmmoText = (weaponInfo.ammo + " / ∞").ToString();
        if (weaponInfo.startReloading) {
            weaponAmmoText = "Reloading...";
        }

        // roundTime = MatchManager.Instance.roundTimeSeconds;
        // string formattedRoundTimer = string.Format("{0:00}:{1:00}", roundTime / 60, roundTime % 60);
    
        fuelPercentage = (handle_Movement.currentFuelAmount / handle_Movement.fuelMax);
        fuelText = ("Fuel: " + Math.Round(fuelPercentage * 100, 1)).ToString();


        // newHealth = Mathf.Lerp(oldHealth, health, Time.deltaTime * 2f);
        // Debug.Log(newHealth);
      
        healthPercentage = health / 100;
        healthText = ("Health: " + Math.Round(healthPercentage * 100)).ToString();
    }

    // legacy renderer but idc because it gets the job done the quickest.
    void OnGUI()
    {
        Rect weaponInfoRect = new Rect(Screen.width/2 + 300, Screen.height/2 + 180, 110, 30);

        Rect healthBarOuterRect = new Rect(Screen.width/2 - 400, Screen.height/2 + 180, 200, 15);
        Rect healthBarInnerRect = new Rect(Screen.width/2 - 398, Screen.height/2 + 182, healthPercentage * 196, 11);
        Rect healthTextRect = new Rect(Screen.width/2 - 400, Screen.height/2 + 170, 50, 11);
        Color healthBarInnerRectColor = new Color(0f, 255/255f, 0f, 1f);
        Color healthLebelColor = Color.green;
        GUI.DrawTexture(healthBarOuterRect, Texture2D.blackTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.5f), 0, 0);
        GUI.DrawTexture(healthBarInnerRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, healthBarInnerRectColor, 0, 0);
        GUIStyle healthLabelStyle = new GUIStyle();
        healthLabelStyle.fontSize = 10;
        healthLabelStyle.normal.textColor = healthLebelColor;

        GUI.Label(healthTextRect, healthText, healthLabelStyle);




        Rect fuelBarOuterRect = new Rect(Screen.width/2 - 100, Screen.height/2 + 180, 200, 30);

        float fuelBarInnerRectLerpedEnd = Mathf.Lerp(0, 196, fuelPercentage);

        Rect fuelBarInnerRect = new Rect(Screen.width/2 - 98, Screen.height/2 + 182, fuelBarInnerRectLerpedEnd, 26);

        Rect fuelTextRect = new Rect(Screen.width/2 - 98, Screen.height/2 + 182, 196, 26);

        Color fuelBarInnerRectColor = new Color(255/255f, 221/255f, 0f, 1f); 

        Color fuelLebelColor = Color.black; 
        
        GUI.DrawTexture(fuelBarOuterRect, Texture2D.blackTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.5f), 0, 0);

        if (fuelPercentage * 100 < 25) {
            fuelBarInnerRectColor = Color.red;
            fuelLebelColor = Color.red;
            fuelBarInnerRectColor.a = Mathf.PingPong(Time.time * 3f, 1);
            fuelLebelColor.a = Mathf.PingPong(Time.time * 3f, 1);
            fuelText = "FUEL LOW";
        }

        GUIStyle fuelLebelStyle = new GUIStyle(GUI.skin.label);
        fuelLebelStyle.normal.textColor = fuelLebelColor;
        fuelLebelStyle.fontSize = 14;
        fuelLebelStyle.alignment = TextAnchor.MiddleCenter;


        GUI.DrawTexture(fuelBarInnerRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, fuelBarInnerRectColor, 0, 0);
        GUI.Label(fuelTextRect, fuelText, fuelLebelStyle);


        // crosshair
        Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);

        Rect topLeftLineRect = new Rect(center.x - crosshairGap - crosshairWidth, center.y - crosshairGap, crosshairWidth, crosshairHeight);
        Rect topRightLineRect = new Rect(center.x + crosshairGap, center.y - crosshairGap, crosshairWidth, crosshairHeight);
        Rect bottomLeftLineRect = new Rect(center.x - crosshairGap - crosshairWidth, center.y + crosshairGap, crosshairWidth, crosshairHeight);
        Rect bottomRightLineRect = new Rect(center.x + crosshairGap, center.y + crosshairGap, crosshairWidth, crosshairHeight);
        Rect centerDotRect = new Rect(center.x - 2, center.y - 2, 4, 4);
    
    
        // crosshair dynamic color check to see if our crosshair is over an enemy collider (mesh)
        // default color
        Color crosshairColor = new Color(1, 1, 1, 0.8f);


        // Vector3 enemyWorldPos = cockpitCamera.WorldToScreenPoint(enemy.transform.position);
        // enemyWorldPos.y = Screen.height - enemyWorldPos.y;

        CapsuleCollider enemyCollider = enemy.GetComponent<CapsuleCollider>();


        // fuck that just check for a trace hitting it
        if (enemyCollider != null && weaponInfo.hitInfoLOS.transform == enemy) {
            crosshairColor = new Color(138/255, 189/255, 255/255, 0.8f);
            crosshairGap = Mathf.Lerp(crosshairGap, 13, Time.deltaTime * 10f);
        }
        else {
            crosshairGap = Mathf.Lerp(crosshairGap, 10, Time.deltaTime * 10f);
        }
        




        // if ((enemyWorldPos.x > topLeftLineRect.x && enemyWorldPos.x < bottomRightLineRect.xMax) && (enemyWorldPos.y > topLeftLineRect.y && enemyWorldPos.y < bottomRightLineRect.yMax)) {
        //     crosshairColor = Color.green;
        // }







        // Top-left line
        GUIUtility.RotateAroundPivot(45f, topLeftLineRect.center);
        GUI.DrawTexture(topLeftLineRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, crosshairColor, 0, 5f);
        // reset rotation for next element
        GUIUtility.RotateAroundPivot(-45f, topLeftLineRect.center);
        
        // Bottom-right line
        GUIUtility.RotateAroundPivot(45f, bottomRightLineRect.center);
        GUI.DrawTexture(bottomRightLineRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, crosshairColor, 0, 5f);
        // reset rotation for next element
        GUIUtility.RotateAroundPivot(-45f, bottomRightLineRect.center);

        // Bottom-left line
        GUIUtility.RotateAroundPivot(-45f, bottomLeftLineRect.center);
        GUI.DrawTexture(bottomLeftLineRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, crosshairColor, 0, 5f);
        // reset rotation for next element
        GUIUtility.RotateAroundPivot(45f, bottomLeftLineRect.center);

        // Top-right line
        GUIUtility.RotateAroundPivot(-45f, topRightLineRect.center);
        GUI.DrawTexture(topRightLineRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, crosshairColor, 0, 5f);
        // reset rotation for next element
        GUIUtility.RotateAroundPivot(45f, topRightLineRect.center);


        // Center-dor rect
        GUI.DrawTexture(centerDotRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, crosshairColor, 0, 1f);
        


        GUIStyle ammoLebelStyle = new GUIStyle(GUI.skin.label);
        ammoLebelStyle.normal.textColor = Color.white;
        ammoLebelStyle.fontSize = 20;
        ammoLebelStyle.alignment = TextAnchor.MiddleCenter;
        ammoLebelStyle.fontStyle = FontStyle.Bold;
        GUI.Label(weaponInfoRect, weaponAmmoText, ammoLebelStyle);

    }
}