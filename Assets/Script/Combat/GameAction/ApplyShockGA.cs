public class ApplyShockGA : GameAction
{
    public int ShockDamage {  get; private set; }
    public int ShockStacks { get; private set; }
    public CombatantView Target { get; private set; }
    public bool UseCurrentStacks { get; private set; }
    public StatusTriggerStackMode TriggerStacks { get; private set; }
    public StatusStackConsumption ConsumeStacks { get; private set; } = StatusStackConsumption.One;

    public ApplyShockGA(int shockStacks, CombatantView target)
    {
        ShockStacks = shockStacks;
        Target = target;
    }

    // Card triggers read their damage and consumption from the stacks present at execution.
    public ApplyShockGA(CombatantView target, StatusTriggerStackMode triggerStacks,
        StatusStackConsumption consumeStacks)
    {
        Target = target;
        UseCurrentStacks = true;
        TriggerStacks = triggerStacks;
        ConsumeStacks = consumeStacks;
    }
}
