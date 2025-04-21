using UnityEngine;
using UnityEngine.EventSystems;
public class GlobalBackButton : MonoBehaviour, IPointerClickHandler
{
    public UIManager mainMenuManager;

    void Start()
    {
        mainMenuManager = UIManager.instance;
    }

    public void OnPointerClick(PointerEventData eventData) {
        mainMenuManager.currentPanelIndex = 0; // for main menu panel
        mainMenuManager.ShowUpdatedPanel();
    }
}
