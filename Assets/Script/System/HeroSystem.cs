
using UnityEngine;
using UnityEngine.Rendering;

public class HeroSystem : SingleTon<HeroSystem>
{
    [field: SerializeField] 
    public PlayerView playerView {  get; private set; }

    [field: SerializeField]
    public PlayerView secondPlayerView { get; private set; }

    public void Setup(CharData charData)
    {
        playerView.Setup(charData);
    }

    public void Setup(CharData firstCharData, CharData secondCharData)
    {
        playerView.Setup(firstCharData);
        secondPlayerView.Setup(secondCharData);
    }

    public PlayerView GetHero(int heroIndex)
    {
        return heroIndex == 0 ? playerView : secondPlayerView;
    }

    void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
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
