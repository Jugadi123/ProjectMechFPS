using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Collections.Generic;
public class Preloader : MonoBehaviour {

    private UnityTransport transport;
    private string ipAddress;
    private ushort port;

    private List<string> commandLineArgs = new List<string>();
    
    private void Awake()
    {
        Application.targetFrameRate = 70;

        ipAddress = "127.0.0.1";
        port = 7777;

        transport = GetComponent<UnityTransport>();
        transport.SetConnectionData(ipAddress, port);

        string[] cmdArgs = Environment.GetCommandLineArgs();

        foreach (string arg in cmdArgs) {
            commandLineArgs.Add(arg);
        }
    }

    private void Start()
    {
        // for headless build
        if (commandLineArgs.Contains("-server")) {
            Logging.LogColor(ConsoleColor.Green, "Build: Server");
            NetworkManager.Singleton.StartServer();
        }
        else {
            // Logging.LogColor(ConsoleColor.Green, "Build: Client");
            // NetworkManager.Singleton.StartClient();
        }
    }
}