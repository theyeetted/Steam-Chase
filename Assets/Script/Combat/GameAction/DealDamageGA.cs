using System.Collections.Generic;
using UnityEngine;

public class DealDamageGA : GameAction, IHaveCaster
{
    public int Amount { get; set; }
    public List<CombatantView> Targets { get; set; }

    public CombatantView Caster { get; private set; }
    public AttackKind Kind { get; }
    public EffectValue AmountSource { get; }
    public EffectContext ValueContext { get; }
    public int AdditionalStunFillPercent { get; }

    public DealDamageGA(int amount, List<CombatantView> targets, CombatantView caster,
        AttackKind kind = AttackKind.Normal, EffectValue amountSource = null, EffectContext valueContext = null,
        int additionalStunFillPercent = 0)
    {
        Amount = amount;
        Targets = targets == null ? new() : new(targets);
        Caster = caster;
        Kind = kind;
        AmountSource = amountSource;
        ValueContext = valueContext ?? new EffectContext(Targets, caster);
        AdditionalStunFillPercent = Mathf.Clamp(additionalStunFillPercent, 0, 100);
    }
}
