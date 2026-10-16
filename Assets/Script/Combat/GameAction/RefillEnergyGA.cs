using UnityEngine;

public class RefillEnergyGA : GameAction
{
    public int HeroIndex { get; private set; }

    public RefillEnergyGA(int heroIndex = 0)
    {
        HeroIndex = heroIndex;
    }
}
