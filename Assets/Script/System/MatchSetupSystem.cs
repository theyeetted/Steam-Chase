using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private CharData charData;
    [SerializeField] private PerkData PerkData;
    [SerializeField] private List<EnemyData> enemyDatas;

    private IEnumerator Start()
    {
        // Leave the board and deck untouched until the start menu is dismissed.
        while (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            yield return null;
        }

        HeroSystem.Instance.Setup(charData);
        EnemySystem.Instance.Setup(enemyDatas);
        CardSystem.Instance.Setup(charData.Deck);
        PerkSystem.Instance.AddPerk(new Perk(PerkData));
        DrawCardsGA drawCardsGA = new(5);
        RefillEnergyGA refillEnergyGA = new();
        // Perform rejects overlapping actions, so refill after the opening draw.
        ActionSystem.Instance.Perform(drawCardsGA, () => ActionSystem.Instance.Perform(refillEnergyGA));
    }
}
