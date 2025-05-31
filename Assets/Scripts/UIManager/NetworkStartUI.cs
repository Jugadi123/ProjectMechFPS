using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkStartUI : MonoBehaviour {
    [SerializeField] Button startHostButton;
    [SerializeField] Button startServerButton;
    [SerializeField] Button startClientButton;

    [SerializeField] GameObject clientPrefab;
    [SerializeField] GameObject serverPrefab;

    
    void Start() {
        startHostButton.onClick.AddListener(StartHost);
        startServerButton.onClick.AddListener(StartServer);
        startClientButton.onClick.AddListener(StartClient);
    }
    
    void StartHost() {
        Debug.Log("Starting host");
        NetworkManager.Singleton.StartHost();
        Hide();
    }

    void StartServer() {
        Debug.Log("Starting Server");
        NetworkManager.Singleton.StartServer();
        Hide();
    }

    void StartClient() {
        Debug.Log("Starting client");
        NetworkManager.Singleton.StartClient();
        Hide();
    }

    void Hide() => gameObject.SetActive(false);
}
