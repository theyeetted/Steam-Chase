using System.Collections.Generic;

public class HealGA : GameAction, IHaveCaster
{
    public int Amount { get; }
    public List<CombatantView> Targets { get; }
    public CombatantView Caster { get; }

    public HealGA(int amount, List<CombatantView> targets, CombatantView caster)
    {
        Amount = amount;
        Targets = targets == null ? new() : new(targets);
        Caster = caster;
    }
}
