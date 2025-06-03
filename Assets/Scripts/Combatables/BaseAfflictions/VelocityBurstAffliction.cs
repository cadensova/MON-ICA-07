// VelocityBurstAffliction.cs
using UnityEngine;

/// <summary>
/// A SIMPLE affliction that doesn’t expire on its own (Duration = int.MaxValue).
/// Each time SliceSkill hits, we add one stack (up to MaxStacks).
/// PunchSkill will read & consume these stacks to boost knockback.
/// </summary>
public class VelocityBurstAffliction : Affliction
{
    /// <summary>
    /// How many extra force units each stack grants to the next punch.
    /// </summary>
    private readonly float extraForcePerStack;

    /// <summary>
    /// Creates a new VelocityBurstAffliction.
    /// </summary>
    /// <param name="extraForcePerStack">
    ///   How much extra force one stack grants.  
    ///   (PunchSkill can read CurrentStacks and multiply by this.)
    /// </param>
    /// <param name="maxStacks">
    ///   Maximum number of VelocityBurst stacks on a target.
    /// </param>
    public VelocityBurstAffliction(float extraForcePerStack, int maxStacks = 3)
        : base(name: "VelocityBurst",
               duration: int.MaxValue,         // never auto‐expires (SIMPLE type)
               isStackable: true,
               maxStacks: maxStacks,
               type: AType.SIMPLE)
    {
        this.extraForcePerStack = extraForcePerStack;
    }

    public override void OnApply(CombatActor actor)
    {
        base.OnApply(actor);
        // (Optional) Here you could spawn a tiny VFX so the player sees “burst stack gained.”
        // e.g. VFXManager.Spawn(“VelocityBurstIcon”, actor.transform.position);
    }

    public override bool TryStack(Affliction incoming)
    {
        bool stacked = base.TryStack(incoming);
        if (stacked && CurrentStacks <= MaxStacks)
        {
            // (Optional) Update VFX or UI: e.g. actor.ShowBurstStacks(CurrentStacks);
        }
        return stacked;
    }

    // Provide a way for PunchSkill to query “how much extra force per stack?”
    public float GetExtraForcePerStack()
    {
        return extraForcePerStack;
    }
}
