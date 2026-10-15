public class DecayStatusDamageBuffsGA : GameAction
{
    public CombatantView Target { get; }

    public DecayStatusDamageBuffsGA(CombatantView target)
    {
        Target = target;
    }
}
