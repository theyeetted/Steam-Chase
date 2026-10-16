using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FollowUpAttackEffect : Effect
{
    [Tooltip("Extra damage for this Follow-Up strike only. Ordinary damage buffs do not apply.")]
    [SerializeField, Min(0)] private int followUpDamageBonus;

    [Tooltip("After this Follow-Up hits, add this percentage of the actual target's maximum Stun gauge, even through Block. Fractional points round up; Stun cooldown still applies. Zero adds no Stun.")]
    [SerializeField, Range(0, 100)] private int stunFillPercent;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new FollowUpAttackGA(targets, caster, followUpDamageBonus, stunFillPercent);
    }
}
