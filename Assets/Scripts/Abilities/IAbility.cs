using UnityEngine;

public interface IAbility
{
    string Id { get; }
    bool IsActive { get; }
    bool RequiresTicking { get; }

    void Initialize(GameObject owner, Object config = null);
    void Activate();
    void TryActivate();
    void Tick();
    void FixedTick();
    void Cancel(); // if needed for things like charging or hold-to-cancel
}