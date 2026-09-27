using UnityEngine;

public class DrawCardsGA : GameAction
{
    public int Amount { get; set; }

    public DrawCardsGA(int numberOfCards)
    {
        Amount = numberOfCards;
    }
}
