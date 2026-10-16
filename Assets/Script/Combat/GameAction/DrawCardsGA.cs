using UnityEngine;

public class DrawCardsGA : GameAction
{
    // Old ver: public int Amount { get; set; }
    public int Amount { get; private set; } 
    public int HeroIndex { get; private set; }

    public DrawCardsGA(int numberOfCards, int heroIndex = 0)
    {
        Amount = numberOfCards;
        HeroIndex = heroIndex;
    }
}
