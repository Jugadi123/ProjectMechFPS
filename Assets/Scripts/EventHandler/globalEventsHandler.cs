// a script to manage all global events that can be used by any script in the game at any time
// will be handling game state too

using UnityEngine;
public class globalEventsHandler : MonoBehaviour {

    // singleton instance of the game manager
    public static globalEventsHandler instance;

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
    }

    // All global game events mainly for state management
    public System.Action EVENT_GAME_LAUNCHED;
    public System.Action EVENT_GAME_EXITED;
    public System.Action EVENT_GAME_SHOW_MAIN_MENU;


    public void OnEnable() {
        EVENT_GAME_LAUNCHED += HandleGameLaunch;
        EVENT_GAME_EXITED += HandleGameExit;
    }

    public void OnDisable() {
        EVENT_GAME_LAUNCHED -= HandleGameLaunch;
        EVENT_GAME_EXITED -= HandleGameExit;
    }

    void HandleGameLaunch() {
        Debug.Log("Game launched!");
    }

    void HandleGameExit() {
        Debug.Log("Exiting Game...");
        // UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit(0);
    }


    void Start()
    {
        EVENT_GAME_LAUNCHED?.Invoke();
    }

    void OnGUI()
    {
        // EVENT_GAME_SHOW_MAIN_MENU?.Invoke();
    }

}