using UnityEngine;
public class HandleFuelSystem : MonoBehaviour
{
    private const float TICK_INTERVAL = NetworkTimer.TickInterval;

    // fuel
    private float fuelCooldownTimer = 0f;
    private PlayerNetvars playerNetvars;

    private void Awake()
    {
        playerNetvars = GetComponent<PlayerNetvars>();
    }


    public void UpdateFuel(bool isThrusting, bool isSprinting)
    {

        var usingFuel = true;

        // 1. Fuel usage
        if (usingFuel && playerNetvars.fuel.Value > 0f)
        {
            float fuelCost = 0f;

            if (isThrusting)
                fuelCost += playerNetvars.thrusterFuelUsage.Value;
            if (isSprinting)
                fuelCost += playerNetvars.sprintingFuelUsage.Value;

            playerNetvars.fuel.Value = Mathf.Max(playerNetvars.fuel.Value - fuelCost * TICK_INTERVAL * 10f, 0f);

            // Reset cooldown
            fuelCooldownTimer = playerNetvars.fuelRegenDelay.Value;
            playerNetvars.IsFuelOnCooldown.Value = true;
        }

        // 2. Regen timer
        if (!usingFuel && playerNetvars.fuel.Value < playerNetvars.fuelMax.Value)
        {
            if (fuelCooldownTimer > 0f)
            {
                fuelCooldownTimer -= TICK_INTERVAL;
            }
            else
            {
                // 3. Regenerate
                playerNetvars.fuel.Value = Mathf.Min(playerNetvars.fuel.Value + playerNetvars.fuelRegenRate.Value * TICK_INTERVAL * 2f, playerNetvars.fuelMax.Value);
                playerNetvars.IsFuelOnCooldown.Value = false;
            }
        }
    }
}