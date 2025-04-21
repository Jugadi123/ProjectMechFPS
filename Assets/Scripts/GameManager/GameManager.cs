// This script is responsible for all game modes and states from menu to game end.
// A seperate gamestate manager script will then look at the current game mode and adjust the game's state accordingly.
// Which makes this script the highest level of logic or state.


using UnityEngine;
using UnityEngine.SceneManagement;
// hello world!
public class GameManager : MonoBehaviour {
    
    public static GameManager instance;

    public ScriptableLoadout selectedLoadout;

    public enum GameStates
    {
        MainMenu,
        LoadingMap,
        MatchWarmup,
        MatchLive,
        MatchEnd
    };

    public GameStates CurrentGameState;

    void Awake()
    {

        Debug.Log("hello from game state manager");
        if (instance != null && instance != this) {
            Debug.Log("Found instance duplicate! Destroying...");
            Destroy(gameObject); // Avoid duplicates
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        UpdateGameState(GameStates.MainMenu); // start with main menu
    }

    public void UpdateGameState(GameStates state) {
        CurrentGameState = state;
        Debug.Log("Game state changed: " + CurrentGameState);

        // change the scenes if a game state requires so
        UpdateSceneAfterGameState(CurrentGameState);
    }

    private void UpdateSceneAfterGameState(GameStates state) {

        if (state == GameStates.MainMenu) {
            SceneManager.LoadScene("MainMenuScene");
        } 
        else if (state == GameStates.LoadingMap) {

        }
        else if (state == GameStates.MatchWarmup) {

        }
        else if (state == GameStates.MatchLive) {
            SceneManager.LoadSceneAsync("GameScene");
        }
        else if (state == GameStates.MatchEnd) {
            // load back into main menu once the match is over.
            SceneManager.LoadSceneAsync("MainMenuScene");
        }
    }
}