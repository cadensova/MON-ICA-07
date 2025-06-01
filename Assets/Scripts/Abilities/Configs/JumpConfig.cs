using UnityEngine;

[AddComponentMenu("Abilities/Jump Config")]
public class JumpConfig : AbilityConfig<JumpAbility>
{
    public bool IsActive;

    [Header("Jump Parameters")]
    public float JUMP_FORCE = 12f;
    public float JUMP_COOLDOWN = 0.15f;

    public float LOW_JUMP_MULTI = 2f;
    public float FALL_MULTI = 2.5f;

    public override void SetUp(GameObject owner)
    {
        if (owner == null)
        {
            Debug.LogError("Owner GameObject is null. Cannot set up JumpConfig.");
            return;
        }

        base.SetUp(owner);
    }
}