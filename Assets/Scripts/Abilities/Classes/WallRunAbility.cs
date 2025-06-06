using UnityEngine;

public class WallRunAbility : IAbility
{
    public string Id { get; private set; } = "WallRun";
    public bool IsActive => isWallRunning;
    public bool RequiresTicking { get; private set; } = true;

    private Rigidbody rb;
    private PlayerMovement movement;
    private WallRunConfig config;

    private bool isWallRunning = false;
    private float timeLeftWall = 0f;
    private float timeSinceLastWallRun;

    // Called once at startup:
    public void Initialize(GameObject owner, Object cfg = null)
    {
        if (cfg is not WallRunConfig wallCfg)
        {
            Debug.LogError("WallRunAbility: need WallRunConfig!");
            return;
        }
        config = wallCfg;

        movement = owner.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError($"WallRunAbility: {owner.name} is missing PlayerMovement!");
            return;
        }

        rb = movement.rb;
        if (rb == null)
        {
            Debug.LogError($"WallRunAbility: {owner.name}'s PlayerMovement is missing Rigidbody!");
            return;
        }

        // So that first activation isn't blocked:
        timeSinceLastWallRun = Time.time - config.WALL_RUN_COOLDOWN;
    }

    // Not used by this script but is needed to fit IAbility
    public void Activate() { }

    // Called by input handler when player presses “wall run” button:
    public void TryActivate()
    {
        // 1) Check cooldown:
        if (Time.time < timeSinceLastWallRun + config.WALL_RUN_COOLDOWN)
            return;

        // 2) Only allow if not on ground/sliding/dashing:
        if (movement.sm.CurrentMain == PlayerStateMachine.MainState.Grounded ||
            movement.sm.CurrentSub == PlayerStateMachine.SubState.Sliding ||
            movement.sm.CurrentSub == PlayerStateMachine.SubState.Dashing)
            return;

        // 3) Must be touching a valid side wall AND pushing into it:
        if (!IsFacingWall())
            return;

        // 4) Activate wall-run:
        isWallRunning = true;
        config.IsActive = true;
        timeLeftWall = 0f;              // reset any previous grace
        rb.useGravity = false;

        // Stop vertical momentum:
        Vector3 v = rb.linearVelocity;
        rb.AddForce(movement.MoveDirection * config.Wall_BOOST);
        rb.linearVelocity = new Vector3(v.x, 0f, v.z);

        movement.AddRestraint(this);

        // Tilt camera toward the wall:
        float leanDir = (movement.wallDirection == WallDirection.Right) ? +1f : -1f;
        CameraTilt.Instance.SetTilt(config.TILT_AMOUNT * leanDir, true);
    }

    // Called every frame:
    public void Tick()
    {
        // If we’re no longer on a valid face of a side wall, start counting grace:
        if (!IsFacingWall())
            timeLeftWall += Time.deltaTime;
        else
            timeLeftWall = 0f;

        // If we exceed the grace window, cancel:
        if (timeLeftWall > config.WALL_RUN_GRACE)
        {
            //Debug.LogError("Left the wall!");
            Cancel();
        }
    }

    // Called every FixedUpdate:
    public void FixedTick()
    {
        // 1) Apply stickiness force (press them into the wall)
        Vector3 dirToWall = -movement.wallHit.normal;
        rb.AddForce(dirToWall * config.WALL_STICKINESS, ForceMode.Force);

        // 2) If stick is okay and push-up input is held, keep y=0. Otherwise, slip down:
        if (movement.MoveInput.y > 0.25f)
        {
            // Zero vertical so they remain perfectly horizontal:
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, 0f, v.z);
        }
        else
        {
            // Apply a downward force so they slide:
            Vector3 downForce = FlowPhysics.Instance.GravityDirection * config.WALL_SLIP_SPEED;
            rb.AddForce(downForce, ForceMode.Force);
        }
    }

    // Called to forcibly end the wall run:
    public void Cancel()
    {
        if (!isWallRunning) 
            return;

        isWallRunning = false;
        config.IsActive = false;

        // Re-enable gravity so they’ll fall
        rb.useGravity = true;

        // Record when this ended, for cooldown logic:
        timeSinceLastWallRun = Time.time;

        movement.RemoveRestraint(this);

        CameraTilt.Instance.ResetTilt();
    }

    // “Am I on a side wall (Left or Right) AND pushing into it at ≤ MAX_WALL_ANGLE?”
    private bool IsFacingWall()
    {
        if (movement.wallDirection != WallDirection.Left &&
            movement.wallDirection != WallDirection.Right)
        {
            return false;
        }

        // Compute angle between player input dir and wall normal
        Vector3 dirToWall = -movement.wallHit.normal;
        float currentAngle = Vector3.Angle(movement.MoveDirection, dirToWall);
        return (currentAngle < config.MAX_WALL_ANGLE);
    }
}
