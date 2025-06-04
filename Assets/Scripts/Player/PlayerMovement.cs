using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum WallDirection
{
    None,
    Left,
    Right,
    Forward,
    Backward
}

public class PlayerMovement : MonoBehaviour
{
    private class WallHitObject
    {
        public RaycastHit hit;
        public Vector3 direction;
        public float distance;

        public WallHitObject(RaycastHit hit, WallDirection direction)
        {
            this.hit = hit;

            switch (direction)
            {
                case WallDirection.Left:
                    this.direction = Vector3.left;
                    break;
                case WallDirection.Right:
                    this.direction = Vector3.right;
                    break;
                case WallDirection.Forward:
                    this.direction = Vector3.forward;
                    break;
                case WallDirection.Backward:
                    this.direction = Vector3.back;
                    break;
            }

            this.distance = hit.distance;
        }

        public WallDirection GetDirection()
        {
            if (direction == Vector3.left) return WallDirection.Left;
            if (direction == Vector3.right) return WallDirection.Right;
            if (direction == Vector3.forward) return WallDirection.Forward;
            if (direction == Vector3.back) return WallDirection.Backward;
            return WallDirection.None;
        }
    }


    public Vector2 MoveInput { get; private set; }
    public Vector3 MoveDirection => ori.forward * MoveInput.y + ori.right * MoveInput.x;
    [field: SerializeField] public List<object> Restraints = new List<object>();
    public bool Restricted => Restraints.Count > 0;
    [SerializeField] private bool _Restricted = false;


    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float airControl = 0.5f;
    [SerializeField] private float baseMoveMulti = 5f;


    [Header("Modified Movement Settings")]
    [SerializeField] private float walkMoveMulti = 1f;
    [SerializeField] private float crouchMoveMulti = 0.5f;
    [field: SerializeField] public bool IsWalking { get; private set; } = false;
    [field: SerializeField] public bool IsCrouching { get; private set; } = false;


    [Header("Jump Settings")]
    [SerializeField] private float jumpBufferGrace = 0.15f;
    [SerializeField] private float coyoteTime = 0.15f;
    private float bufferTimeLeft = 0f;
    private float lastTimeOnGround = 0f;


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


    [Header("Wall Collision Settings")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float wallCheckRadius = 0.5f;
    [SerializeField] private float wallCheckOffset = 0.1f;
    [field: SerializeField] public bool IsWallRunning { get; private set; } = false;
    [field: SerializeField] public bool IsTouchingWall { get; private set; } = false;
    [field: SerializeField] public WallDirection wallDirection { get; private set; } = WallDirection.None;
    private List<WallHitObject> wallHits = new List<WallHitObject>(4);


    public RaycastHit groundHit { get; private set; } = new RaycastHit();
    public RaycastHit wallHit { get; private set; } = new RaycastHit();


    [Header("Physics Settings")]
    [SerializeField] private float MAX_SPEED = 75f;
    [SerializeField] private float maxAirSpeed;
    [SerializeField] private float minAirSpeed = 15f;

    // COMPONENTS
    public CapsuleCollider col { get; private set; }
    public Rigidbody rb { get; private set; }
    public InputProvider input { get; private set; }
    public Transform ori { get; private set; }
    public PlayerStateMachine sm { get; private set; }
    public AbilityLoader abilities { get; private set; }
    public Vector3 HorizontalVelocity => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);



    #region Subscription Setup
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

    public bool Verified { get; private set; } = false;
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
        FlowPhysics.Instance.Register(rb);

        AssignInputs();

        Debug.Log("PlayerMovement subscribed to input.");
        Verified = true;

