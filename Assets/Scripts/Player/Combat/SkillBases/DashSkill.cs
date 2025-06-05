using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "DashSkill", menuName = "PlayerSkills/Dash")]
public class DashSkill : PlayerSkill
{
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Detection Settings")]
    [SerializeField] private float dashDetectRange = 1.5f;
    [SerializeField] private float dashDetectRadius = 0.5f;
    [SerializeField] private LayerMask detectionMask;

    [Header("Conclusion Settings")]
    [SerializeField] private float minDis = 1f;

    private Transform cameraTransform;
    private Transform dashTarget;

    private bool subbed = false;
    private bool inputPressed = false;
    private float lastDashTime = 0f;
    private bool gotClose;

    private void OnEnable()
    {
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;

        IsActive = false;
        gotClose = false;
        lastDashTime = 0f;
    }

    public override void OnExecute()
    {
        // Don’t even try if PlayerCombatManager hasn’t finished subscribing inputs
        if (!combat.subscribed) 
            return;

        if (lastDashTime > Time.time) return;
        
        // 1) Subscribe exactly once, using a named method
        if (!subbed && skillInputButton != null)
        {
            skillInputButton.OnPressed += OnDashPressed;
            subbed = true;
        }

        // 2) Find the “closest hit” in front of the camera
        RaycastHit? bestHit = FindBestHitInFrontOfCamera();

        if (bestHit.HasValue)
        {
            dashTarget = bestHit.Value.transform;

            // 3) Only start a dash if (a) the button was pressed, (b) we’re not already active,
            // (c) the cooldown has expired:
            if (inputPressed && !IsActive)
            {
                inputPressed = false;    // consume the press
                IsActive = true;
                //Debug.Log("Started Dash");
                combat.StartCoroutine(HandleDashingLogic(dashTarget.GetComponent<Rigidbody>()));
            }
        }
        else
        {
            // No valid target in sight → reset dashTarget so coroutine can bail if needed
            dashTarget = null;
        }
    }

    private void OnDashPressed()
    {
        // Called once by the input‐system when the dash button is pressed
        inputPressed = true;
    }

    public RaycastHit? FindBestHitInFrontOfCamera()
    {
        if (cameraTransform == null) return null;

        RaycastHit[] hits = Physics.SphereCastAll(
            cameraTransform.position,
            dashDetectRadius,
            cameraTransform.forward,
            dashDetectRange,
            detectionMask
        );

        Debug.DrawLine(cameraTransform.position, cameraTransform.position + cameraTransform.forward * dashDetectRange, Color.yellow);

        if (hits.Length == 0)
            return null;

        Camera cam = Camera.main;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        float bestDist = Mathf.Infinity;
        RaycastHit bestHit = new RaycastHit();

        foreach (var hit in hits)
        {
            Vector3 vp = cam.WorldToViewportPoint(hit.point);
            if (vp.z < 0f) 
                continue;

            float d = Vector2.Distance(new Vector2(vp.x, vp.y), screenCenter);
            if (d < bestDist)
            {
                bestDist = d;
                bestHit = hit;
            }
        }

        if (bestDist == Mathf.Infinity) 
            return null;

        return bestHit;
    }

    private IEnumerator HandleDashingLogic(Rigidbody targetRB)
    {
        // 1) Add a movement restraint so player can’t move during dash
        movement.AddRestraint(this);

        // 2) Temporarily disable gravity so we don’t drop mid‐dash
        rb.useGravity = false;

        // 3) Immediately fling the player toward the dashTarget’s position
        if (dashTarget != null)
        {
            Vector3 direction = (dashTarget.position - userGO.transform.position).normalized;
            rb.AddForce(direction * dashSpeed, ForceMode.VelocityChange);
            targetRB.isKinematic = true;
        }

        // 4) Wait until either we’re close enough (<=minDis) OR we’ve overshot (> dashDetectRange)
        yield return new WaitUntil(() =>
        {
            if (dashTarget == null)
                return true; // target disappeared, end dash

            float currentDist = Vector3.Distance(dashTarget.position, userGO.transform.position);

            if (currentDist <= minDis)
            {
                Debug.Log("Got Close!");
                gotClose = true;
            }

            return (gotClose || currentDist > dashDetectRange + 5f);
        });

        StopDash(targetRB);
    }

    private void StopDash(Rigidbody targetRB = null)
    {
        // 1) Remove movement restraint so player regains control
        movement.RemoveRestraint(this);

        // 2) Re‐enable gravity
        rb.useGravity = true;

        // 4) Reset flags & start cooldown
        lastDashTime = Time.time + dashCooldown;
        inputPressed = false;
        IsActive = false;
        dashTarget = null;

        rb.linearVelocity = Vector3.zero;

        targetRB.isKinematic = false;
        float dis = Vector3.Distance(targetRB.transform.position, userGO.transform.position);
        // 5) Apply knockback to the target if it still exists
        if (targetRB != null && dis <= 5f)
        {
            // Knock it in the direction the player is facing
            targetRB.AddForce(cameraTransform.forward * knockbackForce, ForceMode.Impulse);
            gotClose = false;
        }
        else Debug.LogWarning(dis);

        //Debug.Log("Stopping dash; cooldown will expire at " + (lastDashTime + dashCooldown).ToString("F2"));
    }

    private void OnDisable()
    {
        // Unsubscribe the exact same method we subscribed earlier
        if (subbed && skillInputButton != null)
            skillInputButton.OnPressed -= OnDashPressed;

        subbed = false;
        gotClose = false;
        inputPressed = false;
        lastDashTime = 0f;
    }
}
