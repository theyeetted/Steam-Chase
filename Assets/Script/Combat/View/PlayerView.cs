using UnityEngine;

public class PlayerView : CombatantView
{
    [SerializeField] private int heroIndex;

    public void Setup(CharData charData)
    {
        SetupBase(charData.Health, charData.Image);
    }

    private void OnMouseDown()
    {
        CardSystem.Instance.SetActiveHero(heroIndex);
    }
}
