using UnityEngine;

[AddComponentMenu("Abilities/WallRun Config")]
public class WallRunConfig : AbilityConfig<WallRunAbility>
{
    public bool IsActive = false;

    [Header("WallRun Parameters")]
    public float WALL_STICKINESS = 0.5f;
    public float WALL_SLIP_SPEED = 0.5f;
    public float WALL_RUN_GRACE = 0.15f;

    public override void SetUp(GameObject owner)
    {
        if (owner == null)
        {
            Debug.LogError("Owner GameObject is null. Cannot set up WallRunConfig.");
            return;
        }
        base.SetUp(owner);
    }
}
