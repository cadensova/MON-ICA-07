using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PlayerCombatManager : MonoBehaviour
{
    [Header("Assign Your Skills (ScriptableObjects)")]
    [SerializeField] private List<PlayerSkill> skills = new List<PlayerSkill>();
    public List<PlayerSkill> Skills => skills;

    // COMPONENTS (same as before)
    public PlayerMovement movement { get; private set; }
    public CapsuleCollider col { get; private set; }
    public Rigidbody rb { get; private set; }
    public InputProvider input { get; private set; }
    public Transform ori { get; private set; }
    public PlayerStateMachine sm { get; private set; }


    private SlideBumpSkill slideSkill;


    [Header("Targeting & Physics")]
    [SerializeField] private LayerMask hitable;
    

    private Coroutine subscribeCoroutine;
    public bool subscribed { get; private set; } = false;

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

    private IEnumerator SubscribeWhenReady()
    {
        // Wait for everything to initialize (InputProvider, FlowPhysics, etc.)
        while (InputProvider.Instance == null || FlowPhysics.Instance == null)
        {
            yield return new WaitForSeconds(0.05f);
        }

        input = InputProvider.Instance;
        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        ori = transform.Find("Orientation");
        sm = GetComponent<PlayerStateMachine>();

        if (movement == null || rb == null || col == null || sm == null)
        {
            Debug.LogError("PlayerCombatManager: missing required components.");
            yield break;
        }

        AssignInputs();
        Debug.Log("PlayerCombatManager subscribed to input.");
        subscribed = true;
    }

    private void AssignInputs()
    {
        foreach (PlayerSkill skill in skills)
        {
            skill.Initialize(gameObject, this);

            if (skill is SlideBumpSkill && slideSkill == null)
                slideSkill = (SlideBumpSkill)skill;
        }
        
    }

    private void Unsubscribe()
    {
        if (subscribeCoroutine != null)
            StopCoroutine(subscribeCoroutine);

        UninitializeAllSkills();
        subscribed = false;
    }

    private void UninitializeAllSkills()
    {
        foreach (PlayerSkill skill in skills)
        {
            skill.Uninitialize();
        }
    }

    public bool IsSkillActive(string skillToCheck)
    {
        var skill = skills.Find(s => s.name == skillToCheck + "Skill");
        if (skill != null)
            return skill.IsActive;

        Debug.LogWarning("Couldn't find skill: " + skillToCheck);

        return false;
    }

    private void Update()
    {
        // You can put combo-window logic or animations here—
        // but the actual “meat” is in each Skill’s Execute().
        if (!subscribed) return;

        if (sm.CurrentSub == PlayerStateMachine.SubState.Sliding)
            slideSkill?.OnExecute();
        else if (slideSkill?.bumped.Count > 0)
            slideSkill?.Clear();
    }
}
