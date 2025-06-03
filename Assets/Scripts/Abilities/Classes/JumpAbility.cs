using UnityEngine;

public class JumpAbility : IAbility
{
    public string Id => "Jump";
    public bool IsActive => isJumping;
    public bool RequiresTicking => true;

    private Rigidbody rb;
    private PlayerMovement movement;
    public JumpConfig config;
    private float lastJumpTime = 0f;

    private bool isJumping;
    private bool didEarlyRelease;
    private bool hasLeftGround;

    public void Initialize(GameObject owner, Object cfg = null)
    {
        if (Id == null || Id == "")
        {
            Debug.LogError("JumpAbility ID is not set.");
            return;
        }

        if (cfg is not JumpConfig jumpCfg)
        {
            Debug.LogError("Missing or invalid JumpConfig for JumpAbility.");
            return;
        }
        config = jumpCfg;
        movement = owner.GetComponent<PlayerMovement>();
        rb = movement.rb;

        // We listen for crouch “release” only to cancel early if needed
        movement.input.Jump.OnReleased += OnJumpReleased;

        if (rb == null || movement == null)
            Debug.LogError("JumpAbility needs Rigidbody + PlayerMovement on the owner.");
    }

    public void Activate() { }

    public void TryActivate()
    {
        if (lastJumpTime > Time.time) return;

        isJumping = true;
        hasLeftGround = false;
        didEarlyRelease = false;
        config.IsActive = true;
        lastJumpTime = 0f;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.down * -config.JUMP_FORCE, ForceMode.VelocityChange);

        //Debug.Log("Jump activated!");
        lastJumpTime = Time.time + config.JUMP_COOLDOWN;
    }

    public void Tick() { }
    public void FixedTick()
    {
        ApplyFastFallModifier();

        if (!hasLeftGround)
        {
            if (!movement.IsGrounded)
                hasLeftGround = true;

            return;
        }

        if (rb.linearVelocity.y <= movement.sm.fallThreshold || movement.IsGrounded)
            Cancel();
    }

    public void Cancel()
    {
        isJumping = false;
        config.IsActive = false;

        if (didEarlyRelease)
        {
            // Make Player Reset fall multi when they land i guess
        }
    }


    private void OnJumpReleased()
    {
        // only if we're still moving up
        if (rb.linearVelocity.y > 0f)
        {
            // cut that upward velocity for a low jump
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                rb.linearVelocity.y / config.LOW_JUMP_MULTI,
                rb.linearVelocity.z
            );
            didEarlyRelease = true;
        }
    }
    
    private void ApplyFastFallModifier()
    {
        // when in air, already falling, and we did an early release
        if (!movement.IsGrounded
            && movement.sm.CurrentMain == PlayerStateMachine.MainState.Airborne
            && rb.linearVelocity.y < 0f
            && didEarlyRelease)
        {
            // extra downward force = (fallMultiplier – 1) × normal gravity
            float extraG = (config.FALL_MULTI - 1f) * FlowPhysics.Instance.GravityStrength;
            rb.AddForce(Vector3.down * extraG, ForceMode.Acceleration);
        }
        // reset when back on ground
        else if (movement.IsGrounded && didEarlyRelease)
        {
            didEarlyRelease = false;
        }
    }
}