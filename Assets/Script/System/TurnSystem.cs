using UnityEngine;
using System.Collections;

public class TurnSystem : SingleTon<TurnSystem>
{
    public enum TurnPhase { Player, Enemy }

    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Player;
    public bool CanPlayerAct => CurrentPhase == TurnPhase.Player;

    private readonly bool[] heroMustSkipTurn = new bool[2];

    private void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(
            EnemyTurnPostReaction,
            ReactionTiming.POST
        );
    }

    private void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(
            EnemyTurnPostReaction,
            ReactionTiming.POST
        );
    }

    public bool CanHeroPlayCards(int heroIndex)
    {
        if (!CanPlayerAct || heroIndex < 0 || heroIndex >= 2)
            return false;

        PlayerView hero = HeroSystem.Instance.GetHero(heroIndex);

        return hero != null
            && hero.CurrentHealth > 0
            && !hero.IsStunned
            && !heroMustSkipTurn[heroIndex];
    }

    // Connect this method to End Turn button
    public void EndPlayerTurn()
    {
        if (CurrentPhase != TurnPhase.Player)
            return;

        for (int i = 0; i < 2; i++)
        {
            PlayerView hero = HeroSystem.Instance.GetHero(i);

            if (hero == null)
                continue;

            // The hero's skipped turn has now finished.
            if (hero.IsStunned)
                hero.ConsumeStun();
            else if (hero.IsStunOnCooldown)
                hero.EndStunCooldown();

            heroMustSkipTurn[i] = false;
        }

        CurrentPhase = TurnPhase.Enemy;
        Debug.Log("[TurnSystem] Enemy turn started.");

        ActionSystem.Instance.AddReaction(new EnemyTurnGA());
    }

    private void EnemyTurnPostReaction(EnemyTurnGA action)
    {
        // Enemy actions and their queued reactions have finished.
        // Check whether either hero must skip the new player turn.
        for (int i = 0; i < 2; i++)
        {
            PlayerView hero = HeroSystem.Instance.GetHero(i);

            heroMustSkipTurn[i] =
                hero != null && hero.CurrentHealth > 0 && hero.IsStunned;
        }

        CurrentPhase = TurnPhase.Player;

        Debug.Log("[TurnSystem] Player turn started.");

        StartCoroutine(SwitchToAvailableHero());
    }

    private IEnumerator SwitchToAvailableHero()
    {
        // Wait until the current frame has finished processing.
        yield return null;

        int activeHeroIndex = -1;

        for (int i = 0; i < 2; i++)
        {
            if (CanHeroPlayCards(i)) {
                activeHeroIndex = i;
                break;
            }
        }

        if (activeHeroIndex != -1) {
            CardSystem.Instance.SetActiveHero(activeHeroIndex);
            Debug.Log($"[TurnSystem] Switched to hero {activeHeroIndex}.");
        } else {
            CardSystem.Instance.HideActiveHand();
            Debug.LogWarning("[TurnSystem] Neither hero can act. Both may be stunned or defeated.");
        }
    }
}