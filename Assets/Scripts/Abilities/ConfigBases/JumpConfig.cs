// JumpConfig.cs
using UnityEngine;

[CreateAssetMenu(
    fileName = "JumpConfig",
    menuName = "Abilities/Jump Config",
    order = 100)]
public class JumpConfig : AbilityConfig<JumpAbility>
{
    [Header("Runtime State (optional)")]
    public bool IsActive;       // you can still track “IsActive” here if you use it

    [Header("Jump Parameters")]
    public float JUMP_FORCE     = 12f;
    public float JUMP_COOLDOWN  = 0.15f;
    public float LOW_JUMP_MULTI = 2f;
    public float FALL_MULTI     = 2.5f;

    public override void SetUp(GameObject owner)
    {
        if (owner == null)
        {
            Debug.LogError("JumpConfig: Owner GameObject is null. Cannot set up JumpAbility.");
            return;
        }
        base.SetUp(owner);
    }
}
