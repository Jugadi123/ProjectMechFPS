using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
public class SettingsButton : MonoBehaviour, IPointerClickHandler
{

    private UIManager mainMenuManager;

    void Start()
    {
        mainMenuManager = UIManager.instance;
    }

    public void OnPointerClick(PointerEventData eventData) {
        mainMenuManager.currentPanelIndex = 2; // for settings panel
        mainMenuManager.ShowUpdatedPanel();
    }
}
