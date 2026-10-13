using DG.Tweening;
using UnityEngine;

public class CardViewCreator : SingleTon<CardViewCreator>
{
    [SerializeField] private CardView cardViewPrefab;

    public CardView CreateCardView(Card card, Vector3 position, Quaternion rotation, int heroIndex = 0)
    {
        CardView cardView = Instantiate(cardViewPrefab, position, rotation);
        cardView.transform.localScale = Vector3.zero;
        cardView.transform.DOScale(Vector3.one, 0.15f);
        cardView.Setup(card, heroIndex);
        return cardView;
    }
}
