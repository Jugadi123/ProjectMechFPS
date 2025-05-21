using UnityEngine;
using Unity.Netcode;
using TMPro;
public class fpsDisplay : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private TextMeshProUGUI fpsText;

    private void Awake()
    {
        fpsText = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        float fps = Mathf.Floor(1f / Time.deltaTime);
        fpsText.text = $"FPS: {fps}";
    }
}
