using DG.Tweening;
using UnityEngine;

public class CardViewCreator : SingleTon<CardViewCreator>
{
    [SerializeField] private CardView cardViewPrefab;

    

    public CardView CreateCardView(Card card, Vector3 position, Quaternion roptation)
    {
        CardView cardView = Instantiate(cardViewPrefab, position, roptation);
        cardView.transform.localScale = Vector3.zero;
        cardView.transform.DOScale(Vector3.one, 0.15f);
        cardView.Setup(card);
        return cardView;
    }
}
