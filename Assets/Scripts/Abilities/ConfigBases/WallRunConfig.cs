// WallRunConfig.cs
using UnityEngine;

[CreateAssetMenu(
    fileName = "WallRunConfig",
    menuName = "Abilities/WallRun Config",
    order = 120)]
public class WallRunConfig : AbilityConfig<WallRunAbility>
{
    [Header("Runtime / State Tracking")]
    public bool IsActive = false;

    [Header("WallRun Parameters")]
    public float WALL_STICKINESS     = 0.5f;
    public float WALL_PUSH_OFF_FORCE = 5f;
    public float WALL_SLIP_SPEED     = 0.5f;
    public float WALL_RUN_GRACE      = 0.25f;
    public float WALL_RUN_COOLDOWN   = 0.5f;
    public float MAX_WALL_ANGLE = 150f;
    public float Wall_BOOST = 15f;

    [Header("Extras")]
    public float TILT_AMOUNT = 10f;

    public override void SetUp(GameObject owner)
    {
        if (owner == null)
        {
            Debug.LogError("WallRunConfig: Owner GameObject is null. Cannot set up WallRunAbility.");
            return;
        }
        base.SetUp(owner);
    }
}
