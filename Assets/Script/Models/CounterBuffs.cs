using System;
using System.Collections.Generic;

/// <summary>Counter damage grants for one hero, each with its own enemy-turn duration.</summary>
public sealed class CounterBuffs
{
    private sealed class Grant
    {
        public int Damage;
        // Zero means the rest of this combat; timed grants are removed upon reaching zero.
        public int RemainingTurns;
        public int AdditionalStunPercent;
    }

    private readonly List<Grant> grants = new();

    public bool HasTimedBuffs => grants.Exists(grant => grant.RemainingTurns > 0);

    public int Damage
    {
        get
        {
            int total = 0;
            foreach (Grant grant in grants)
                total += grant.Damage;
            return total;
        }
    }

    // Damage combines into one strike. Each surviving grant contributes its own bonus;
    // the final percentage is bounded because a gauge cannot fill past its maximum.
    public int AdditionalStunPercent
    {
        get
        {
            int total = 0;
            foreach (Grant grant in grants)
                total = Math.Min(100, total + grant.AdditionalStunPercent);
            return total;
        }
    }

    public bool Add(int damage, int turns, int additionalStunPercent = 0)
    {
        if (damage <= 0 || turns < 0)
            return false;

        int acceptedDamage = Math.Min(damage, int.MaxValue - Damage);
        if (acceptedDamage == 0)
            return false;

        grants.Add(new Grant
        {
            Damage = acceptedDamage,
            RemainingTurns = turns,
            AdditionalStunPercent = Math.Max(0, Math.Min(100, additionalStunPercent))
        });
        return true;
    }

    public void Remove(int damage)
    {
        // Shared status removal consumes damage points from the oldest grants first.
        for (int i = 0; i < grants.Count && damage > 0;)
        {
            Grant grant = grants[i];
            int removed = Math.Min(damage, grant.Damage);
            grant.Damage -= removed;
            damage -= removed;
            if (grant.Damage == 0)
                grants.RemoveAt(i);
            else
                i++;
        }
    }

    public void AdvanceTurn()
    {
        for (int i = grants.Count - 1; i >= 0; i--)
        {
            Grant grant = grants[i];
            if (grant.RemainingTurns == 0)
                continue;

            grant.RemainingTurns--;
            if (grant.RemainingTurns == 0)
                grants.RemoveAt(i);
        }
    }

    public void Clear()
    {
        grants.Clear();
    }
}
