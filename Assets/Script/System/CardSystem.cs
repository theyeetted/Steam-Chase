using DG.Tweening;
using NUnit.Framework;
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
        if (!IsValidHero(heroIndex)) { return; }

        activeHeroIndex = heroIndex;

        if (displayHandCoroutine != null)
        {
            StopCoroutine(displayHandCoroutine);
            displayHandCoroutine = null;
        }
    
        // Remove visible card objects only.
        // The actual cards remain in each hero's hand list.
        handView.ResetHand();

        displayHandCoroutine = StartCoroutine(DisplayHand(heroIndex));
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

        displayHandCoroutine = null;
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
            List<CombatantView> targets = effectWrapper.TargetMode.GetTargets();

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

        yield return handView.AddCard(cardView);
    }

    private void RefillDeck(int heroIndex)
    {
        drawPiles[heroIndex].AddRange(discardPiles[heroIndex]);
        discardPiles[heroIndex].Clear();
    }

    private IEnumerator DiscardCard(int heroIndex, CardView cardView)
    {
        discardPiles[heroIndex].Add(cardView.Card);

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

        drawPiles[heroIndex].Clear();
        hands[heroIndex].Clear();
        discardPiles[heroIndex].Clear();
    }

    private bool IsValidHero(int heroIndex)
    {
        return heroIndex >= 0 && heroIndex < hands.Length;
    }
}
