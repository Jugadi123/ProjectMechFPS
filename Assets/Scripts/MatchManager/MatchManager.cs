using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchManager : MonoBehaviour
{

    public static MatchManager Instance;

    public float roundTimeSeconds = 120f;

    public GameManager.GameStates CurrentMatchState;

    // static array of respawn points
    private GameObject[] allMapSpawnPoints;

    private List<Vector3> spawnLocations = new List<Vector3>();

    // a dynamic list consisting of players that need/waiting to be respawned. 
    public List<GameObject> allPlayers = new List<GameObject>();

    // events throughout the match
    public static event Action<string, string> OnPlayeDeath;

    public class KillFeedData {
        public int id;
        public string killMessage;
        public float killTime;
    }

    public int killid;

    private List<KillFeedData> KillFeedInfo = new List<KillFeedData>();

    void Awake()
    {
        if (Instance != null && Instance != this) {
            Debug.Log("Found instance duplicate! Destroying...");
            Destroy(gameObject); // Avoid duplicates
            return;
        }

        Instance = this;
    }

    void Start()
    {


        killid = 0;

        StartCoroutine(StartRoundTimer());
        // get and store all respawn locations
        allMapSpawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

        foreach (GameObject point in allMapSpawnPoints)
        {
            spawnLocations.Add(point.transform.position);
        }

        // at the start of the match or map load, add all players to a dynamic list
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player")) {
            allPlayers.Add(player);
        }

    }

    void FixedUpdate()
    {
        void PrepareRespawn(enemy stats, GameObject player) {

            // find the best spawn location (check enemy dist)
            
            Vector3 bestSpawnLocation = Vector3.zero;

            foreach (Vector3 spawnPosition in spawnLocations)
            {
                foreach (GameObject playerf in allPlayers) {
                    // ignore the player that needs to be respawned.
                    if (playerf == gameObject) return;
                    float distanceFromPlayer = Vector3.Distance(spawnPosition, playerf.transform.position);

                    if (distanceFromPlayer > 30f) {
                        Debug.Log(distanceFromPlayer);
                        bestSpawnLocation = spawnPosition;
                        break;
                    }
                }
            }

            // if a best location to spawn the player is found: spawn the player

            if (bestSpawnLocation != Vector3.zero) {
                stats.isAlive = true;
                stats.health = 100;
                player.transform.position = bestSpawnLocation;
                player.SetActive(true);
                Debug.Log("Respawned: " + player.name);
            }
        }

        foreach (GameObject player in allPlayers) {;

            enemy enemy = player.GetComponent<enemy>();

            if (enemy != null) {
                // if not alive, begin respawning this dude
                if (!enemy.isAlive) {
                    PrepareRespawn(enemy, player);
                }
            }
        }
    }

    public static void TriggerPlayerDeath(string victimName, string attackerName) {
        OnPlayeDeath?.Invoke(victimName, attackerName);
    }

    void OnEnable()
    {
        OnPlayeDeath += StoreKillFeed;
    }

    void OnDisable()
    {
        OnPlayeDeath -= StoreKillFeed;
    }
    
    void StoreKillFeed(string victimName, string attackerName ) {
        killid++;
        string message = "["+ killid + "] " + victimName + " killed " + attackerName;
        KillFeedInfo.Add(new KillFeedData {
            id = killid,
            killMessage = message,
            killTime = Time.time + 10f
        });
    }


    void DisplayKillFeed() {
        Rect killfeedRect = new Rect(
            new Vector2(Screen.width - 300, 50), 
            new Vector2 (290, 200)
        );

        GUIStyle killfeedMessageStyle = new GUIStyle(GUI.skin.label);
        killfeedMessageStyle.normal.textColor = Color.white;
        killfeedMessageStyle.fontSize = 15;
        killfeedMessageStyle.alignment = TextAnchor.MiddleLeft;

        int maxMessageLength = 40;

        // GUI.Box(killfeedRect, GUIContent.none);  // placeholder container

        float defaultPositionY = killfeedRect.y;


        if (KillFeedInfo.Count > 0) {
            // limit the amount of text on the killfeed
            if (KillFeedInfo.Count > 10) {
                KillFeedInfo.RemoveAt(0);
            }
            
            foreach (KillFeedData data in KillFeedInfo.ToList()) {
                // checking the expiry time for the current killfeed
                if (Time.time > data.killTime) {
                    KillFeedInfo.Remove(data);
                    continue;
                }

                defaultPositionY += 30f;

                Rect killfeedMessageRect = new Rect(
                    new Vector2(killfeedRect.x, defaultPositionY), 
                    new Vector2 (290, 30)
                );

                // GUI.Box(killfeedMessageRect, GUIContent.none);  // placeholder container


                string message = data.killMessage;

                if (message.Length >= maxMessageLength) {
                    string splitMessage = message.Substring(0, 40);
                    message = splitMessage + "...";
                }
            
                GUI.Label(killfeedMessageRect, message, killfeedMessageStyle);

            }
        }
    }

    void OnGUI() {
        DisplayKillFeed();
    }

    IEnumerator StartRoundTimer() {
        while (roundTimeSeconds > 0f) {
            yield return new WaitForSeconds(1f);
            roundTimeSeconds -= 1f;
        }
    }
}
