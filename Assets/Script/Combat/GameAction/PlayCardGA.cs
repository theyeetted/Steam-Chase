using UnityEngine;

public class PlayCardGA : GameAction
{
    public EnemyView MannualTarget {  get; private set; }
    public Card Card {  get; set; }
    public PlayCardGA(Card card)
    {
        Card = card;
        MannualTarget = null;
    }

    public PlayCardGA(Card card, EnemyView mannualTarget)
    {
        Card = card;
        MannualTarget = mannualTarget;
    }

}
