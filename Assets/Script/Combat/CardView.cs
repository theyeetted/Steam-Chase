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

    void OnMouseEnter()
    {
        Wrapper.SetActive(false);
        Vector3 pos = new(transform.position.x, -2, 0);
        CardViewHoverSystem.Instance.show(Card, pos);
    }

    void OnMouseExit()
    {
        CardViewHoverSystem.Instance.hide();
        Wrapper.SetActive(true);
    }
}
