using UnityEngine;
using UnityEngine.EventSystems;

public class ExitButton : MonoBehaviour, IPointerClickHandler
{
    // Free any resources if needed
    public void OnPointerClick(PointerEventData eventData) {
        Application.Quit(0);
        // UnityEditor.EditorApplication.isPlaying = false;
    }
}
