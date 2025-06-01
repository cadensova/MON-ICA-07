using UnityEngine;

[AddComponentMenu("Abilities/Slide Config")]
public class SlideConfig : AbilityConfig<SlideAbility>
{
    public bool IsActive = false;
    
    [Header("Runtime Parameters")]
    public bool HasBurst = false;

    [Header("Slide Parameters")]
    public float FULL_SPEED_DURATION = 0.6f;
    public float SLIDE_BURST_FORCE = 12f;
    public float SLIDE_REDUCE_FORCE = 2f;
    public float MOVEMENT_CONSERVATION_RATIO = 2 / 3f;
    
    [Tooltip("If current horizontal speed ≥ this, don’t add extra boost on landing.")]
    public float SLIDE_SPEED_THRESHOLD = 8f;
    

    [Header("Slide Conditions")]
    public float SLIDE_MINS_SPEED = 2f;
    public float SLIDE_COOLDOWN = 1f;
    public float SLIDE_COOLDOWN_JUMPING = 0.15f;

    [Header("Effects")]
    public float SLIDE_TILT_AMOUNT = 10f;

    public override void SetUp(GameObject owner)
    {
        if (owner == null)
        {
            Debug.LogError("Owner GameObject is null. Cannot set up SlideConfig.");
            return;
        }
        base.SetUp(owner);
    }
}