        StopCoroutine(SubscribeWhenReady());
    }

    private void AssignInputs()
    {
        input.Jump.OnPressed += () => bufferTimeLeft = Time.time + jumpBufferGrace;

        input.Crouch.Started += () => abilities.GetAbility<SlideAbility>()?.TryActivate();
        input.Crouch.OnReleased += () => IsCrouching = false;

        input.ToggleWalk.OnPressed += () => IsWalking = !IsWalking;
    }

    private void Unsubscribe()
    {
        FlowPhysics.Instance.Unregister(rb);

        Verified = false;
    }
    #endregion

    private void Update()
    {
        if (IsWalking) HandleToggleWalk();

        _Restricted = Restricted;
        if (!Verified)
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
        if (!Verified)
            return;

        DetermineCollisions();

        if (!IsGrounded && IsTouchingWall)
            abilities.GetAbility<WallRunAbility>()?.TryActivate();


        DragControl();

        targetDrag = IsSliding
            ? slideDragOverride
            : (IsGrounded ? groundDrag : airDrag);

        DetermineMaxSpeed();
        if (!Restricted)
            ApplyMovement();
    }


    private void DetermineCollisions()
    {
        // ── Ground check (unchanged) ─────────────────────────────────────────────
        RaycastHit tempHitGround;
        IsGrounded = Physics.SphereCast(
            transform.position + col.center,
            groundCheckRadius,
            Vector3.down,
            out tempHitGround,
            col.height/2 + groundCheckDistance,
            groundLayer
        );
        groundHit = tempHitGround;
        rb.useGravity = !IsGrounded;

        // ── Wall checks ────────────────────────────────────────────────────────────
        wallHits.Clear();
        IsTouchingWall = false;

        // For each of the four WallDirection values, do one SphereCast:
        foreach (WallDirection dir in System.Enum.GetValues(typeof(WallDirection)))
        {
            GetWallCastParams(dir, out Vector3 origin, out Vector3 castDir);

            if (Physics.SphereCast(
                origin,
                wallCheckRadius,
                castDir,
                out RaycastHit hitInfo,
                wallCheckDistance,
                wallLayer))
            {
                wallHits.Add(new WallHitObject(hitInfo, dir));
                IsTouchingWall = true;
            }
        }

        // If we hit at least one wall, pick the closest:
        if (IsTouchingWall)
        {
            WallHitObject closest = wallHits[0];
            foreach (var wh in wallHits)
            {
                if (wh.distance < closest.distance)
                    closest = wh;
            }

            wallHit = closest.hit;
            wallDirection = closest.GetDirection();
        }
        else
        {
            wallHit = new RaycastHit();  
            wallDirection = WallDirection.None;
        }
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
        maxAirSpeed = Mathf.Clamp(maxAirSpeed, minAirSpeed, MAX_SPEED);
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

    private Vector3 AdjustVelocityToSlope(Vector3 velocity)
    {
        IsOnSlope = false;
        if (groundHit.transform == null) return velocity;

        float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
        if (slopeAngle < 0.01f || slopeAngle > 75f) return velocity;

        IsOnSlope = true;
        return Vector3.ProjectOnPlane(velocity, groundHit.normal).normalized * velocity.magnitude;
    }

    private float smoothDrag = 0f;
    private void DragControl()
    {
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


    public Vector3 GetVelocity() => Verified ? rb.linearVelocity : Vector3.zero;
    private void GetWallCastParams(WallDirection dir, out Vector3 castOrigin, out Vector3 castDirection)
    {
        // Base “center” point (player’s position)
            Vector3 center = transform.position + col.center;

        switch (dir)
        {
            case WallDirection.Left:
                // We want to cast LEFT (–ori.right),
                // so we start ORIGIN slightly to the RIGHT of center:
                castDirection = -ori.right;
                castOrigin = center + (ori.right * wallCheckOffset);
                break;

            case WallDirection.Right:
                // We want to cast RIGHT (+ori.right),
                // so start ORIGIN slightly to the LEFT of center:
                castDirection = ori.right;
                castOrigin = center + (-ori.right * wallCheckOffset);
                break;

            case WallDirection.Forward:
                // Cast FORWARD (+ori.forward),
                // so start slightly BEHIND (–ori.forward):
                castDirection = ori.forward;
                castOrigin = center + (-ori.forward * wallCheckOffset);
                break;

            case WallDirection.Backward:
                // Cast BACKWARD (–ori.forward),
                // so start slightly AHEAD (+ori.forward):
                castDirection = -ori.forward;
                castOrigin = center + (ori.forward * wallCheckOffset);
                break;

            default:
                // (If you add more enum values in the future—just default to “no cast.”)
                castDirection = Vector3.zero;
                castOrigin = center;
                break;
        }
    }

    // Draw ground check gizmo in the editor
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        if (!Verified)
            return;

        // Draw the ground‐check gizmo (unchanged)
        Vector3 groundOrigin = transform.position + col.center;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * (col.height/2 + groundCheckDistance));
        Gizmos.DrawWireSphere(groundOrigin + Vector3.down * (col.height/2 + groundCheckDistance), groundCheckRadius);

        // Draw each wall‐cast start point + direction
        foreach (WallDirection dir in System.Enum.GetValues(typeof(WallDirection)))
        {
            GetWallCastParams(dir, out Vector3 origin, out Vector3 castDir);

            // Draw the “sphere” start position:
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, wallCheckRadius);

            // Draw the cast‐direction as a line
            Gizmos.color = IsTouchingWall && wallDirection == dir
                        ? Color.red    // if we’re actually touching *this* wall right now, color it red
                        : Color.cyan;  // otherwise, just cyan
            Gizmos.DrawLine(origin, origin + (castDir.normalized * wallCheckDistance));
        }
    }
}
