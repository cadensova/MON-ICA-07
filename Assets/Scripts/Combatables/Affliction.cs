using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base for anything that can afflict a CombatActor.
/// Tracks name, remaining duration (in turns), whether it stacks, and how many stacks currently exist.
/// 
/// Concrete subclasses override OnApply, OnTick (called each turn), and OnExpire (called when duration reaches zero).
/// </summary>
public abstract class Affliction
{
    public string Name { get; protected set; }

    /// <summary>
    /// Number of turns remaining. When it hits 0, the affliction expires and OnExpire() runs.
    /// If you want a permanent buff, you can set Duration = int.MaxValue or skip ticking it.
    /// </summary>
    public int Duration { get; protected set; }

    /// <summary>
    /// True if multiple stacks of this same affliction can be applied
    /// (e.g. “Bleed” stacking 3 times). If false, re‐applying just refreshes Duration.
    /// </summary>
    public bool IsStackable { get; protected set; }

    /// <summary>
    /// How many stacks are currently on the target. If !IsStackable, this stays at 1.
    /// </summary>
    public int CurrentStacks { get; protected set; }

    /// <summary>
    /// The maximum number of stacks allowed. Only relevant if IsStackable = true.
    /// </summary>
    public int MaxStacks { get; protected set; }

    /// <summary>
    /// If this affliction is an “end‐of‐turn” effect (EOT), we’ll trigger OnTick() at end of actor’s turn.
    /// If it’s a simple “stat modifier” (SIMPLE), it might only apply once in OnApply and expire later.
    /// </summary>
    public enum AType { SIMPLE, EOT }
    public AType Type { get; protected set; }

    // A reference to who is afflicted. Assigned when applying to a CombatActor.
    protected CombatActor TargetActor;

    /// <summary>
    /// ctor is protected so only subclasses can instantiate.
    /// </summary>
    protected Affliction(string name, int duration, bool isStackable, int maxStacks, AType type)
    {
        Name = name;
        Duration = duration;
        IsStackable = isStackable;
        MaxStacks = Mathf.Max(1, maxStacks);
        Type = type;
        CurrentStacks = 1; // when first created, we have 1 stack by default
    }

    /// <summary>
    /// Called by CombatActor when this affliction is first applied.
    /// Subclasses should override to do initial stat changes / setup VFX, etc.
    /// </summary>
    public virtual void OnApply(CombatActor actor)
    {
        TargetActor = actor;
        // e.g. if a buff, apply stat mods now
        // If an EOT effect, you might just wait until the first OnTick.
    }

    /// <summary>
    /// Called each time the actor’s turn ends (if Type == EOT).
    /// Subclasses do whatever (deal damage, heal, etc.). Then reduce Duration by 1.
    /// </summary>
    public virtual void OnTick()
    {
        // Default implementation just decrements Duration.
        Duration = Mathf.Max(0, Duration - 1);
    }

    /// <summary>
    /// Called right when Duration reaches 0 (or if you explicitly remove it).
    /// Subclasses should override to remove stat buffs or clean up VFX.
    /// </summary>
    public virtual void OnExpire()
    {
        // e.g. revert any stat changes made in OnApply
    }

    /// <summary>
    /// Try to “stack” this affliction with another instance of the same type.
    /// Returns true if we successfully stacked (i.e. increased CurrentStacks or refreshed Duration),
    /// false if this affliction is not stackable or already at MaxStacks.
    /// </summary>
    public virtual bool TryStack(Affliction incoming)
    {
        // Only stack if they’re exactly the same subclass type and IsStackable == true
        if (!IsStackable || GetType() != incoming.GetType())
            return false;

        // If we’re under MaxStacks, increment the count and maybe reset duration.
        if (CurrentStacks < MaxStacks)
        {
            CurrentStacks++;
            // Optionally reset Duration back to full
            Duration = Mathf.Max(Duration, incoming.Duration);
            return true;
        }
        else
        {
            // Already at max stacks—just refresh Duration (but do not increase CurrentStacks)
            Duration = Mathf.Max(Duration, incoming.Duration);
            return true;
        }
    }


    /// <summary>
    /// Sets the duration of the affliction.
    /// </summary>
    public void SetDuration(int newDur)
    {
        Duration = newDur;
    }
}
