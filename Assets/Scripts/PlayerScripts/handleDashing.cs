using UnityEngine;

public class handleDashing : MonoBehaviour
{

    private handleMovement movementScript;

    public AudioSource dashSound;

    void Start()
    {
        movementScript = GetComponent<handleMovement>();
    }

    void Update()
    {
        // play dash sound
        if (movementScript.IsDashing) {
            if (!dashSound.isPlaying)
            {
                dashSound.Play();
            }
        }
    }
}