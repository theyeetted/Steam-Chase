using System;
using System.Collections.Generic;

/// <summary>
/// Runtime bonuses owned by one combatant. Each grant keeps its own expiration.
/// Zero remaining turns means permanent for this battle. Stacks represent bonus damage.
/// </summary>
public sealed class StatusDamageBuffs
{
    private sealed class Grant
    {
        public StatusEffectType Type;
        public int Amount;
        public int RemainingTurns;
    }

    private readonly List<Grant> grants = new();

    public bool HasTimedBuffs => grants.Exists(grant => grant.RemainingTurns > 0);

    public static bool IsDamageBuff(StatusEffectType type)
    {
        return type == StatusEffectType.MarkFollowUpDamageUp ||
            type == StatusEffectType.FollowUpAttackDamageUp ||
            type == StatusEffectType.ExtraAttackDamageUp;
    }

    public int GetBonus(StatusEffectType type)
    {
        int total = 0;
        foreach (Grant grant in grants)
        {
            if (grant.Type == type)
                total += Math.Min(grant.Amount, int.MaxValue - total);
        }
        return total;
    }

    public bool Add(StatusEffectType type, int amount, int turns)
    {
        if (!IsDamageBuff(type) || amount <= 0 || turns < 0)
            return false;

        // Keep both the displayed stack total and the damage calculation within integer range.
        int acceptedAmount = Math.Min(amount, int.MaxValue - GetBonus(type));
        if (acceptedAmount == 0)
            return false;

        grants.Add(new Grant { Type = type, Amount = acceptedAmount, RemainingTurns = turns });
        return true;
    }

    public void Remove(StatusEffectType type, int amount)
    {
        // Cleansing removes bonus points from the oldest grants first, including permanent ones.
        for (int i = 0; i < grants.Count && amount > 0;)
        {
            Grant grant = grants[i];
            if (grant.Type != type)
            {
                i++;
                continue;
            }

            int removed = Math.Min(amount, grant.Amount);
            grant.Amount -= removed;
            amount -= removed;
            if (grant.Amount == 0)
                grants.RemoveAt(i);
            else
                i++;
        }
    }

    public HashSet<StatusEffectType> AdvanceTurn()
    {
        HashSet<StatusEffectType> expiredTypes = new();
        for (int i = grants.Count - 1; i >= 0; i--)
        {
            Grant grant = grants[i];
            if (grant.RemainingTurns == 0)
                continue;

            grant.RemainingTurns--;
            if (grant.RemainingTurns == 0)
            {
                expiredTypes.Add(grant.Type);
                grants.RemoveAt(i);
            }
        }
        return expiredTypes;
    }

    public HashSet<StatusEffectType> Clear()
    {
        HashSet<StatusEffectType> clearedTypes = new();
        foreach (Grant grant in grants)
            clearedTypes.Add(grant.Type);
        grants.Clear();
        return clearedTypes;
    }
}
