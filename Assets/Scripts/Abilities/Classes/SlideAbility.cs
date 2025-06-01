using Unity.Mathematics;
using UnityEngine;

public class SlideAbility : IAbility
{
    public string Id => "Slide";
    public bool IsActive => isSliding;
    public bool RequiresTicking => true;

    // References
    private Rigidbody rb;
    private PlayerMovement movement;
    private SlideConfig config;

    // Runtime
    private float slideStartTime = 0f;
    private float lastSlideTime = 0f;
    private bool isSliding = false;
    private Vector3 slideDirection;

    public void Initialize(GameObject owner, Object cfg = null)
    {
        if (Id == null || Id == "")
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
        rb = movement.rb;

        // We listen for crouch “release” only to cancel early if needed
        movement.input.Crouch.OnReleased += Cancel;

        if (rb == null || movement == null)
            Debug.LogError("SlideAbility needs Rigidbody + PlayerMovement on the owner.");
    }

    public void Activate() { /* not used */ }

    public void TryActivate()
    {
        // 1) Enforce cooldown
        if (Time.time < lastSlideTime) return;
        // 2) Only activate if player has some movement input (so slideDirection is non‐zero)
        if (movement.MoveInput.sqrMagnitude < 0.1f) return;

        // Start sliding
        isSliding = true;
        config.IsActive = true;
        slideStartTime = Time.time;
        config.HasBurst = false;

        // Capture the direction the player was moving in (world‐space)
        // Here I assume you have a Movement.MoveDirection that returns a Vector3
        slideDirection = movement.MoveDirection;

        // Tell PlayerMovement to reduce drag, etc.
        movement.StartSlide();
        movement.AddRestraint(this);

        Debug.Log("Slide activated!");

        float lean = movement.input.MoveInput.x;
        float tiltAmount = config.SLIDE_TILT_AMOUNT * Mathf.Max(Mathf.Abs(lean), 0.35f);
        CameraTilt.Instance.SetTilt(tiltAmount * Mathf.Sign(lean), true);
    }

    public void Tick()
    {
        // No need to do anything here for now
    }

    public void FixedTick()
    {
        if (!config.HasBurst && movement.IsGrounded)
        {
            config.HasBurst = true;

            // Grab current velocity and separate horizontal from vertical
            Vector3 fullVel = rb.linearVelocity;
            Vector3 horizontalVel = new Vector3(fullVel.x, 0f, fullVel.z);
            float currentSpeed = horizontalVel.magnitude;

            // === REDIRECT MOMENTUM ===
            Vector3 desiredDir = slideDirection.normalized;
            Vector3 redirectedVel = desiredDir * currentSpeed;

            // Preserve vertical component (gravity, etc.), just swap horizontals:
            rb.linearVelocity = new Vector3(redirectedVel.x, fullVel.y, redirectedVel.z);

            // === CONDITIONAL BOOST ===
            if (currentSpeed < config.SLIDE_SPEED_THRESHOLD)
            {
                // Scale that boost so it doesn’t push you past the threshold by too much:
                float missingSpeed = config.SLIDE_SPEED_THRESHOLD - currentSpeed;
                float burstAmount = Mathf.Min(config.SLIDE_BURST_FORCE, missingSpeed);
                rb.AddForce(desiredDir * burstAmount, ForceMode.VelocityChange);
            }

            //Debug.Log($"Slide landed. Redirected speed={currentSpeed:F2}. Applied burst={ (currentSpeed < config.SLIDE_SPEED_THRESHOLD ? burstAmount : 0f):F2 }");
        }

        //AFTER “FULL SPEED DURATION”, start slowing down slide
        if (slideStartTime + config.FULL_SPEED_DURATION < Time.time)
            rb.AddForce(slideDirection * -config.SLIDE_REDUCE_FORCE, ForceMode.Acceleration);

        // STOP CONDITIONS
        bool shouldStop = false;
        Vector3 vel2 = movement.GetVelocity();
        if (vel2.sqrMagnitude < config.SLIDE_MINS_SPEED) // almost stopped on ground
            shouldStop = true;
        if (!movement.IsGrounded && config.HasBurst)    // left ground again after burst
            shouldStop = true;

        if (shouldStop)
            Cancel();
    }

    public void Cancel()
    {
        if (movement.sm.CurrentSub != PlayerStateMachine.SubState.Jumping)
            lastSlideTime = Time.time + config.SLIDE_COOLDOWN;
        else
        {
            lastSlideTime = Time.time + config.SLIDE_COOLDOWN_JUMPING;
            Debug.Log("Slide cancelled while jumping, applying shorter cooldown.");
        }

        config.HasBurst = false;
        slideStartTime = 0f;
        isSliding = false;
        config.IsActive = false;

        movement.RemoveRestraint(this);
        movement.StopSlide();

        CameraTilt.Instance.ResetTilt();
    }
}
