public class DecayCounterGA : GameAction
{
    public PlayerView Target { get; }

    public DecayCounterGA(PlayerView target)
    {
        Target = target;
    }
}
