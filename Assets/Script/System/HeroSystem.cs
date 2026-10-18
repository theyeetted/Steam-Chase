
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class HeroSystem : SingleTon<HeroSystem>
{
    [field: SerializeField] 
    public PlayerView playerView {  get; private set; }

    [field: SerializeField]
    public PlayerView secondPlayerView { get; private set; }

    public CombatAttackHistory AttackHistory { get; } = new();
    public ScheduledEffects ScheduledEffects { get; } = new();

    public void ResetCombatState()
    {
        AttackHistory.Clear();
        ScheduledEffects.Clear();
    }

    public void Setup(CharData charData)
    {
        ResetCombatState();
        playerView.Setup(charData);
    }

    public void Setup(CharData firstCharData, CharData secondCharData)
    {
        ResetCombatState();
        playerView.Setup(firstCharData);
        secondPlayerView.Setup(secondCharData);
    }

    public PlayerView GetHero(int heroIndex)
    {
        return heroIndex == 0 ? playerView : secondPlayerView;
    }

    public bool TryGetHeroIndex(CombatantView hero, out int heroIndex)
    {
        heroIndex = -1;
        if (hero == null)
            return false;
        if (hero == playerView)
            heroIndex = 0;
        else if (hero == secondPlayerView)
            heroIndex = 1;
        return heroIndex >= 0;
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<EndPlayerPhaseGA>(EndPlayerPhasePerformer);
        ActionSystem.AttachPerformer<SchedulePlayerPhaseEndGA>(SchedulePlayerPhaseEndPerformer);
        ActionSystem.AttachPerformer<StartPlayerPhaseGA>(StartPlayerPhasePerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<EndPlayerPhaseGA>();
        ActionSystem.DetachPerformer<SchedulePlayerPhaseEndGA>();
        ActionSystem.DetachPerformer<StartPlayerPhaseGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    private IEnumerator EndPlayerPhasePerformer(EndPlayerPhaseGA action)
    {
        if (action.Delivered)
            yield break;
        action.Delivered = true;
        ScheduledEffects.QueuePlayerPhaseEnd();
        yield break;
    }

    private IEnumerator SchedulePlayerPhaseEndPerformer(SchedulePlayerPhaseEndGA action)
    {
        if (action.Owner == null || action.Owner.CurrentHealth <= 0 ||
            !TryGetHeroIndex(action.Owner, out _))
            yield break;
        // Preserve trigger provenance across the delay, just like scheduled draw rules.
        object triggerSource = null;
        for (GameAction ancestor = action; ancestor != null; ancestor = ancestor.ParentAction)
        {
            if (ancestor.TriggerSource == null)
                continue;
            triggerSource = ancestor.TriggerSource;
            break;
        }
        ScheduledEffects.ScheduleForPlayerPhaseEnd(action.Payload, action.Owner, triggerSource, action.Lifetime);
        yield break;
    }

    private IEnumerator StartPlayerPhasePerformer(StartPlayerPhaseGA action)
    {
        if (action.Advanced)
            yield break;
        action.Advanced = true;
        for (int i = 0; i < 2; i++)
        {
            PlayerView hero = GetHero(i);
            if (hero == null)
                continue;
            // One authority for Counter expiry, independent of POST subscription order.
            if (hero.HasTimedCounter)
                hero.AdvanceCounterTurn();
            hero.AdvanceTriggeredRulePhase();
        }
        ScheduledEffects.QueueNextPlayerPhase();
        yield break;
    }

    //Reaction

    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnPreGA)
    {
        // Discard cards from both heroes' hands.
        ActionSystem.Instance.AddReaction(new DiscardAllCardsGA());
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnPostGA)
    {
        // Process SHOCK separately for each hero.
        for (int i = 0; i < 2; i++)
        {
            PlayerView hero = GetHero(i);

            if (hero == null || hero.CurrentHealth <= 0)
                continue;

            // A stunned hero recovers at the end of the enemy turn.
            if (hero.IsStunned)
            {
                hero.ConsumeStun();
                continue;
            }

            // End cooldown after the hero has spent one enemy turn
            // recovering from stun.
            if (hero.IsStunOnCooldown)
            {
                hero.EndStunCooldown();
            }

            // Process SHOCK.
            int shockStacks =
                hero.GetStatusEffectStacks(StatusEffectType.SHOCK);

            if (shockStacks > 0)
            {
                ActionSystem.Instance.AddReaction(
                    new ApplyShockGA(shockStacks, hero)
                );
            }
        }

        // Draw five cards for each hero.
        ActionSystem.Instance.AddReaction(new DrawCardsGA(5, 0));
        ActionSystem.Instance.AddReaction(new DrawCardsGA(5, 1));
    }

}
