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

        if (rb == null || movement == null)
            Debug.LogError("JumpAbility needs Rigidbody + PlayerMovement on the owner.");
    }

    public void Activate() { }

    public void TryActivate()
    {
        if (movement.sm.CurrentSub == PlayerStateMachine.SubState.Dashing || movement.sm.CurrentSub == PlayerStateMachine.SubState.GroundPounding)
                return;

        if (lastJumpTime > Time.time) return;

        isJumping = true;
        hasLeftGround = false;
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
    }
}