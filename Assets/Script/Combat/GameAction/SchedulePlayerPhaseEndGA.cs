public sealed class SchedulePlayerPhaseEndGA : GameAction
{
    public Effect Payload { get; }
    public CombatantView Owner { get; }
    public SchedulePlayerPhaseEndEffect.LifetimeMode Lifetime { get; }

    public SchedulePlayerPhaseEndGA(Effect payload, CombatantView owner,
        SchedulePlayerPhaseEndEffect.LifetimeMode lifetime = SchedulePlayerPhaseEndEffect.LifetimeMode.ThisCombat)
    {
        Payload = payload;
        Owner = owner;
        Lifetime = lifetime;
    }
}
