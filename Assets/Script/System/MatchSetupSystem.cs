using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private CharData charData;
    [SerializeField] private PerkData PerkData;
    [SerializeField] private List<EnemyData> enemyDatas;

    private void Start()
    {
        HeroSystem.Instance.Setup(charData);
        EnemySystem.Instance.Setup(enemyDatas);
        CardSystem.Instance.Setup(charData.Deck);
        PerkSystem.Instance.AddPerk(new Perk(PerkData));
        DrawCardsGA drawCardsGA = new(5);
        RefillEnergyGA refillEnergyGA = new();
        ActionSystem.Instance.Perform(drawCardsGA);
        ActionSystem.Instance.Perform(refillEnergyGA);
    }
}
