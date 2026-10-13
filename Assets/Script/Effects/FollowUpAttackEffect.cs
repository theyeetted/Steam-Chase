using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FollowUpAttackEffect : Effect
{
    [Tooltip("Extra damage for this Follow-Up strike only. Ordinary damage buffs do not apply.")]
    [SerializeField, Min(0)] private int followUpDamageBonus;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new FollowUpAttackGA(targets, caster, followUpDamageBonus);
    }
}
