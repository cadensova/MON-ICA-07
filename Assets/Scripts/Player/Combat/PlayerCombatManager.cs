using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PlayerCombatManager : MonoBehaviour
{
    [Header("Assign Your Skills (ScriptableObjects)")]
    [SerializeField] private List<PlayerSkill> skills = new List<PlayerSkill>();

    // COMPONENTS (same as before)
    public PlayerMovement movement { get; private set; }
    public CapsuleCollider col { get; private set; }
    public Rigidbody rb { get; private set; }
    public InputProvider input { get; private set; }
    public Transform ori { get; private set; }
    public PlayerStateMachine sm { get; private set; }

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

    private void Update()
    {
        // You can put combo-window logic or animations here—
        // but the actual “meat” is in each Skill’s Execute().
    }
}
