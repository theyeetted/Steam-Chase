using System.Collections.Generic;

public class TriggerStatusGA : GameAction
{
    public StatusEffectType StatusEffectType { get; private set; }
    public List<CombatantView> Targets { get; private set; }
    public StatusTriggerStackMode TriggerStacks { get; private set; }
    public StatusStackConsumption ConsumeStacks { get; private set; }
    public EffectResults ResultStore { get; }
    public string ResultKey { get; }

    public TriggerStatusGA(StatusEffectType statusEffectType, List<CombatantView> targets,
        StatusTriggerStackMode triggerStacks = StatusTriggerStackMode.All,
        StatusStackConsumption consumeStacks = StatusStackConsumption.One,
        EffectResults resultStore = null, string resultKey = null)
    {
        StatusEffectType = statusEffectType;
        Targets = targets == null ? new() : new(targets);
        TriggerStacks = triggerStacks;
        ConsumeStacks = consumeStacks;
        ResultStore = resultStore ?? new EffectResults();
        ResultKey = resultKey;
    }
}
