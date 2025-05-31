using UnityEngine;

public interface IAbilityConfig
{
    IAbility RuntimeAbility { get; }
}

[RequireComponent(typeof(AbilityLoader))]
public abstract class AbilityConfig<TAbility> : MonoBehaviour, IAbilityConfig where TAbility : IAbility, new()
{
    public TAbility RuntimeAbility { get; private set; }

    IAbility IAbilityConfig.RuntimeAbility => RuntimeAbility;
    public virtual void SetUp(GameObject owner)
    {
        RuntimeAbility = new TAbility();
        RuntimeAbility.Initialize(owner, this);
    }

}