public class DecayAttackDownGA : GameAction
{
    public CombatantView Target { get; private set; }

    public DecayAttackDownGA(CombatantView target)
    {
        Target = target;
    }
}
