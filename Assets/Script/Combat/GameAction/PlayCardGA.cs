using UnityEngine;

public class PlayCardGA : GameAction
{
    public EnemyView MannualTarget {  get; private set; }
    public Card Card { get; set; }
    public int HeroIndex { get; private set; }

    public PlayCardGA(Card card, int heroIndex = 0)
    {
        Card = card;
        MannualTarget = null;
        HeroIndex = heroIndex;
    }

    public PlayCardGA(Card card, EnemyView mannualTarget, int heroIndex = 0)
    {
        Card = card;
        MannualTarget = mannualTarget;
        HeroIndex = heroIndex;
    }

}
