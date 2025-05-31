using UnityEngine;

[AddComponentMenu("Abilities/Jump Config")]
public class JumpConfig : AbilityConfig<JumpAbility>
{
    public float JUMP_FORCE = 12f;
    public float JUMP_COOLDOWN = 0.15f;

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