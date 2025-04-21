
// basically a sample database for the game's weapons and loadouts.

using UnityEngine;

[CreateAssetMenu(fileName = "Loadout Collection", menuName = "Loadouts/Loadout Collection")]
public class ScriptableLoadoutCollection : ScriptableObject {

    public ScriptableLoadout[] loadouts = new ScriptableLoadout[6];



}