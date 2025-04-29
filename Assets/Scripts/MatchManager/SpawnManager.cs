using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "ScriptableSpawnPoints", menuName = "ScriptableSpawnPoints")]
public class SpawnManager : ScriptableObject {
    public List<GameObject> spawnPoints;
}