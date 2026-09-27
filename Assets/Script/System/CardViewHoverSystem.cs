using UnityEngine;

public class CardViewHoverSystem : SingleTon<CardViewHoverSystem>
{
    [SerializeField] private CardView cardViewHover;

    public void show(Card card, Vector3 position)
    {
        cardViewHover.gameObject.SetActive(true);
        cardViewHover.Setup(card);
        cardViewHover.transform.position = position;
    }

    public void hide()
    {
        cardViewHover.gameObject.SetActive(false);
    }
}
