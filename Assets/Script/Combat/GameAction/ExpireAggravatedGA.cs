public class ExpireAggravatedGA : GameAction
{
    public EnemyView Target { get; private set; }

    public ExpireAggravatedGA(EnemyView target)
    {
        Target = target;
    }
}
