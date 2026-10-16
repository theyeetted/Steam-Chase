using System.Collections.Generic;

public class TriggerShockGA : GameAction
{
    public List<CombatantView> Targets { get; private set; }

    public TriggerShockGA(List<CombatantView> targets)
    {
        Targets = targets == null ? new() : new(targets);
    }
}
