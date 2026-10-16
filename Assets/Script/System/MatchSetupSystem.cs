using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class MatchSetupSystem : MonoBehaviour
{
    [Header("Heroes")]
    [SerializeField] private CharData firstCharData;
    [SerializeField] private CharData secondCharData;

    [Header("Match")]
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
        HeroSystem.Instance.Setup(firstCharData, secondCharData);
        EnemySystem.Instance.Setup(enemyDatas);
        
        // Independent deck
        CardSystem.Instance.Setup(0, firstCharData.Deck);
        CardSystem.Instance.Setup(1, secondCharData.Deck);

        // Do we need seperated perk for each char?
        PerkSystem.Instance.AddPerk(new Perk(PerkData));
        DrawCardsGA drawCardsGA = new(5);
        RefillEnergyGA refillEnergyGA = new();
        // Perform rejects overlapping actions, so refill after the opening draw.
        ActionSystem.Instance.Perform(drawCardsGA, () => ActionSystem.Instance.Perform(refillEnergyGA));

        DrawOpeningHands();
    }


    // Added --------------------

    private void DrawOpeningHands()
    {
        ActionSystem.Instance.Perform(new DrawCardsGA(5, 0), () =>
        {
            ActionSystem.Instance.Perform(new DrawCardsGA(5, 1), () =>
            {
                // Ensure both energy counters are full.
                EnergySystem.Instance.ResetEnergy();
            });
        });
    }


    public void RestartFight()
    {
        StartCoroutine(RestartFightRoutine());
    }

    private IEnumerator RestartFightRoutine()
    {
        // Wait until the current action finishes.
        while (ActionSystem.Instance.IsPerformingAction)
        {
            yield return null;
        }

        // Reset game over state
        FindFirstObjectByType<DamageSystem>()?.ResetGameOverState();

        // Restore both heroes.
        HeroSystem.Instance.playerView.ResetCombatant();
        HeroSystem.Instance.secondPlayerView.ResetCombatant();

        // Rebuild the enemy
        yield return EnemySystem.Instance.ResetEnemies(enemyDatas);

        // Restore both decks and energy counters
        CardSystem.Instance.Setup(0, firstCharData.Deck);
        CardSystem.Instance.Setup(1, secondCharData.Deck);
        EnergySystem.Instance.ResetEnergy();

        // Draw five cards for each hero.
        DrawOpeningHands();
    }
}
