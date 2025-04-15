using UnityEngine;

public class Debugging : MonoBehaviour
{
    public static Debugging instance;

    public float defaultFps = 60f;

    public float defaultVolume = 100f;

    public float defaultMouseSensitivity = 2f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.Log("Found instance duplicate! Destroying...");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // set default frame rate
        Application.targetFrameRate = 105; 
    }


    public void SetGameFrameRate()
    {
        Application.targetFrameRate = (int)defaultFps;
    }
    

    public void SetGameVolume()
    {
        AudioListener.volume = defaultVolume / 100;
    }

}

