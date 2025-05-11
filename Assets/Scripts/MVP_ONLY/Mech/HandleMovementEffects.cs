using UnityEngine;
using Unity.Netcode;
public class HandleMovementEffects : NetworkBehaviour
{
    private HandleMovement movementScript;
    public AudioSource dashSound;
    private AudioListener audioListener;

    void Start()
    {
        audioListener = GetComponent<AudioListener>();
        movementScript = GetComponent<HandleMovement>();
    }

    void Update()
    {
        // play dash sound only for us
        if (IsOwner) {
            if (movementScript.IsDashing && !dashSound.isPlaying)
            {
                dashSound.Play();
            }
        }
    }
}