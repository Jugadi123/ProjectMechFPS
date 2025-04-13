using System.Collections;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class enemy : MonoBehaviour
{

    // our goal here is to write a class that is responsible for the enemy's death
    // detect if a ray that's traced from a player has hit us. If it has, subtract health.
    // If health is 0, turn on ragdoll physics and set a flag for player dead.
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float health;

    public bool isAlive;

    void Start()
    {
       health = 100f;
       isAlive = true;
    }

    public void Takedamage(float damage) {
        health -= damage;
        if(health <= 0f) {
            // usually an even is triggered here for player death
            isAlive = false;
            // using set active instead of destroying the object because respawning will be a thing in our game. No point in destroying objects and recreating them all the time when there's respawning. Of course it depends on the game mode too but rn let's keep things SIMPLE.
            gameObject.SetActive(false);
        }
    }
}
