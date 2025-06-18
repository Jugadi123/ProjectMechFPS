// setup all netvars for player's mech
using UnityEngine;
using Unity.Netcode;
public class PlayerNetvars : NetworkBehaviour
{

    // general player related netvars

    // is alive

    public NetworkVariable<bool> isAlive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // health
    public NetworkVariable<float> health = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // player speeds
    // walk speed
    public NetworkVariable<float> maxWalkSpeed = new NetworkVariable<float>(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // sprint speed
    public NetworkVariable<float> maxSprintSpeed = new NetworkVariable<float>(12f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // dash speed
    public NetworkVariable<float> dashSpeed = new NetworkVariable<float>(35f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // normal air speed
    public NetworkVariable<float> minAirSpeed = new NetworkVariable<float>(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // max in air speed
    public NetworkVariable<float> maxAirSpeed = new NetworkVariable<float>(40f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // acceleration
    public NetworkVariable<float> acceleration = new NetworkVariable<float>(20f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // air acceleration
    public NetworkVariable<float> airAcceleration = new NetworkVariable<float>(10f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // deceleration
    public NetworkVariable<float> friction = new NetworkVariable<float>(6f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // gravity
    public NetworkVariable<float> gravity = new NetworkVariable<float>(-7.00f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // fuel
    // max
    public NetworkVariable<float> fuelMax = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // regen
    public NetworkVariable<float> fuelRegenRate = new NetworkVariable<float>(50f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // regen delay
    public NetworkVariable<float> fuelRegenDelay = new NetworkVariable<float>(2f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // thruster fuel usage
    public NetworkVariable<float> thrusterFuelUsage = new NetworkVariable<float>(50f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // sprinting fuel usage
    public NetworkVariable<float> sprintingFuelUsage = new NetworkVariable<float>(10f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> fuel = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsFuelOnCooldown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // dashing
    // dash cooldown
    public NetworkVariable<float> dashCooldown = new NetworkVariable<float>(0.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // dash duration
    public NetworkVariable<float> dashDuration = new NetworkVariable<float>(0.3f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    // Momentum jumping

    public NetworkVariable<float> jumpHeight = new NetworkVariable<float>(14f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> jumpVelocityMultiplier = new NetworkVariable<float>(1.3f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ground and air vars 

    public NetworkVariable<float> stickToGroundForce = new NetworkVariable<float>(-2f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
}
        