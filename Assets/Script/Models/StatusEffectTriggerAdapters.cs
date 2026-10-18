/// <summary>
/// Compatibility for authored status-based cards. Stacks remain authoritative;
/// adapters only translate the current value into the shared trigger/payload model.
/// New trigger/reward combinations can use GrantTriggeredEffect without new statuses.
/// </summary>
public static class StatusEffectTriggerAdapters
{
    public static TriggeredEffectRule Create(StatusEffectType type, int stacks)
    {
        if (type != StatusEffectType.DrawOnVulnerable || stacks <= 0)
            return null;

        return new TriggeredEffectRule(
            new StatusAppliedTrigger(StatusEffectType.VULNERABLE, true, StatusAppliedTrigger.RecipientScope.Enemy),
            new SelfTM(),
            new DrawCardsEffect(stacks, DrawCardsEffect.Recipient.Caster));
    }

    public static bool Supports(StatusEffectType type)
    {
        return type == StatusEffectType.DrawOnVulnerable;
    }
}
