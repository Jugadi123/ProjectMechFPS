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
    public NetworkVariable<float> playerWalkSpeed = new NetworkVariable<float>(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // sprint speed
    public NetworkVariable<float> playerSprintSpeed = new NetworkVariable<float>(10f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // dash speed
    public NetworkVariable<float> playerDashSpeed = new NetworkVariable<float>(30f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // acceleration
    public NetworkVariable<float> playerAcceleration = new NetworkVariable<float>(7f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // deceleration
    public NetworkVariable<float> playerDeceleration = new NetworkVariable<float>(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // gravity
    public NetworkVariable<float> gravity = new NetworkVariable<float>(-9.81f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    // fuel
    // max
    public NetworkVariable<float> fuelMax = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // regen
    public NetworkVariable<float> fuelRegenRate = new NetworkVariable<float>(20f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // regen delay
    public NetworkVariable<float> fuelRegenDelay = new NetworkVariable<float>(2f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // thruster fuel usage
    public NetworkVariable<float> thrusterFuelUsage = new NetworkVariable<float>(43f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // sprinting fuel usage
    public NetworkVariable<float> sprintingFuelUsage = new NetworkVariable<float>(10f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // dashing
    // dash cooldown
    public NetworkVariable<float> dashCoolDown = new NetworkVariable<float>(0.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // dash time
    public NetworkVariable<float> dashTime = new NetworkVariable<float>(0.2f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Thruster Force
    public NetworkVariable<float> thrusterForce = new NetworkVariable<float>(20f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

}
        