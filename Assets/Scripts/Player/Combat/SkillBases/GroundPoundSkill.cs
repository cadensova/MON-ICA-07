using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "GroundPoundSkill", menuName = "PlayerSkills/GroundPound")]
public class GroundPoundSkill : PlayerSkill
{
    [Header("GP Settings")]
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float slamSpeed = 15f;
    [SerializeField] private float bounceForce = 50f;
    [SerializeField] private JumpConfig config;
    [SerializeField] private LayerMask groundLayer;

    private bool subbedToPressed = false;
    private bool didEarlyRelease = false;
    private bool wantsToJump = false;
    private bool bouncing = false;

    public override void OnExecute()
    {
        if (col == null || IsActive) return;

        // Raycast down from bottom of collider, not its center
        Vector3 rayOrigin = userGO.transform.position + Vector3.up * (col.bounds.extents.y);
        if (!Physics.Raycast(rayOrigin, FlowPhysics.Instance.GravityDirection, out RaycastHit hit, 250f, groundLayer))
        {
            // No ground found (too far or invalid), so bail
            return;
        }

        float distToGround = Vector3.Distance(rayOrigin, hit.point);
        if (distToGround < minHeight)
            return; // not high enough

        StartGroundPound();
    }

    private void StartGroundPound()
    {
        IsActive = true;
        movement.AddRestraint(this);

        // Zero vertical velocity, then slam down
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(FlowPhysics.Instance.GravityDirection * slamSpeed, ForceMode.VelocityChange);

        // Subscribe to Jump Press so we can track a bounce request
        if (!subbedToPressed)
        {
            InputProvider.Instance.Jump.OnPressed += WantsToJump;
            subbedToPressed = true;
            wantsToJump = false; 
        }

        movement.StartCoroutine(GroundPoundRoutine());
    }

    private IEnumerator GroundPoundRoutine()
    {
        // 1) Wait until we hit ground
        yield return new WaitUntil(() => movement.IsGrounded);

        // 2) Unsubscribe the "pressed" listener
        InputProvider.Instance.Jump.OnPressed -= WantsToJump;
        subbedToPressed = false;

        // 3) Reset the initial slam restraint (we’re on the ground)
        ResetPound(full: false);

        // 4) If player tapped jump exactly at landing, do a bounce
        if (wantsToJump)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(-FlowPhysics.Instance.GravityDirection * bounceForce, ForceMode.Impulse);
            wantsToJump = false;
            bouncing = true;

            // Subscribe to OnReleased so we can do fast-fall if they let go too quickly
            InputProvider.Instance.Jump.OnReleased += CancelEarly;
        }

        // 5) If they did an early release mid-bounce, apply a fast-fall
        if (didEarlyRelease)
        {
            ApplyFastFallModifier();
        }

        // 6) Wait until the bounce is completely done
        yield return new WaitUntil(() => !bouncing);

        // 7) Final cleanup: remove bounce listener and fully reset
        InputProvider.Instance.Jump.OnReleased -= CancelEarly;
        ResetPound(full: true);
    }

    private void WantsToJump()
    {
        wantsToJump = true;
    }

    private void CancelEarly()
    {
        if (!bouncing) return;

        if (rb.linearVelocity.y > 0f)
        {
            // cut that upward velocity for a “low jump” effect
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
        // only apply if still airborne and currently falling
        if (!movement.IsGrounded
            && movement.sm.CurrentMain == PlayerStateMachine.MainState.Airborne
            && rb.linearVelocity.y < 0f)
        {
            float extraG = (config.FALL_MULTI - 1f) * FlowPhysics.Instance.GravityStrength;
            rb.AddForce(Vector3.down * extraG, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Resets IsActive and removes restraint. If 'full' is true, also clear bounce flags and early-release flag.
    /// </summary>
    private void ResetPound(bool full = false)
    {
        IsActive = false;
        movement.RemoveRestraint(this);

        if (full)
        {
            didEarlyRelease = false;
            bouncing = false;
            wantsToJump = false;
        }
    }
}
