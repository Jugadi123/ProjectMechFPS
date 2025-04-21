using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour, IPointerClickHandler
{
    public UIManager mainMenuManager;

    public GameManager gameManager;

    void Start()
    {
        gameManager = GameManager.instance;
        mainMenuManager = UIManager.instance;
    }

    public void OnPointerClick(PointerEventData eventData) {
        // load main scene
        SceneManager.LoadSceneAsync("GameScene");

        // hide menu
        mainMenuManager.HideMainMenu();
        gameManager.UpdateGameState(GameManager.GameStates.MatchLive);
        gameObject.SetActive(false);
    }
}
