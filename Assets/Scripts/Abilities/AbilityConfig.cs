// AbilityConfig.cs (revised)
using UnityEngine;

public interface IAbilityConfig
{
    IAbility RuntimeAbility { get; }
    void SetUp(GameObject owner);
}

public abstract class AbilityConfig<TAbility> 
    : ScriptableObject, IAbilityConfig 
    where TAbility : IAbility, new()
{
    public TAbility RuntimeAbility { get; private set; }
    IAbility IAbilityConfig.RuntimeAbility => RuntimeAbility;

    // Because we’ve added SetUp(...) to the interface, this automatically satisfies it
    public virtual void SetUp(GameObject owner)
    {
        RuntimeAbility = new TAbility();
        RuntimeAbility.Initialize(owner, this);
    }
}
