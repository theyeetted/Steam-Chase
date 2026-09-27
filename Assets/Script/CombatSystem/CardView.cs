using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TextMeshPro Title;
    [SerializeField] private TextMeshPro Description;
    [SerializeField] private TextMeshPro Cost;
    [SerializeField] private SpriteRenderer CardImage;
    [SerializeField] private GameObject Wrapper;

    public Card Card { get; private set; }

    public void Setup(Card card)
    {
        Card = card;
        Title.text = card.Title;
        Description.text = card.Description;
        Cost.text = card.Cost.ToString();
        CardImage.sprite = card.Image;
    }
}
