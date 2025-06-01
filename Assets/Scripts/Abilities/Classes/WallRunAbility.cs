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

    public void Initialize(GameObject owner, Object cfg = null)
    {
        if (Id == null || Id == "")
        {
            Debug.LogError("WallRunAbility ID is not set.");
            return;
        }

        if (cfg is not WallRunConfig wallCfg)
        {
            Debug.LogError("Missing or invalid WallRunConfig for WallRunAbility.");
            return;
        }
        config = wallCfg;
        movement = owner.GetComponent<PlayerMovement>();
        rb = movement.rb;

        if (rb == null || movement == null)
            Debug.LogError("WallRunAbility needs Rigidbody + PlayerMovement on the owner.");
    }

    public void Activate() { }

    public void TryActivate()
    {
        // Try activation logic here

        if (movement.IsGrounded || movement.IsCrouching || movement.IsSliding)
            return;

        if (IsNotSideWall())
            return;

        isWallRunning = true;
        config.IsActive = true;
        rb.useGravity = false;

        movement.AddRestraint(this);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    public void Tick()  // Called every frame
    {
        if (IsNotSideWall())
            timeLeftWall += Time.deltaTime;
        else
            timeLeftWall = 0f;

        if (timeLeftWall > config.WALL_RUN_GRACE)
        {
            Cancel();
            return;
        }
    }
    
    public void FixedTick() // Called every physics update
    {
        //TODO: convert all of this into a force vector that we can just set the rb.linearVelocity to.

        Vector3 dirToWall = -movement.wallHit.normal;

        // only apply stick as long as input is not away from the wall (Probably use movement.MoveDirection)
        // otherwise if we want to move away from the wall push off wall
        rb.AddForce(dirToWall * config.WALL_STICKINESS, ForceMode.Force);

        if (movement.MoveInput.y <= .25f)
        {
            rb.AddForce(FlowPhysics.Instance.GravityDirection * config.WALL_SLIP_SPEED, ForceMode.Force);
            Debug.LogWarning("Slipping");
        }
    }

    public void Cancel()
    {
        // Cancel logic here
        isWallRunning = false;
        config.IsActive = false;
        rb.useGravity = true;
        movement.RemoveRestraint(this);
    }

    private bool IsNotSideWall()
    {
        return movement.wallDirection != WallDirection.Left && movement.wallDirection != WallDirection.Right;
    }
}
