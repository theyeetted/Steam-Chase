public class DecayAttackUpGA : GameAction
{
    public CombatantView Target { get; private set; }

    public DecayAttackUpGA(CombatantView target)
    {
        Target = target;
    }
}
