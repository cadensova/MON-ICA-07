// CombatActor.cs
using System.Collections.Generic;
using UnityEngine;

public class CombatActor : MonoBehaviour
{
    // All afflictions currently on this actor
    [field: SerializeField] private List<Affliction> afflictions = new List<Affliction>();
    private Dictionary<string, Affliction> afflictionLookup = new Dictionary<string, Affliction>();

    #region Public API

    public void AddAffliction(Affliction newAffliction)
    {
        if (afflictionLookup.TryGetValue(newAffliction.Name, out var existing))
        {
            if (existing.TryStack(newAffliction))
            {
                return;
            }
            existing.SetDuration(Mathf.Max(existing.Duration, newAffliction.Duration));
            return;
        }

        afflictions.Add(newAffliction);
        afflictionLookup[newAffliction.Name] = newAffliction;
        newAffliction.OnApply(this);
    }

    public void RemoveAffliction(Affliction aff)
    {
        if (afflictions.Contains(aff))
        {
            aff.OnExpire();
            afflictions.Remove(aff);
            afflictionLookup.Remove(aff.Name);
        }
    }

    public Affliction GetAfflictionInstance(string name)
    {
        foreach (Affliction a in afflictions)
        {
            if (a.Name == name)
            {
                return a;
            }
        }

        Debug.LogWarning("Affliction: " + name + ", not found on: " + gameObject.name);
        return null;
    } 

    public void ProcessEndOfTurnAfflictions()
    {
        List<Affliction> expired = new List<Affliction>();
        foreach (var aff in afflictions)
        {
            if (aff.Type == Affliction.AType.EOT)
            {
                aff.OnTick();
                if (aff.Duration <= 0)
                    expired.Add(aff);
            }
        }
        foreach (var e in expired)
        {
            e.OnExpire();
            afflictions.Remove(e);
            afflictionLookup.Remove(e.Name);
        }
    }

    #endregion

    #region New Helper Methods for Affliction Stacks

    /// <summary>
    /// Returns how many stacks of the named affliction this actor currently has.
    /// If none, returns 0.
    /// </summary>
    public int GetAfflictionStacks(string afflictionName)
    {
        if (afflictionLookup.TryGetValue(afflictionName, out var aff))
        {
            return aff.CurrentStacks;
        }
        return 0;
    }

    /// <summary>
    /// Removes (and expires) the named affliction if present.
    /// Useful to “consume” any VelocityBurst stacks after a punch.
    /// </summary>
    public void ConsumeAffliction(string afflictionName)
    {
        if (afflictionLookup.TryGetValue(afflictionName, out var aff))
        {
            RemoveAffliction(aff);
        }
    }

    #endregion

    #region Example: Taking Damage & Healing (stubbed out)

    public void TakeDamage(int amount)
    {
        // You’ll wire this up later when you have HP
    }

    private void Die()
    {
        Debug.Log($"{name} died!");
    }

    #endregion
}
