using System.Collections.Generic;

public class ExtraAttackGA : GameAction, IHaveCaster
{
    public const int BaseDamage = 4;

    public CombatantView Caster { get; private set; }
    public List<CombatantView> Targets { get; private set; }

    public ExtraAttackGA(List<CombatantView> targets, CombatantView caster)
    {
        Targets = targets != null ? new List<CombatantView>(targets) : new List<CombatantView>();
        Caster = caster;
    }
}
