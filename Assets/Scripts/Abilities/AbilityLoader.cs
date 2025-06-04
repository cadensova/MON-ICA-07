using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbilityLoader : MonoBehaviour
{
    /// <summary>
    /// Once each config’s SetUp(...) is called at runtime, its RuntimeAbility 
    /// will hold a fully‐initialized IAbility instance. We collect those here.
    /// </summary>
    [field: SerializeField]
    public List<IAbility> loadedAbilities { get; private set; } = new();

    private PlayerMovement movement;

    //
    // Instead of: GetComponentsInChildren<IAbilityConfig>() (which only picks up 
    // MonoBehaviours), we now let you drag any ScriptableObject into this list.
    // At runtime, we check “is this object an IAbilityConfig?” and then SetUp().
    //
    [Header("Drag‐and‐drop your ScriptableObject configs here")]
    [Tooltip("Anything in this list that implements IAbilityConfig (e.g. JumpConfig, SlideConfig, WallRunConfig, etc.) will be SetUp(...) at Start().")]
    [SerializeField]
    private List<ScriptableObject> abilityConfigObjects = new List<ScriptableObject>();

    private void Start()
    {
        // Find PlayerMovement on the root (your Player GameObject):
        movement = transform.root.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError("AbilityLoader: PlayerMovement component not found on root GameObject.");
            return;
        }

        // We wait until movement.Verified is true (if you’re doing any “late initialization” there):
        StartCoroutine(LoadAbilities());
    }

    private IEnumerator LoadAbilities()
    {
        // If PlayerMovement.Verified is false, wait until it becomes true
        if (!movement.Verified)
            yield return new WaitUntil(() => movement.Verified);

        // Loop over every ScriptableObject the designer dragged into the Inspector:
        foreach (var so in abilityConfigObjects)
        {
            if (so is IAbilityConfig config)
            {
                // Call SetUp(owner), which should internally do:
                //     RuntimeAbility = new TAbility(); 
                //     RuntimeAbility.Initialize(owner, this);
                config.SetUp(transform.root.gameObject);

                // After SetUp, config.RuntimeAbility should be non‐null:
                var runtimeProp = config.GetType().GetProperty("RuntimeAbility");
                if (runtimeProp?.GetValue(config) is IAbility ability)
                {
                    loadedAbilities.Add(ability);
                    //Debug.Log($"Loaded ability: {ability.Id} (RequiresTicking = {ability.RequiresTicking})");
                }
                else
                {
                    Debug.LogWarning($"AbilityLoader: Config '{so.name}' did not yield a RuntimeAbility.");
                }
            }
            else
            {
                Debug.LogWarning($"AbilityLoader: ScriptableObject '{so.name}' does not implement IAbilityConfig, so it was skipped.");
            }
        }

        //Debug.Log($"AbilityLoader: Finished loading {loadedAbilities.Count} abilities.");
    }

    private void Update()
    {
        // Tick() only called if ability.RequiresTicking == true && ability.IsActive == true
        for (int i = 0; i < loadedAbilities.Count; i++)
        {
            var ability = loadedAbilities[i];
            if (!ability.RequiresTicking || !ability.IsActive) 
                continue;

            ability.Tick();
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < loadedAbilities.Count; i++)
        {
            var ability = loadedAbilities[i];
            if (!ability.RequiresTicking || !ability.IsActive) 
                continue;

            ability.FixedTick();
        }
    }

    /// <summary>
    /// Returns the first loaded ability whose type matches T. 
    /// For example: GetAbility<JumpAbility>().
    /// </summary>
    public T GetAbility<T>() where T : class, IAbility
    {
        for (int i = 0; i < loadedAbilities.Count; i++)
        {
            if (loadedAbilities[i] is T typed)
                return typed;
        }
        return null;
    }

    /// <summary>
    /// Returns true if an ability with the given Id is currently active.
    /// </summary>
    public bool IsAbilityActive(string abilityId)
    {
        for (int i = 0; i < loadedAbilities.Count; i++)
        {
            if (loadedAbilities[i].Id == abilityId)
                return loadedAbilities[i].IsActive;
        }
        return false;
    }
}
