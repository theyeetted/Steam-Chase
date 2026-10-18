using System.Collections.Generic;

public class DetonateStressGA : GameAction
{
    public EnemyView Target { get; private set; }
    public bool RequiresStun { get; private set; } = true;
    public StatusTriggerStackMode TriggerStacks { get; private set; } = StatusTriggerStackMode.All;
    public StatusStackConsumption ConsumeStacks { get; private set; } = StatusStackConsumption.All;
    public List<DamageResult> Results { get; }

    public DetonateStressGA(EnemyView target)
    {
        Target = target;
    }

    // An explicit card trigger works even before the enemy is stunned.
    public DetonateStressGA(EnemyView target, StatusTriggerStackMode triggerStacks,
        StatusStackConsumption consumeStacks, List<DamageResult> results = null)
    {
        Target = target;
        RequiresStun = false;
        TriggerStacks = triggerStacks;
        ConsumeStacks = consumeStacks;
        Results = results;
    }
}
