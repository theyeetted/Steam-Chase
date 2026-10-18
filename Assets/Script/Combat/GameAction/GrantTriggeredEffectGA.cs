using System.Collections.Generic;

public sealed class GrantTriggeredEffectGA : GameAction
{
    public TriggeredEffectRule Rule { get; }
    public List<CombatantView> Targets { get; }

    public GrantTriggeredEffectGA(TriggeredEffectRule rule, List<CombatantView> targets)
    {
        Rule = rule;
        Targets = targets == null ? new() : new(targets);
    }
}
