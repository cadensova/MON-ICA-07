using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AbilityLoader : MonoBehaviour
{
    [field: SerializeField] public List<IAbility> loadedAbilities { get; private set; } = new();
    private PlayerMovement movement;

    private void Start()
    {
        movement = transform.root.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError("PlayerMovement component not found on root GameObject.");
            return;
        }

        StartCoroutine(LoadAbilities());
    }

    private IEnumerator LoadAbilities()
    {
        if (!movement.subscribed)
            yield return new WaitUntil(() => movement.subscribed);

        foreach (var config in GetComponentsInChildren<IAbilityConfig>())
        {
            if (config is MonoBehaviour mb)
            {
                var method = config.GetType().GetMethod("SetUp");
                method?.Invoke(config, new object[] { transform.root.gameObject });

                var abilityProp = config.GetType().GetProperty("RuntimeAbility");
                if (abilityProp?.GetValue(config) is IAbility ability)
                {
                    loadedAbilities.Add(ability);
                    //Debug.Log($"Loaded ability: {ability.Id} from config: {config.GetType().Name}. Does it require ticking? {ability.RequiresTicking}");
                }
            }
        }

        //Debug.Log("Loaded  " + loadedAbilities.Count + "abilities.");
    }

    private void Update()
    {
        foreach (var ability in loadedAbilities)
        {
            if (!ability.RequiresTicking || !ability.IsActive) continue;

            ability.Tick();
        }
    }

    private void FixedUpdate()
    {
        foreach (var ability in loadedAbilities)
        {
            if (!ability.RequiresTicking || !ability.IsActive) continue;

            ability.FixedTick();
        }
    }

    public T GetAbility<T>() where T : class, IAbility
    {
        foreach (var ability in loadedAbilities)
        {
            if (ability is T typed)
                return typed;
        }
        return null;
    }
    
    public bool IsAbilityActive(string abilityId)
    {
        foreach (var ability in loadedAbilities)
        {
            if (ability.Id == abilityId)
                return ability.IsActive;
        }
        return false;
    }
}
