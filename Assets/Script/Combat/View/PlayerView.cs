using UnityEngine;

public class PlayerView : CombatantView
{
    public void Setup(CharData charData)
    {
        SetupBase(charData.Health, charData.Image);
    }
}
