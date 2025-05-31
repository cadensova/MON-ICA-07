using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public Vector3 MoveDirection => ori.forward * MoveInput.y + ori.right * MoveInput.x;
    [field: SerializeField] public List<object> Restraints = new List<object>();
    public bool Restricted => Restraints.Count > 0;
    [SerializeField] private bool _Restricted = false;


    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float airControl = 0.5f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float baseMoveMulti = 5f;


    [Header("Modified Movement Settings")]
    
    [SerializeField] private float walkMoveMulti = 1f;
    [SerializeField] private float crouchMoveMulti = 0.5f;
    [field: SerializeField] public bool IsWalking { get; private set; } = false;
    [field: SerializeField] public bool IsCrouching { get; private set; } = false;


    [Header("Jump Settings")]
    [SerializeField] private float jumpBufferGrace = 0.15f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    private float bufferTimeLeft = 0f;
    private float lastTimeOnGround = 0f;
    private bool didEarlyRelease = false;


    [Header("Slide Settings")]
    [SerializeField] private float slideDragOverride;
    [field: SerializeField] public bool IsSliding { get; private set; }
    


    [Header("Drag Settings")]
    [SerializeField] private float groundDrag = 0.1f;
    [SerializeField] private float airDrag = 0.1f;
    [SerializeField] private float dragAccel = 2f;
    [SerializeField] private float dragDecel = 2f;
    [SerializeField] private float targetDrag = 0f;


    [Header("Collision Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [field: SerializeField] public bool IsGrounded { get; private set; } = false;
    [field: SerializeField] public bool IsOnSlope { get; private set; } = false;
    [field: SerializeField] public RaycastHit groundHit { get; private set; } = new RaycastHit();

    [Header("Physics Settings")]
    [SerializeField] private float MAX_SPEED = 75f;
    [SerializeField] private float maxAirSpeed;


    // COMPONENTS
    public CapsuleCollider col { get; private set; }
    public Rigidbody rb { get; private set; }
    public InputProvider input { get; private set; }
    public Transform ori { get; private set; }
    public PlayerStateMachine sm { get; private set; }
    public AbilityLoader abilities { get; private set; }
    public Vector3 HorizontalVelocity => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

    #region  Subscription Setup
    private Coroutine subscribeCoroutine;

    private void OnEnable()
    {
        subscribeCoroutine = StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (subscribeCoroutine != null)
            StopCoroutine(subscribeCoroutine);

        Unsubscribe();
    }

    public bool subscribed { get; private set; } = false;
    private IEnumerator SubscribeWhenReady()
    {
        // Wait for dependencies
        while (InputProvider.Instance == null || FlowPhysics.Instance == null)
        {
            yield return new WaitForSeconds(0.05f);
        }

        input = InputProvider.Instance;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        ori = transform.Find("Orientation");
        sm = GetComponent<PlayerStateMachine>();
        abilities = GetComponentInChildren<AbilityLoader>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on PlayerMovement. Please ensure the player has a Rigidbody component.");
            yield break;
        }

        if (col == null)
        {
            Debug.LogError("CapsuleCollider not found on PlayerMovement. Please ensure the player has a CapsuleCollider component.");
            yield break;
        }

        if (sm == null)
        {
            Debug.LogError("PlayerStateMachine not found on PlayerMovement. Please ensure the player has a PlayerStateMachine component.");
            yield break;
        }

        if (abilities == null)
        {
            Debug.LogError("AbilityLoader not found on PlayerMovement. Please ensure the player has an AbilityLoader component.");
            yield break;
        }

        rb.useGravity = false;
        FlowPhysics.Instance.Register(rb);

        AssignInputs();

        Debug.Log("PlayerMovement subscribed to input.");
        subscribed = true;
    }


    private void AssignInputs()
    {
        input.Jump.OnPressed += () => bufferTimeLeft = Time.time + jumpBufferGrace;

        input.Jump.OnReleased += OnJumpReleased;

        input.Crouch.Started += () => abilities.GetAbility<SlideAbility>()?.TryActivate();
        input.Crouch.OnReleased += () => IsCrouching = false;

        input.ToggleWalk.OnPressed += () => IsWalking = !IsWalking;
    }

    private void Unsubscribe()
    {
        //Could be used to unsubscribe from input events if needed
        FlowPhysics.Instance.Unregister(rb);

        if (input != null)
            input.Jump.OnReleased -= OnJumpReleased;
        subscribed = false;
    }

    #endregion

    private void Update()
    {
        if (IsWalking) HandleToggleWalk();

        _Restricted = Restricted;
        if (!subscribed)
            return;

        if (input != null)
            MoveInput = input.MoveInput;

        if (IsGrounded)
            lastTimeOnGround = Time.time + coyoteTime;

        if (bufferTimeLeft > Time.time && lastTimeOnGround > Time.time)
        {
            bufferTimeLeft = 0f;
            abilities.GetAbility<JumpAbility>()?.TryActivate();
        }
    }

    private void FixedUpdate()
    {
        if (!subscribed)
            return;

        RaycastHit hit;
        IsGrounded = Physics.SphereCast(
            transform.position,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer
        );
        groundHit = hit;
        rb.useGravity = !IsGrounded;

        DragControl();

        targetDrag = IsSliding
            ? slideDragOverride
            : (IsGrounded ? groundDrag : airDrag);

        DetermineMaxSpeed();
        if (!Restricted)
            ApplyMovement();
        

        ApplyFastFallModifier();
    }

    private void HandleToggleWalk()
    {
        if (!IsWalking) return;

        if (IsCrouching || IsSliding || !IsGrounded)
            IsWalking = false;
    }

    private void DetermineMaxSpeed()
    {
        if (IsGrounded)
            maxAirSpeed = HorizontalVelocity.magnitude;
    }

    private void ApplyMovement()
    {
        Vector3 moveForce = Vector3.zero;

        if (sm.CurrentMain == PlayerStateMachine.MainState.Grounded || 
            sm.CurrentMain == PlayerStateMachine.MainState.Crouching)
        {
            float groundMulti = baseMoveMulti;
            if (IsWalking)
                groundMulti = baseMoveMulti * walkMoveMulti;
            if (IsCrouching)
                groundMulti = baseMoveMulti * crouchMoveMulti;

            // Regular grounded movement
            moveForce = AdjustVelocityToSlope(MoveDirection.normalized * moveSpeed * groundMulti);

            if (MoveInput == Vector2.zero)
            {
                rb.AddForce(-AdjustVelocityToSlope(HorizontalVelocity), ForceMode.VelocityChange);
                return;
            }
        }
        else if (sm.CurrentMain == PlayerStateMachine.MainState.Airborne)
        {
            // Apply air control force
            moveForce = MoveDirection.normalized * moveSpeed * baseMoveMulti * airControl;

            // Clamp horizontal air speed to current max
            if (HorizontalVelocity.magnitude > maxAirSpeed)
            {
                Vector3 clamped = HorizontalVelocity.normalized * maxAirSpeed;
                rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
            }
        }

        rb.AddForce(moveForce, ForceMode.Acceleration);

        rb.linearVelocity = new Vector3(
            Mathf.Clamp(rb.linearVelocity.x, -MAX_SPEED, MAX_SPEED),
            rb.linearVelocity.y,
            Mathf.Clamp(rb.linearVelocity.z, -MAX_SPEED, MAX_SPEED)
        );
    }


    private Vector3 AdjustVelocityToSlope(Vector3 velocity) {
        IsOnSlope = false;
        if (groundHit.transform == null) return velocity;

        float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
        if (slopeAngle < 0.01f || slopeAngle > 75f) return velocity;

        IsOnSlope = true;
        return Vector3.ProjectOnPlane(velocity, groundHit.normal).normalized * velocity.magnitude;
    }

    private float smoothDrag = 0f;
    private void DragControl() {
        if (targetDrag == 0f)
        {
            rb.linearDamping = 0f;
            return;
        }

        if (targetDrag > smoothDrag)
            smoothDrag = Mathf.MoveTowards(smoothDrag, targetDrag, dragAccel * Time.deltaTime);
        else
            smoothDrag = Mathf.MoveTowards(smoothDrag, targetDrag, dragDecel * Time.deltaTime);

        rb.linearDamping = smoothDrag;
    }

    private void OnJumpReleased()
    {
        // only if we're still moving up
        if (rb.linearVelocity.y > 0f)
        {
            // cut that upward velocity for a low jump
            rb.linearVelocity = new Vector3(rb.linearVelocity.x,
                                    rb.linearVelocity.y / lowJumpMultiplier,
                                    rb.linearVelocity.z);
            didEarlyRelease = true;
        }
    }

    private void ApplyFastFallModifier()
    {
        // when in air, already falling, and we did an early release
        if (!IsGrounded
            && sm.CurrentMain == PlayerStateMachine.MainState.Airborne
            && rb.linearVelocity.y < 0f
            && didEarlyRelease)
        {
            // extra downward force = (fallMultiplier – 1) × normal gravity
            float extraG = (fallMultiplier - 1f) * FlowPhysics.Instance.GravityStrength;
            rb.AddForce(Vector3.down * extraG, ForceMode.Acceleration);
        }
        // reset when back on ground
        else if (IsGrounded && didEarlyRelease)
        {
            didEarlyRelease = false;
        }
    }

    public void StartSlide()
    {
        if (IsSliding) return;

        IsSliding = true;
    }

    public void StopSlide()
    {
        if (!IsSliding) return;

        IsSliding = false;

        if (input.Crouch.IsPressed)
            IsCrouching = true;
        else
            IsCrouching = false;
    }


    public Vector3 GetVelocity() => subscribed == true ? rb.linearVelocity : Vector3.zero;
    
    public void AddRestraint(object restraint)
    {
        if (!Restraints.Contains(restraint))
            Restraints.Add(restraint);
    }
    public void RemoveRestraint(object restraint)
    {
        if (Restraints.Contains(restraint))
            Restraints.Remove(restraint);
    }

    // Draw ground check gizmo in the editor
    private void OnDrawGizmosSelected()
    {
        // Origin at the player's position
        Vector3 origin = transform.position;

        // Draw a line down to the check distance
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);

        // Draw the sphere at the end of the check
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius);
    }
}
