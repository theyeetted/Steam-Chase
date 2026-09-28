
using UnityEngine;
using UnityEngine.Rendering;

public class HeroSystem : SingleTon<HeroSystem>
{
    [field: SerializeField] public PlayerView playerView {  get; private set; }

    public void Setup(CharData charData)
    {
        playerView.Setup(charData);
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
        DiscardAllCardsGA discardAllCardsGA = new();
        ActionSystem.Instance.AddReaction(discardAllCardsGA);
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnPostGA)
    {
        int shockStack = playerView.GetStatusEffectStacks(StatusEffectType.SHOCK);
        if (shockStack > 0)
        {
            ApplyShockGA applyShockGA = new(shockStack, playerView);
            ActionSystem.Instance.AddReaction(applyShockGA);
        }
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.AddReaction(drawCardsGA);
    }

}
