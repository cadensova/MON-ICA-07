using UnityEngine;

public class JumpAbility : IAbility
{
    public string Id => "Jump";
    public bool IsActive => false;
    public bool RequiresTicking => false;

    private Rigidbody rb;
    public JumpConfig config;
    private float lastJumpTime = 0f;

    public void Initialize(GameObject owner, Object config = null)
    {
        if (config is not JumpConfig jumpConfig)
        {
            Debug.LogError("Invalid or missing JumpConfig for JumpAbility.");
            return;
        }

        if (owner == null)
        {
            Debug.LogError("Owner GameObject is null. Cannot initialize JumpAbility.");
            return;
        }

        this.config = jumpConfig;
        rb = owner.GetComponent<Rigidbody>();
    }

    public void Activate() { }

    public void TryActivate()
    {
        if (lastJumpTime > Time.time) return;

        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on JumpAbility. Please ensure the owner has a Rigidbody component.");
            return;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.down * -config.JUMP_FORCE, ForceMode.VelocityChange);

        Debug.Log("Jump activated!");
        lastJumpTime = Time.time + config.JUMP_COOLDOWN;
    }

    public void Tick() { }
    public void FixedTick() { }
    public void Cancel() { }
}