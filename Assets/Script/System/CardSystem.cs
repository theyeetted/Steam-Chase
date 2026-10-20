using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSystem : SingleTon<CardSystem>
{
    private sealed class PlayedAttackCard
    {
        public Card Card { get; }
        public CombatantView Caster { get; }
        public CombatantView ManualTarget { get; }
        public List<List<CombatantView>> OtherEffectTargets { get; }

        public PlayedAttackCard(Card card, CombatantView caster, CombatantView manualTarget,
            List<List<CombatantView>> otherEffectTargets)
        {
            Card = card;
            Caster = caster;
            ManualTarget = manualTarget;
            OtherEffectTargets = otherEffectTargets;
        }
    }

    [Header("Card Positions")]
    [SerializeField] private HandView handView;
    [SerializeField] private Transform drawPilePoint;
    [SerializeField] private Transform discardPilePoint;

    private readonly List<Card>[] drawPiles = {
        new List<Card>(),
        new List<Card>()
    };

    private readonly List<Card>[] hands = {
        new List<Card>(),
        new List<Card>()
    };

    private readonly List<Card>[] discardPiles = {
        new List<Card>(),
        new List<Card>()
    };

    private int activeHeroIndex;
    private Coroutine displayHandCoroutine;
    private bool isDisplayingHand;
    private PlayedAttackCard firstAttackCardPlayedThisTurn;
    private bool repeatedThisTurn;
    private bool repeatFirstAttackEachTurn;

    public int ActiveHeroIndex => activeHeroIndex;
    public bool IsDisplayingHand => isDisplayingHand;

    // Read-only counts for the HUD; no active hero (-1) displays empty piles.
    public int GetDrawPileCount(int heroIndex)
    {
        return IsValidHero(heroIndex) ? drawPiles[heroIndex].Count : 0;
    }

    public int GetDiscardPileCount(int heroIndex)
    {
        return IsValidHero(heroIndex) ? discardPiles[heroIndex].Count : 0;
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DrawCardsGA>(DrawCardsPerformer);
        ActionSystem.AttachPerformer<DiscardAllCardsGA>(DiscardAllCardsPerformer);
        ActionSystem.AttachPerformer<PlayCardGA>(PlayCardPerformer);
        ActionSystem.AttachPerformer<RepeatFirstAttackCardGA>(RepeatFirstAttackCardPerformer);
        ActionSystem.AttachPerformer<PlayRandomAttackCardGA>(PlayRandomAttackCardPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(ResetAttackCardRecord, ReactionTiming.POST);

    }

    void OnDisable()
    {
        if (displayHandCoroutine != null)
        {
            StopCoroutine(displayHandCoroutine);
            displayHandCoroutine = null;
        }
        isDisplayingHand = false;

        ActionSystem.DetachPerformer<DrawCardsGA>();
        ActionSystem.DetachPerformer<DiscardAllCardsGA>();
        ActionSystem.DetachPerformer<PlayCardGA>();
        ActionSystem.DetachPerformer<RepeatFirstAttackCardGA>();
        ActionSystem.DetachPerformer<PlayRandomAttackCardGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(ResetAttackCardRecord, ReactionTiming.POST);

    }

    //Publics

    // Added
    public void Setup(List<CardData> deckData)
    {
        Setup(0, deckData);
    }

    public void Setup(int heroIndex, List<CardData> deckData)
    {
        if (!IsValidHero(heroIndex)) { return; }

        repeatFirstAttackEachTurn = false;
        ResetAttackCardRecord();
        ResetCards(heroIndex); // Reset card of that hero

        foreach (CardData cardData in deckData) // Add card of specific hero to draw pile
        {
            drawPiles[heroIndex].Add(new Card(cardData));
        }
    }


    public void SetActiveHero(int heroIndex)
    {
        if (!IsValidHero(heroIndex))
        {
            Debug.LogWarning($"[CardSystem] Invalid hero index: {heroIndex}. Switch cancelled.");
            return;
        }

        if (TurnSystem.Instance != null)
        {
            Debug.Log($"[CardSystem] Current phase: {TurnSystem.Instance.CurrentPhase}");

            var turn = TurnSystem.Instance;
            if (!turn.CanPlayerAct)
            {
                Debug.LogWarning("[CardSystem] Cannot switch heroes because it is not the player phase.");
                return;
            }

            if (!turn.CanHeroPlayCards(heroIndex))
            {
                Debug.LogWarning($"[CardSystem] Hero {heroIndex} cannot act. Checking the other hero...");

                int otherHeroIndex = 1 - heroIndex; // Select another hero

                Debug.LogWarning("[CardSystem] Neither hero can act. Clearing the active hand.");

                if (!TurnSystem.Instance.CanHeroPlayCards(otherHeroIndex))
                {
                    if (displayHandCoroutine != null)
                    {
                        StopCoroutine(displayHandCoroutine);
                        displayHandCoroutine = null;
                    }

                    isDisplayingHand = false;
                    activeHeroIndex = -1;
                    handView.ResetHand();
                    return;
                }
                Debug.Log($"[CardSystem] Switching request redirected to hero {otherHeroIndex}.");
                heroIndex = otherHeroIndex;
            }
        }

        if (displayHandCoroutine != null)
        {
            Debug.Log("[CardSystem] Stopping previous hand display coroutine.");
            StopCoroutine(displayHandCoroutine);
            displayHandCoroutine = null;
        }

        Debug.Log($"[CardSystem] Activating hero {heroIndex}. Hand size: {hands[heroIndex].Count}");
        activeHeroIndex = heroIndex;
        handView.ResetHand();
        isDisplayingHand = true;
        displayHandCoroutine = StartCoroutine(DisplayHand(heroIndex));
    }

    // Handles stopping the coroutine and resetting the hand in one place
    private void ResetActiveHero()
    {
        if (displayHandCoroutine != null)
        {
            StopCoroutine(displayHandCoroutine);
            displayHandCoroutine = null;
        }

        isDisplayingHand = false;
        handView.ResetHand();
    }


    private IEnumerator DisplayHand(int heroIndex)
    {
        foreach (Card card in hands[heroIndex])
        {
            CardView cardView = CardViewCreator.Instance.CreateCardView(
                card,
                drawPilePoint.position,
                drawPilePoint.rotation,
                heroIndex
            );

            yield return handView.AddCard(cardView);
        }
        Debug.Log("[CardSystem] Displaying hand...");

        isDisplayingHand = false;
        displayHandCoroutine = null;
    }

    public void HideActiveHand()
    {
        if (displayHandCoroutine != null)
        {
            StopCoroutine(displayHandCoroutine);
            displayHandCoroutine = null;
        }

        isDisplayingHand = false;
        activeHeroIndex = -1;
        handView.ResetHand();
        Debug.Log("[CardSystem] Hiding hand...");
    }

    //Performer
    private IEnumerator DrawCardsPerformer(DrawCardsGA drawcardsGA)
    {
        int heroIndex = drawcardsGA.HeroIndex;

        if (!IsValidHero(heroIndex)) { yield break; }

        for (int i = 0; i < drawcardsGA.Amount; i++)
        {
            if (drawPiles[heroIndex].Count == 0)
            {
                RefillDeck(heroIndex);
            }

            // There may be fewer cards available than the requested draw amount.
            if (drawPiles[heroIndex].Count == 0) { yield break; }

            yield return DrawCard(heroIndex);
        }
    }

    private IEnumerator DiscardAllCardsPerformer(DiscardAllCardsGA discardAllCardsGA)
    {
        for (int heroIndex = 0; heroIndex < hands.Length; heroIndex++)
        {
            List<Card> hand = hands[heroIndex];

            foreach (Card card in hand)
            {
                CardView cardView = null;
                if (heroIndex == activeHeroIndex) { cardView = handView.RemoveCard(card); }

                if (cardView != null)
                {
                    yield return DiscardCard(heroIndex, cardView);
                }
                else
                {
                    discardPiles[heroIndex].Add(card); // Hidden cards must still be discarded.
                }
            }

            hand.Clear();
        }
    }

    private IEnumerator PlayCardPerformer(PlayCardGA playCardGA)
    {
        int heroIndex = playCardGA.HeroIndex;

        if (!IsValidHero(heroIndex) || playCardGA.Card == null) { yield break; }

        // DisplayHand iterates this hero's hand while cards are animating into view.
        // Reject plays until the animation is complete so removing a card cannot
        // invalidate that enumeration.
        if (isDisplayingHand)
        {
            Debug.Log("Cannot play a card while the hand is being displayed.");
            yield break;
        }

        // A hidden hero cannot play a card.
        if (heroIndex != activeHeroIndex)
        {
            yield break;
        }

        if (TurnSystem.Instance == null || !TurnSystem.Instance.CanHeroPlayCards(heroIndex))
        {
            Debug.Log($"Hero {heroIndex} cannot play cards right now.");
            yield break;
        }

        // Revalidate before paying or moving the card, including plays created outside CardView.
        // A manual healing card accepts heroes, while old cards remain enemy-only by default.
        if (playCardGA.Card.MannualTargetEffect != null &&
            !ManualTargetRules.IsValid(playCardGA.MannualTarget, playCardGA.Card.ManualTargetKind))
            yield break;

        if (EnergySystem.Instance == null ||
            !EnergySystem.Instance.HasEnoughEnergy(playCardGA.Card.Cost, heroIndex))
            yield break;

        if (!hands[heroIndex].Remove(playCardGA.Card))
        {
            yield break;
        }

        yield return MovePlayedCard(heroIndex, playCardGA.Card);

        ActionSystem.Instance.AddReaction(
            new SpendEnergyGA(playCardGA.Card.Cost, heroIndex)
        );

        CombatantView caster = HeroSystem.Instance.GetHero(heroIndex);
        List<List<CombatantView>> otherEffectTargets = ResolveOtherEffectTargets(
            playCardGA.Card, caster, playCardGA.MannualTarget);

        QueuePlayedCardEffects(playCardGA.Card, caster, playCardGA.MannualTarget, otherEffectTargets);

    }

    private IEnumerator PlayRandomAttackCardPerformer(PlayRandomAttackCardGA action)
    {
        // A selected card can contain compositions/replay effects. It must not
        // recursively execute more random cards from this same action tree.
        for (GameAction ancestor = action.ParentAction; ancestor != null; ancestor = ancestor.ParentAction)
        {
            if (ancestor is PlayRandomAttackCardGA)
                yield break;
        }

        HeroSystem heroes = HeroSystem.Instance;
        if (heroes == null || action.Owner == null || action.Owner.CurrentHealth <= 0 ||
            !heroes.TryGetHeroIndex(action.Owner, out int heroIndex) || !IsValidHero(heroIndex))
            yield break;

        // Finish a hand-switch animation before changing the list it enumerates.
        if (heroIndex == activeHeroIndex && displayHandCoroutine != null)
            yield return displayHandCoroutine;

        if (action.Owner == null || action.Owner.CurrentHealth <= 0)
            yield break;
        List<CombatantView> enemies = new AllEnemiesTM().GetTargets(action.Owner);
        if (enemies.Count == 0)
            yield break;

        List<Card> candidates = new();
        foreach (Card candidate in hands[heroIndex])
        {
            // Enemy-directed auto play cannot supply a manual hero target.
            if (candidate != null && candidate.Type == CardType.Attack &&
                (candidate.MannualTargetEffect == null || candidate.ManualTargetKind == ManualTargetKind.Enemy))
                candidates.Add(candidate);
        }
        if (candidates.Count == 0)
            yield break;

        Card card = candidates[Random.Range(0, candidates.Count)];
        CombatantView target = enemies[Random.Range(0, enemies.Count)];
        List<List<CombatantView>> otherTargets = ResolveOtherEffectTargets(card, action.Owner, target);
        if (!hands[heroIndex].Remove(card))
            yield break;

        // Use the normal destination rules once, including hidden hands and
        // Disintegrate. This is effect execution, not a paid/manual card play.
        yield return MovePlayedCard(heroIndex, card);
        QueuePlayedCardEffects(card, action.Owner, target, otherTargets);
    }

    private IEnumerator RepeatFirstAttackCardPerformer(RepeatFirstAttackCardGA action)
    {
        repeatFirstAttackEachTurn = true;
        if (repeatedThisTurn || firstAttackCardPlayedThisTurn == null ||
            firstAttackCardPlayedThisTurn.Card == null ||
            firstAttackCardPlayedThisTurn.Card.Type != CardType.Attack)
            yield break;

        repeatedThisTurn = true;
        QueueCardEffects(
            firstAttackCardPlayedThisTurn.Card,
            firstAttackCardPlayedThisTurn.Caster,
            firstAttackCardPlayedThisTurn.ManualTarget,
            firstAttackCardPlayedThisTurn.OtherEffectTargets);
        yield break;
    }

    private void QueuePlayedCardEffects(Card card, CombatantView caster, CombatantView manualTarget,
        List<List<CombatantView>> otherEffectTargets)
    {
        bool repeatAttack = false;
        if (card.Type == CardType.Attack && firstAttackCardPlayedThisTurn == null)
        {
            firstAttackCardPlayedThisTurn = new PlayedAttackCard(
                card, caster, manualTarget, otherEffectTargets);
            repeatAttack = repeatFirstAttackEachTurn && !repeatedThisTurn;
            if (repeatAttack)
                repeatedThisTurn = true;
        }

        QueueCardEffects(card, caster, manualTarget, otherEffectTargets);
        if (repeatAttack)
            QueueCardEffects(card, caster, manualTarget, otherEffectTargets);
    }

    private List<List<CombatantView>> ResolveOtherEffectTargets(Card card,
        CombatantView caster, CombatantView manualTarget)
    {
        List<List<CombatantView>> resolvedTargets = new();
        if (card.OtherEffects == null)
            return resolvedTargets;

        foreach (AutoTargetEffect effectWrapper in card.OtherEffects)
        {
            if (effectWrapper == null || effectWrapper.TargetMode == null || effectWrapper.Effect == null)
            {
                resolvedTargets.Add(new());
                continue;
            }

            List<CombatantView> targets = effectWrapper.TargetMode.GetTargets(caster, manualTarget);
            resolvedTargets.Add(targets == null ? new() : new(targets));
        }

        return resolvedTargets;
    }

    private void QueueCardEffects(Card card, CombatantView caster, CombatantView manualTarget,
        List<List<CombatantView>> otherEffectTargets)
    {
        if (card.MannualTargetEffect != null)
        {
            ActionSystem.Instance.AddReaction(new PerformEffectGA(
                card.MannualTargetEffect,
                new List<CombatantView> { manualTarget },
                caster,
                manualTarget));
        }

        if (card.OtherEffects == null)
            return;

        for (int i = 0; i < card.OtherEffects.Count; i++)
        {
            AutoTargetEffect effectWrapper = card.OtherEffects[i];
            if (effectWrapper == null || effectWrapper.TargetMode == null || effectWrapper.Effect == null)
                continue;

            List<CombatantView> targets = i < otherEffectTargets.Count && otherEffectTargets[i] != null
                ? new(otherEffectTargets[i])
                : new();
            EffectContext context = new(targets, caster, manualTarget);
            ActionSystem.Instance.AddReaction(new RepeatAutoTargetEffectGA(effectWrapper, context));
        }
    }

    private void ResetAttackCardRecord(EnemyTurnGA action = null)
    {
        firstAttackCardPlayedThisTurn = null;
        repeatedThisTurn = false;
    }


    //Helper

    private IEnumerator MovePlayedCard(int heroIndex, Card card)
    {
        CardView cardView = heroIndex == activeHeroIndex ? handView.RemoveCard(card) : null;
        if (card.Disintegrate)
        {
            // Keep this runtime copy out of discard before its effects can draw cards.
            if (cardView != null)
                yield return DisintegrateCard(cardView);
        }
        else if (cardView != null)
        {
            yield return DiscardCard(heroIndex, cardView);
        }
        else
        {
            discardPiles[heroIndex].Add(card);
        }
    }

    private IEnumerator DrawCard(int heroIndex)
    {
        Card card = drawPiles[heroIndex].Draw();

        if (card == null) { yield break; }

        hands[heroIndex].Add(card);

        // Only create visible card objects for the selected hero.
        if (heroIndex != activeHeroIndex) { yield break; }

        CardView cardView = CardViewCreator.Instance.CreateCardView(
                    card,
                    drawPilePoint.position,
                    drawPilePoint.rotation,
                    heroIndex
                );

        Debug.Log("[CardSystem] Drawing cards...");

        yield return handView.AddCard(cardView);
    }

    private void RefillDeck(int heroIndex)
    {
        drawPiles[heroIndex].AddRange(discardPiles[heroIndex]);
        discardPiles[heroIndex].Clear();
        Debug.Log("[CardSystem] Refilling deck...");
    }

    private IEnumerator DisintegrateCard(CardView cardView)
    {
        Tween tween = cardView.transform.DOScale(Vector3.zero, 0.15f);
        yield return tween.WaitForCompletion();
        Destroy(cardView.gameObject);
    }

    private IEnumerator DiscardCard(int heroIndex, CardView cardView)
    {
        discardPiles[heroIndex].Add(cardView.Card);

        Debug.Log("[CardSystem] Discarding card...");

        cardView.transform.DOScale(Vector3.zero, 0.15f);
        Tween tween = cardView.transform.DOMove(discardPilePoint.position, 0.15f);
        yield return tween.WaitForCompletion();
        Destroy(cardView.gameObject);
    }

    // Reseting cards
    public void ResetCards()
    {
        for (int i = 0; i < hands.Length; i++)
            ResetCards(i);

        Debug.Log("[CardSystem] Resetting cards...");
    }

    public void ResetCards(int heroIndex)
    {
        if (!IsValidHero(heroIndex)) { return; }

        if (HeroSystem.Instance != null)
            HeroSystem.Instance.ScheduledEffects.CancelPlayerPhaseEnd(HeroSystem.Instance.GetHero(heroIndex));

        if (heroIndex == activeHeroIndex)
        {
            if (displayHandCoroutine != null)
            {
                StopCoroutine(displayHandCoroutine);
                displayHandCoroutine = null;
            }
            handView.ResetHand();
        }
        Debug.Log("[CardSystem] Resetting specific character cards...");

        drawPiles[heroIndex].Clear();
        hands[heroIndex].Clear();
        discardPiles[heroIndex].Clear();
    }

    private bool IsValidHero(int heroIndex)
    {
        return heroIndex >= 0 && heroIndex < hands.Length;
    }
}
