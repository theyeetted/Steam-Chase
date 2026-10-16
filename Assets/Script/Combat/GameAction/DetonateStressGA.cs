public class DetonateStressGA : GameAction
{
    public EnemyView Target { get; private set; }

    public DetonateStressGA(EnemyView target)
    {
        Target = target;
    }
}
