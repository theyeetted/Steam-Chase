using UnityEngine;

public class PlayerView : CombatantView
{
    [SerializeField] private int heroIndex;

    public void Setup(CharData charData)
    {
        SetupBase(charData.Health, charData.Image, charData.MaxStun);
    }

    private void OnMouseDown()
    {
        Debug.Log($"[PlayerView] Clicked player: {name}, heroIndex: {heroIndex}");
        if (CardSystem.Instance != null) {
            CardSystem.Instance.SetActiveHero(heroIndex);
        }
        else {
            Debug.LogWarning("[PlayerView] CardSystem.Instance is null.");
        }
    }
}
