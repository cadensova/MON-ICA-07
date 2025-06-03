using UnityEngine;
using System.Collections;

public class SlideAbility : IAbility
{
    public string Id => "Slide";
    public bool IsActive => isSliding;
    public bool RequiresTicking => true;

    // References
    private Rigidbody rb;
    private CapsuleCollider col;
    private PlayerMovement movement;
    private SlideConfig config;

    // Runtime
    private float slideStartTime = 0f;
    private float lastSlideTime = 0f;
    private bool isSliding = false;
    private Vector3 slideDirection;

    // —— SCALING & COLLIDER FIELDS —— 
    private Vector3 originalScale;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    public void Initialize(GameObject owner, Object cfg = null)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Debug.LogError("SlideAbility ID is not set.");
            return;
        }

        if (cfg is not SlideConfig slideCfg)
        {
            Debug.LogError("Missing or invalid SlideConfig for SlideAbility.");
            return;
        }

        config = slideCfg;
        movement = owner.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError("SlideAbility needs a PlayerMovement component on the owner.");
            return;
        }

        rb = movement.rb;
        col = movement.col;
        if (rb == null || col == null)
        {
            Debug.LogError("SlideAbility: Owner is missing Rigidbody or CapsuleCollider.");
            return;
        }

        // Cache the player's original scale so we can grow back to it later
        originalScale = movement.ori.Find("Graphics").localScale;

        // Cache the capsule's original height & center
        originalColliderHeight = col.height;
        originalColliderCenter = col.center;

        // Listen for crouch “release” to cancel slide early if needed
        movement.input.Crouch.OnReleased += Cancel;
    }

    public void Activate() { /* not used */ }

    public void TryActivate()
    {
        // Check cooldown
        if (Time.time < lastSlideTime) return;

        // Need some movement input to slide
        if (movement.MoveInput.sqrMagnitude < 0.1f) return;

        // Start sliding
        isSliding = true;
        config.IsActive = true;
        slideStartTime = Time.time;
        config.HasBurst = false;

        slideDirection = movement.MoveDirection;

        // Let PlayerMovement handle drag/etc.
        movement.StartSlide();
        movement.AddRestraint(this);

        // Shrink both scale and collider
        movement.StartCoroutine(ChangeSize(true));

        // Camera tilt
        float lean = movement.input.MoveInput.x;
        float tiltAmount = config.TILT_AMOUNT * Mathf.Max(Mathf.Abs(lean), 0.35f);
        CameraTilt.Instance.SetTilt(tiltAmount * Mathf.Sign(lean), true);
    }


    private IEnumerator ChangeSize(bool shrinkOrGrow)
    {
        Transform playerT = movement.ori.Find("Graphics");
        float startY = playerT.localScale.y;
        float targetScaleY = shrinkOrGrow
            ? originalScale.y * config.SLIDE_SCALE_Y
            : originalScale.y;

        float speed = config.SHRINK_SPEED; // scale-units per second
        float fixedX = originalScale.x;
        float fixedZ = originalScale.z;

        // Cache original collider values for proportion:
        float origHeight = originalColliderHeight;
        float origCenterY = originalColliderCenter.y;

        while (!Mathf.Approximately(startY, targetScaleY))
        {
            // 1) Move the player's scale.y toward the target
            startY = Mathf.MoveTowards(startY, targetScaleY, speed * Time.deltaTime);
            playerT.localScale = new Vector3(fixedX, startY, fixedZ);

            
            col.height = origHeight * config.SLIDE_SCALE_Y;
            col.center = new Vector3(
                originalColliderCenter.x,
                origCenterY * config.SLIDE_SCALE_Y,
                originalColliderCenter.z
            );

            yield return null;
        }

        // Snap exact final values to avoid float drift
        playerT.localScale = new Vector3(fixedX, targetScaleY, fixedZ);
        float finalFrac = targetScaleY / originalScale.y;
        col.height = origHeight * finalFrac;
        col.center = new Vector3(
            originalColliderCenter.x,
            originalColliderCenter.y * finalFrac,
            originalColliderCenter.z
        );
    }

    public void Tick()
    {
        // (nothing needed here for slide)
    }

    public void FixedTick()
    {
        // Once grounded, do the redirect/burst logic exactly once
        if (!config.HasBurst && movement.IsGrounded)
        {
            config.HasBurst = true;

            Vector3 fullVel = rb.linearVelocity;
            Vector3 horizontalVel = new Vector3(fullVel.x, 0f, fullVel.z);
            float currentSpeed = horizontalVel.magnitude;

            Vector3 desiredDir = slideDirection.normalized;
            Vector3 redirectedVel = desiredDir * currentSpeed;
            rb.linearVelocity = new Vector3(redirectedVel.x, fullVel.y, redirectedVel.z);

            if (currentSpeed < config.SPEED_THRESHOLD)
            {
                float missingSpeed = config.SPEED_THRESHOLD - currentSpeed;
                float burstAmount = Mathf.Min(config.BURST_FORCE, missingSpeed);
                rb.AddForce(desiredDir * burstAmount, ForceMode.VelocityChange);
            }
        }

        // After the full-speed window, start applying drag
        if (slideStartTime + config.FULL_SPEED_DURATION < Time.time)
        {
            rb.AddForce(slideDirection * -config.REDUCE_FORCE, ForceMode.Acceleration);
        }

        // STOP CONDITIONS:  
        // 1) We're almost stopped on ground  
        // 2) We left the ground after we already did the burst  
        bool shouldStop = false;
        Vector3 vel2 = movement.GetVelocity();
        if (vel2.sqrMagnitude < config.MINS_SPEED) 
            shouldStop = true;
        if (!movement.IsGrounded && config.HasBurst)    
            shouldStop = true;

        if (shouldStop)
            Cancel();
    }

    public void Cancel()
    {
        // Apply cooldown (longer if in mid-jump)
        if (movement.sm.CurrentSub != PlayerStateMachine.SubState.Jumping)
            lastSlideTime = Time.time + config.COOLDOWN;
        else
            lastSlideTime = Time.time + config.COOLDOWN_JUMPING;

        config.HasBurst = false;
        slideStartTime = 0f;
        isSliding = false;
        config.IsActive = false;

        movement.RemoveRestraint(this);
        movement.StopSlide();

        // Grow back both scale & collider
        movement.StartCoroutine(ChangeSize(false));

        CameraTilt.Instance.ResetTilt();
    }
}
