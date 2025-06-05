using System;
using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections;


public abstract class PlayerSkill : ScriptableObject, IPlayerSkill
{
    public void Execute()
    {
        OnExecute();
    }


    // ── 1) STATE / BINDING SETUP ────────────────────────────────────────────────


    [Header("Skill Binding & State")]
    [SerializeField] protected PlayerButton boundButton = PlayerButton.NONE;
    [SerializeField] protected PlayerStateMachine.MainState requiredMainState = PlayerStateMachine.MainState.NONE;
    [SerializeField] protected PlayerStateMachine.SubState requiredSubState = PlayerStateMachine.SubState.NONE;
    protected SimpleButton skillInputButton;
    

    // ── PLAYER COMPONENTS ───────────────────────────────────────────

    protected GameObject userGO;
    protected PlayerMovement movement;
    protected PlayerCombatManager combat;
    protected Rigidbody rb;
    protected CapsuleCollider col;
    protected Transform ori;
    protected PlayerStateMachine sm;

    public bool IsActive { get; protected set; } = false;


    // ── INITIALIZATION ────────────────────────────────────────────────────

    public void Initialize(
        GameObject _user,
        PlayerCombatManager _combat
    )
    {
        userGO = _user;
        combat = _combat;
        movement = _user.GetComponent<PlayerMovement>();
        sm = _combat.sm;
        rb = _combat.rb;
        col = _combat.col;
        ori = userGO.transform.Find("Orientation") ?? userGO.transform;

        if (sm == null || rb == null || col == null || ori == null)
        {
            Debug.LogWarning("Player skill failed! " + name);
        }

        if (boundButton != PlayerButton.NONE)
        {
            skillInputButton = InputProvider.Instance.GetButton(boundButton);
            if (skillInputButton == null)
            {
                Debug.LogWarning($"[{name}] couldn’t find SimpleButton for {boundButton}. Skill will never fire.");
            }
            else
            {
                skillInputButton.OnPressed += TryUseSkill;
            }
        }

        OnInitialize();
    }


    protected virtual void OnInitialize() { }
    public void Uninitialize()
    {
        if (skillInputButton != null)
            skillInputButton.OnPressed -= TryUseSkill;
    }


    private void TryUseSkill()
    {
        // If the skill doesn’t care about MainState, leave requiredMainState = NONE. Otherwise do the check:
        if (requiredMainState != PlayerStateMachine.MainState.NONE
            && sm.CurrentMain != requiredMainState)
        {
            return;
        }

        // If the skill doesn’t care about SubState, leave requiredSubState = NONE. Otherwise do the check:
        if (requiredSubState != PlayerStateMachine.SubState.NONE
            && sm.CurrentSub != requiredSubState)
        {
            return;
        }

        // Passed both checks → perform the actual skill logic
        OnExecute();
    }


    public abstract void OnExecute();
}
