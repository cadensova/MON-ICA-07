using UnityEngine;

public class CameraTilt : MonoBehaviour
{
    [Header("Roll Tilt Settings")]
    [SerializeField] private float maxTiltAngle = 10f;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float movementTiltAmount = 1.5f;

    [Header("Pitch Impulse Settings")]
    [SerializeField] private float jumpPitchAmount = -5f;
    [SerializeField] private float landPitchAmount = 8f;
    [SerializeField] private float pitchDecaySpeed = 5f;

    private float currentRoll = 0f;
    private float targetRoll = 0f;
    private float rollVelocity = 0f;

    private float currentPitch = 0f;
    private float targetPitch = 0f;
    private float pitchVelocity = 0f;

    private bool wasGroundedLastFrame = true;

    private bool overrideTilt = false;
    private float overrideAmount = 0f;
    private InputProvider input;
    private PlayerMovement player;
    private float bonusTilt;

    public static CameraTilt Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        input = InputProvider.Instance;
        player = FindFirstObjectByType<PlayerMovement>();
    }

    void Update()
    {
        Vector2 lookInputNormalized = input.LookInput.normalized;

        // ── Roll (side to side tilt)
        float moveTilt = -input.MoveInput.x * movementTiltAmount;

        targetRoll = overrideTilt 
            ? Mathf.Clamp(overrideAmount, -maxTiltAngle, maxTiltAngle)
            : Mathf.Clamp(moveTilt + bonusTilt, -maxTiltAngle, maxTiltAngle);

        currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, smoothTime);

        // ── Pitch (jump/land impulse tilt)
        bool isGrounded = player.IsGrounded;

        if (!wasGroundedLastFrame && isGrounded)
            targetPitch = landPitchAmount;
        else if (wasGroundedLastFrame && !isGrounded && input.Jump.IsPressed)
            targetPitch = jumpPitchAmount;

        wasGroundedLastFrame = isGrounded;

        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, smoothTime);
        targetPitch = Mathf.MoveTowards(targetPitch, 0f, pitchDecaySpeed * Time.deltaTime);
    }

    private void LateUpdate()
    {
        Quaternion rollTilt = Quaternion.AngleAxis(currentRoll, Vector3.forward);
        Quaternion pitchTilt = Quaternion.AngleAxis(currentPitch, Vector3.right);

        transform.localRotation = pitchTilt * rollTilt;
    }


    /// <summary>
    /// Request a tilt angle,
    /// This is additive and does not override other state inputs unless they call it.
    /// </summary>
    public void SetTilt(float angle, bool wantsToOverride = false)
    {
        if (wantsToOverride)
        {
            overrideTilt = true;
            overrideAmount = angle;
        }
        else
        {
            // Additive influence — modifies roll target
            bonusTilt += angle;
        }
    }

    public void ResetTilt()
    {
        overrideTilt = false;
        overrideAmount = 0f;
        bonusTilt = 0;
    }
}
