using UnityEngine;
using System;
public class NetworkTimer : MonoBehaviour {

    public static NetworkTimer Singleton;

    public const float SERVER_TICK_RATE = 60f;
    private float timer = 0f;
    public float tickInterval => 1f / SERVER_TICK_RATE;
    public ulong currentTick { get; private set; } = 0;

    public Action<ulong> onTickUpdate;

    void Awake()
    {
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

        while (timer >= tickInterval) {
            timer -= Time.deltaTime;
            currentTick++;
            onTickUpdate?.Invoke(currentTick);
        }
    }

    public float GetTickInterval() {
        return tickInterval;
    }

    public float GetTimeSinceLastTick() {
        return timer;
    }

    public float GetTimeUntilNextTick() {
        return tickInterval - timer;
    }
}