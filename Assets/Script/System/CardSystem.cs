using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSystem : SingleTon<CardSystem>
{
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

    public int ActiveHeroIndex => activeHeroIndex;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DrawCardsGA>(DrawCardsPerformer);
        ActionSystem.AttachPerformer<DiscardAllCardsGA>(DiscardAllCardsPerformer);
        ActionSystem.AttachPerformer<PlayCardGA>(PlayCardPerformer);

    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DrawCardsGA>();
        ActionSystem.DetachPerformer<DiscardAllCardsGA>();
        ActionSystem.DetachPerformer<PlayCardGA>();
        
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

        ResetCards(heroIndex); // Reset card of that hero

        foreach (CardData cardData in deckData) // Add card of specific hero to draw pile
        {
            drawPiles[heroIndex].Add(new Card(cardData));
        }
    }

    
    public void SetActiveHero(int heroIndex)
    {
        if (!IsValidHero(heroIndex)) {
            Debug.LogWarning($"[CardSystem] Invalid hero index: {heroIndex}. Switch cancelled.");
            return;
        }

        if (TurnSystem.Instance != null)
        {
            Debug.Log($"[CardSystem] Current phase: {TurnSystem.Instance.CurrentPhase}");

            var turn = TurnSystem.Instance;
            if (!turn.CanPlayerAct) {
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

        displayHandCoroutine = null;
    }

    public void HideActiveHand()
    {
        if (displayHandCoroutine != null)
        {
            StopCoroutine(displayHandCoroutine);
            displayHandCoroutine = null;
        }

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

                if (cardView != null) {
                    yield return DiscardCard(heroIndex, cardView);
                } else { 
                    discardPiles[heroIndex].Add(card); // Hidden cards must still be discarded.
                }
            }

            hand.Clear();
        }
    }

    private IEnumerator PlayCardPerformer(PlayCardGA playCardGA)
    {
        int heroIndex = playCardGA.HeroIndex;

        if (!IsValidHero(heroIndex)) { yield break; }

        // A hidden hero cannot play a card.
        if (heroIndex != activeHeroIndex) {
            yield break;
        }

        if (TurnSystem.Instance == null || !TurnSystem.Instance.CanHeroPlayCards(heroIndex))
        {
            Debug.Log($"Hero {heroIndex} cannot play cards right now.");
            yield break;
        }

        if (!hands[heroIndex].Remove(playCardGA.Card)) {
            yield break;
        }

        // discardPile.Add(playCardGA.Card);
        CardView cardView = handView.RemoveCard(playCardGA.Card);
        
        if (cardView != null) {
            yield return DiscardCard(heroIndex, cardView);
        } else {
            discardPiles[heroIndex].Add(playCardGA.Card);
        }

        ActionSystem.Instance.AddReaction(
            new SpendEnergyGA(playCardGA.Card.Cost, heroIndex)
        );

        if (playCardGA.Card.MannualTargetEffect != null)
        {
            PerformEffectGA effectAction = new(
                playCardGA.Card.MannualTargetEffect,
                new List<CombatantView> { playCardGA.MannualTarget },
                HeroSystem.Instance.GetHero(heroIndex)
            );
            ActionSystem.Instance.AddReaction(effectAction);
        }

        foreach (var effectWrapper in playCardGA.Card.OtherEffects)
        {
            List<CombatantView> targets = effectWrapper.TargetMode.GetTargets(HeroSystem.Instance.GetHero(heroIndex));

            PerformEffectGA effectAction = new(
                effectWrapper.Effect,
                targets,
                HeroSystem.Instance.GetHero(heroIndex)
            );

            ActionSystem.Instance.AddReaction(effectAction);
        }

    }


    //Helper

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
