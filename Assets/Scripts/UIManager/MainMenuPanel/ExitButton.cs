using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;

public class ExitButton : MonoBehaviour, IPointerClickHandler
{
    // Free any resources if needed
    public void OnPointerClick(PointerEventData eventData) {
        // Application.Quit()
        EditorApplication.isPlaying = false;
    }
}
