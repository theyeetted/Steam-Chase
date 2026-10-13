using UnityEngine;

public class SpendEnergyGA : GameAction
{
    public int Amount { get; private set; }
    public int HeroIndex { get; private set; }

    public SpendEnergyGA(int amount, int heroIndex = 0)
    {
        Amount = amount;
        HeroIndex = heroIndex;
    }
}
