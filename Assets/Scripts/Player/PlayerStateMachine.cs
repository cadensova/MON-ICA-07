using Unity.VisualScripting;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public enum MainState { Grounded, Crouching, WallRunning, Airborne }
    public enum SubState { Idle, Walking, Moving, Jumping, Falling, Sliding }

    [field: SerializeField] public MainState CurrentMain { get; private set; }
    [field: SerializeField] public SubState CurrentSub { get; private set; }


    private PlayerMovement movement;
    private bool isGrounded => movement.IsGrounded;
    private bool isCrouching => movement.IsCrouching;
    private bool isSliding => movement.IsSliding;
    private bool isWalking => movement.IsWalking;
    private Vector2 moveInput => movement.MoveInput;



    [Header("State Transitions")]
    public float fallThreshold { get; private set; } = -1f;
    public bool JustLanded { get; private set; } = false;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        SetState(MainState.Grounded, SubState.Idle);
    }

    private bool Verify()
    {
        if (movement == null)
        {
            Debug.LogError("PlayerMovement component is missing.");
            return false;
        }
        if (movement.abilities == null)
        {
            Debug.LogError("PlayerMovement.abilities is not set.");
            return false;
        }

        return true;
    }

    private void Update()
    {
        if (!Verify()) return;

        if (movement.abilities.IsAbilityActive("Slide"))
        {
            SetSubState(SubState.Sliding);

            SetMainState(movement.IsGrounded ? MainState.Grounded : MainState.Airborne);
            return;
        }

        if (movement.abilities.IsAbilityActive("WallRun"))
        {
            SetMainState(MainState.WallRunning);
            SetSubState(moveInput.magnitude >= 0.1f ? SubState.Moving : SubState.Idle);
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
                if (isWalking)
                    SetSubState(SubState.Walking);
                else
                    SetSubState(SubState.Moving);
            }
        }
        else
        {
            if (CurrentMain != MainState.Airborne)
                SetMainState(MainState.Airborne);

            if (movement.abilities.IsAbilityActive("Jump"))
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
