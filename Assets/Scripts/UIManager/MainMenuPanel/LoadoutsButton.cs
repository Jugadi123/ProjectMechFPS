using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
public class LoadoutsButton : MonoBehaviour, IPointerClickHandler
{
    private UIManager mainMenuManager;

    void Start()
    {
        mainMenuManager = UIManager.instance;
    }

    public void OnPointerClick(PointerEventData eventData) {
        mainMenuManager.currentPanelIndex = 1; // for loadouts panel
        mainMenuManager.ShowUpdatedPanel();
    }
}
