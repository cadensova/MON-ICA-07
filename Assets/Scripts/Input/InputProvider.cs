using UnityEngine;
using UnityEngine.InputSystem;


public enum ControlScheme
{
    KeyboardMouse,
    Gamepad
}

public class InputProvider : MonoBehaviour
{
    public ControlScheme CurrentControlScheme { get; private set; } = ControlScheme.KeyboardMouse;

    public static InputProvider Instance { get; private set; }

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }


    // MOVEMENT
    public SimpleButton Jump { get; private set; } = new();
    public SimpleButton Crouch { get; private set; } = new();
    public SimpleButton ToggleWalk { get; private set; } = new();


    // COMBAT
    public SimpleButton Slice { get; private set; } = new();
    public SimpleButton Punch { get; private set; } = new();


    // CONSOLE / DEVELOPER INPUTS
    public SimpleButton VSyncToggle { get; private set; } = new();


    private PlayerInput playerInput;

    [Header("Deadzones")]
    [SerializeField] private float moveDeadzone = 0.1f;
    [SerializeField] private float lookDeadzone = 0.05f;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerInput = GetComponent<PlayerInput>();
        playerInput.actions.FindActionMap("Developer").Enable();
        playerInput.actions.FindActionMap("Combat").Enable();

        playerInput.actions["Move"].performed += ctx =>
        {
            Vector2 raw = ctx.ReadValue<Vector2>();
            MoveInput = raw.magnitude < moveDeadzone ? Vector2.zero : raw.normalized * ((raw.magnitude - moveDeadzone) / (1 - moveDeadzone));

            UpdateControlScheme(ctx.control.device);
        };

        playerInput.actions["Move"].canceled += ctx => MoveInput = Vector2.zero;

        playerInput.actions["Look"].performed += ctx =>
        {
            Vector2 raw = ctx.ReadValue<Vector2>();
            LookInput = raw.magnitude < lookDeadzone ? Vector2.zero : raw.normalized * ((raw.magnitude - lookDeadzone) / (1 - lookDeadzone));
        };

        playerInput.actions["Look"].canceled += ctx => LookInput = Vector2.zero;


        // MOVEMENT
        BindAction("Jump", Jump);
        BindAction("Crouch", Crouch);
        BindAction("ToggleWalk", ToggleWalk);

        // COMBAT
        BindAction("Slice", Slice);
        BindAction("Punch", Punch);

        // CONSOLE / DEVELOPER INPUTS
        BindAction("VSync", VSyncToggle);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void BindAction(string name, SimpleButton button)
    {
        playerInput.actions[name].performed += ctx =>
        {
            button.Press();

            UpdateControlScheme(ctx.control.device);
        };

        playerInput.actions[name].canceled += ctx => button.Release();
    }


    private void UpdateControlScheme(InputDevice device)
    {
        if (device == null)
            return;

        if (device is Gamepad)
            CurrentControlScheme = ControlScheme.Gamepad;
        else
            CurrentControlScheme = ControlScheme.KeyboardMouse;
    }

}
