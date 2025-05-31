using Unity.VisualScripting;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public enum MainState { Grounded, Crouching, Airborne }
    public enum SubState { Idle, Walking, Moving, Jumping, Falling, Sliding }

    [field: SerializeField] public MainState CurrentMain { get; private set; }
    [field: SerializeField] public SubState CurrentSub { get; private set; }

    private PlayerMovement movement;
    private bool isGrounded => movement.IsGrounded;
    private bool isCrouching => movement.IsCrouching;
    private bool isSliding => movement.IsSliding;
    private bool IsWalking => movement.IsWalking;
    private Vector2 moveInput => movement.MoveInput;

    [Header("State Transitions")]
    public float fallThreshold = -1f;
    public bool JustLanded { get; private set; } = false;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        SetState(MainState.Grounded, SubState.Idle);
    }

    private void Update()
    {
        if (movement.IsSliding)
        {
            SetSubState(SubState.Sliding);
            return;
        }

        if (isGrounded)
        {
            if (CurrentMain != MainState.Grounded)
                SetMainState(isCrouching ? MainState.Crouching : MainState.Grounded);

            if (moveInput.magnitude < 0.1f && CurrentSub != SubState.Idle)
                SetSubState(SubState.Idle);
            else if (moveInput.magnitude >= 0.1f)
            {
                if (IsWalking)
                    SetSubState(SubState.Walking);
                else
                    SetSubState(SubState.Moving);
            }
        }
            else
            {
                if (CurrentMain != MainState.Airborne)
                    SetMainState(MainState.Airborne);

                if (movement.GetVelocity().y > fallThreshold)
                    SetSubState(SubState.Jumping);
                else if (movement.GetVelocity().y <= fallThreshold)
                    SetSubState(SubState.Falling);
            }

        if (JustLanded)
            JustLanded = false;
    }

    private void SetState(MainState main, SubState sub)
    {
        if (CurrentMain == main && CurrentSub == sub)
            return;

        //Debug.Log($"State changed: Main = {main}, Sub = {sub}");
        CurrentMain = main;
        CurrentSub = sub;
    }
    
    private void SetMainState(MainState main)
    {
        if (main == MainState.Grounded && CurrentMain == MainState.Airborne)
            JustLanded = true;

        if (CurrentMain == main)
                return;

        //Debug.Log($"Main state: {main}");
        CurrentMain = main;
    }

    private void SetSubState(SubState sub)
    {
        if (CurrentSub == sub)
            return;

        //Debug.Log($"Sub state: {sub}");
        CurrentSub = sub;
    }
}
