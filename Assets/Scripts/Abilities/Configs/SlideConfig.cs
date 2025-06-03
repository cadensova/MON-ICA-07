using UnityEngine;

[AddComponentMenu("Abilities/Slide Config")]
public class SlideConfig : AbilityConfig<SlideAbility>
{
    public bool IsActive = false;
    
    [Header("Runtime Parameters")]
    public bool HasBurst = false;

    [Header("Slide Parameters")]
    public float FULL_SPEED_DURATION = 0.6f;
    public float BURST_FORCE = 12f;
    public float REDUCE_FORCE = 2f;
    public float MOVEMENT_CONSERVATION_RATIO = 2 / 3f;
    
    [Tooltip("If current horizontal speed ≥ this, don’t add extra boost on landing.")]
    public float SPEED_THRESHOLD = 8f;
    

    [Header("Slide Conditions")]
    public float MINS_SPEED = 2f;
    public float COOLDOWN = 1f;
    public float COOLDOWN_JUMPING = 0.15f;

    [Header("Effects")]
    public float TILT_AMOUNT = 10f;
    public float SHRINK_SPEED = 0.25f;
    public float SLIDE_SCALE_Y = 0.5f;

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
