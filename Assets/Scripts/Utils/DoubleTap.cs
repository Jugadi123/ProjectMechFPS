// boilerplate for mono behavior
using UnityEngine;
using System.Collections;
public class DoubleTap : MonoBehaviour
{
    [SerializeField] private float doubleTapWindow = 0.3f;
    private bool isWaiting = false;
    private KeyCode lastKeyPressed;
    
    public void CheckDoubleTap(KeyCode key, System.Action onDoubleTap)
    {
        if (Input.GetKeyDown(key))
        {
            if (isWaiting && key == lastKeyPressed)
            {
                onDoubleTap?.Invoke();
                isWaiting = false;
            }
            else
            {
                lastKeyPressed = key;
                StartCoroutine(DoubleTapTimeout(key));
            }
        }
    }

    IEnumerator DoubleTapTimeout(KeyCode key)
    {
        isWaiting = true;
        yield return new WaitForSeconds(doubleTapWindow);
        isWaiting = false;
    }
}