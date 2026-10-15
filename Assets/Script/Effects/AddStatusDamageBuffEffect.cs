using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grants additional damage for a selected kind of status attack through AddStatusEffectGA.
/// Use HeroTM to buff the player who applies Mark, not the enemy carrying Mark stacks.
/// </summary>
/// <remarks>
/// Example: HeroTM + this effect, Attack = MarkFollowUp, Bonus Damage = 8,
/// Duration Turns = 2, Permanent = false. Mark reactions deal 4 + 8 before Block.
/// The current owner turn counts: the hero ticks at EnemyTurnGA PRE; enemies tick at
/// their own AttackHeroGA POST. A two-turn grant expires on the second such boundary.
/// Permanent grants last for this battle and are cleared when the unit is set up again.
/// Reapplications add separate bonuses with independent timers; they do not refresh old grants.
/// Existing StatusEffectSystem handles this effect; no additional scene component is needed.
/// The status icon count is total bonus damage, not remaining turns or attack charges.
/// </remarks>
[System.Serializable]
public class AddStatusDamageBuffEffect : Effect
{
    public enum AttackKind
    {
        MarkFollowUp,
        FollowUpAttack,
        ExtraAttack
    }

    [SerializeField] private AttackKind attack = AttackKind.MarkFollowUp;
    [SerializeField, Min(1)] private int bonusDamage = 8;
    [Tooltip("Lasts until battle reset. Ignores Duration Turns.")]
    [SerializeField] private bool permanent;
    [Tooltip("Owner turn ends until expiration, counting the turn when applied. Ignored if Permanent is enabled.")]
    [SerializeField, Min(1)] private int durationTurns = 2;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        StatusEffectType type = attack switch
        {
            AttackKind.MarkFollowUp => StatusEffectType.MarkFollowUpDamageUp,
            AttackKind.FollowUpAttack => StatusEffectType.FollowUpAttackDamageUp,
            AttackKind.ExtraAttack => StatusEffectType.ExtraAttackDamageUp,
            _ => StatusEffectType.MarkFollowUpDamageUp
        };
        int turns = permanent ? 0 : Mathf.Max(1, durationTurns);
        return new AddStatusEffectGA(type, Mathf.Max(0, bonusDamage), targets, caster, turns);
    }
}
