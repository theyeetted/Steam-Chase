using System.Collections.Generic;
using UnityEngine;

// A separate action type keeps ordinary DealDamageGA modifiers out of Follow-Up damage.
public class FollowUpAttackGA : GameAction, IHaveCaster
{
    public const int BaseDamage = 4;

    public CombatantView Caster { get; private set; }
    public List<CombatantView> Targets { get; private set; }
    public int FollowUpDamageBonus { get; private set; }
    public int Damage => BaseDamage + FollowUpDamageBonus;
    public bool IsTriggeredByMark { get; private set; }

    public static FollowUpAttackGA CreateFromMark(CombatantView target, CombatantView applier)
    {
        // Mark pays its own stack cost and fixes the target. DamageSystem reads the applier's
        // current MarkFollowUpDamageUp bonus when this reaction executes (base damage is four).
        return new FollowUpAttackGA(new List<CombatantView> { target }, applier)
        {
            IsTriggeredByMark = true
        };
    }

    public FollowUpAttackGA(List<CombatantView> targets, CombatantView caster, int followUpDamageBonus = 0)
    {
        Targets = targets != null ? new List<CombatantView>(targets) : new List<CombatantView>();
        Caster = caster;
        // Only a specifically configured Follow-Up effect supplies this per-action bonus.
        FollowUpDamageBonus = Mathf.Clamp(followUpDamageBonus, 0, int.MaxValue - BaseDamage);
    }
}
