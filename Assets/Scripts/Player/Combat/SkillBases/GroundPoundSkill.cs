using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "GroundPoundSkill", menuName = "PlayerSkills/GroundPound")]
public class GroundPoundSkill : PlayerSkill
{
    [Header("GP Settings")]
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float slamSpeed = 15f;
    [SerializeField] private float bounceUpwardsForce = 50f;
    [SerializeField] private float bounceMovementForce = 50f;
    [SerializeField] private JumpConfig config;
    [SerializeField] private LayerMask groundLayer;

    // You might optionally cap how many times minHeight is allowed:
    [Header("Optional Bounce Cap")]
    [Tooltip("If you fall more than (maxBounceMultiplier × minHeight), your bounce will be clamped.")]
    [SerializeField] private float maxBounceMultiplier = 3f;

    private bool subbedToPressed = false;
    private bool wantsToJump = false;
    private bool bouncing = false;

    // This holds the distance from your feet to ground at the moment you start the slam.
    private float disToGround;

    public override void OnExecute()
    {
        if (col == null || IsActive) 
            return;

        // Raycast down from bottom of collider, not its center:
        Vector3 rayOrigin = userGO.transform.position + Vector3.up * (col.bounds.extents.y);
        if (!Physics.Raycast(rayOrigin, FlowPhysics.Instance.GravityDirection, out RaycastHit hit, 250f, groundLayer))
        {
            // No valid ground within 250m
            return;
        }

        // Record how far your feet were from the ground when you initiated the ground pound
        disToGround = Vector3.Distance(rayOrigin, hit.point);
        if (disToGround < minHeight)
            return; // not high enough to ground pound

        StartGroundPound();
    }

    private void StartGroundPound()
    {
        IsActive = true;
        movement.AddRestraint(this);

        // Zero vertical velocity, then slam down immediately:
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(FlowPhysics.Instance.GravityDirection * slamSpeed, ForceMode.VelocityChange);

        // Subscribe to Jump.OnPressed so we can trigger a bounce if they tap at impact:
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
        // 1) Wait for the moment we actually hit the ground
        yield return new WaitUntil(() => movement.IsGrounded);

        // 2) Unsubscribe the "pressed" listener so we don't handle extra presses later
        InputProvider.Instance.Jump.OnPressed -= WantsToJump;
        subbedToPressed = false;

        // 3) We have landed: remove the slam restraint (we’re on the ground now).
        //    (Do NOT clear disToGround yet, because we need it to compute bounce.)
        ResetPound(full: false);

        // 4) If the player tapped Jump exactly at the landing frame, do a bounce:
        if (wantsToJump)
        {
            rb.linearVelocity = Vector3.zero;

            Vector3 bounceDir = movement.MoveDirection.normalized * bounceMovementForce + FlowPhysics.Instance.GravityDirection.normalized * -bounceUpwardsForce;
            rb.AddForce(bounceDir, ForceMode.Impulse);

            wantsToJump = false;
            bouncing = true;

            movement.SetMaxSpeed((movement.MoveDirection.normalized.magnitude * bounceMovementForce) + movement.HorizontalVelocity.magnitude);
        }

        // 6) Wait until the bounce is fully over (bouncing is set to false by your controller when they land again)
        yield return new WaitUntil(() => !bouncing);
        ResetPound(full: true);
    }

    private void WantsToJump()
    {
        wantsToJump = true;
    }

    /// <summary>
    /// Resets IsActive and removes restraint. If 'full' is true, also clears bounce flags, didEarlyRelease, wantsToJump, and disToGround.
    /// </summary>
    private void ResetPound(bool full = false)
    {
        IsActive = false;
        movement.RemoveRestraint(this);

        if (full)
        {
            bouncing = false;
            wantsToJump = false;
            disToGround = 0f;
        }
    }

    /// <summary>
    /// Returns a bounce‐force based on how far the player fell (disToGround).
    /// Right now, we simply do: base bounceForce × (disToGround / minHeight), clamped by maxBounceMultiplier.
    /// </summary>
    private float GetBounceForceForDistance(float fallDistance)
    {
        // Prevent division by zero, just in case:
        if (minHeight <= 0f) 
            return bounceUpwardsForce;

        // Scale factor: 1 when fallDistance == minHeight, >1 when you fell farther
        float rawMultiplier = fallDistance / minHeight;

        // Optionally clamp so it never bounces more than X × base:
        float clampedMultiplier = Mathf.Clamp(rawMultiplier, 1f, maxBounceMultiplier);

        return bounceUpwardsForce * clampedMultiplier;
    }
}
