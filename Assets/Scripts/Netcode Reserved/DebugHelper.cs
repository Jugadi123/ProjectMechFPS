using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DebugHelper : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI debugTextElement;

    private TextMeshProUGUI debugText;

    // [SerializeField] Slider packetDelaySlider;
    // [SerializeField] Slider packetJitterSlider;
    // [SerializeField] Slider packetLossSlider;
    // [SerializeField] Slider packetLossIntervalSlider;

    void Awake()
    {
        Application.targetFrameRate = 70;
        debugText = debugTextElement.GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        float fps = Mathf.Floor(1f / Time.deltaTime);
        debugText.text = $"FPS: {fps}";
    }


}
