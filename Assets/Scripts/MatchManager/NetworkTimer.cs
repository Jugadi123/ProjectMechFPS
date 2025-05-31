using UnityEngine;
using Unity.Netcode;
using System;
public class NetworkTimer : NetworkBehaviour {

    public static NetworkTimer Singleton { get; private set; }

    // Current tick (synced if clients need to know)
    public NetworkVariable<ulong> CurrentTick = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public const float SERVER_TICK_RATE = 60f;
    private float timer = 0f;
    public const float TickInterval = 1f / SERVER_TICK_RATE;


    public static event Action OnTick;


    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    public override void OnNetworkSpawn() {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(this);
    }

    void Update()
    {
        timer += Time.deltaTime;

        while (timer >= TickInterval)
        {
            timer -= TickInterval;

            OnTick?.Invoke();


            if (IsServer)
            {
                CurrentTick.Value++;
            }

        }
    }

    public int GetServerTickRate() {
        return (int)SERVER_TICK_RATE;
    }

    public float GetTickInterval() {
        return TickInterval;
    }

    public float GetTimeSinceLastTick() {
        return timer;
    }

    public float GetTimeUntilNextTick() {
        return TickInterval - timer;
    }
}