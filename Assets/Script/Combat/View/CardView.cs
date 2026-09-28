using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TextMeshPro Title;
    [SerializeField] private TextMeshPro Description;
    [SerializeField] private TextMeshPro Cost;
    [SerializeField] private SpriteRenderer CardImage;
    [SerializeField] private GameObject Wrapper;
    [SerializeField] private LayerMask dropArea;

    public Card Card { get; private set; }

    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;

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
        if (!Interactions.Instance.PlayerCanHover()) return;
        Wrapper.SetActive(false);
        Vector3 pos = new(transform.position.x, -2, 0);
        CardViewHoverSystem.Instance.Show(Card, pos);
    }

    void OnMouseExit()
    {
        if (!Interactions.Instance.PlayerCanHover()) return;
        CardViewHoverSystem.Instance.Hide();
        Wrapper.SetActive(true);
    }

    void OnMouseDown()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        Interactions.Instance.PlayerIsDraging = true;
        CardViewHoverSystem.Instance.Hide();
        Wrapper.SetActive(true);
        dragStartPosition = transform.position;
        dragStartRotation = transform.rotation;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    void OnMouseDrag()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    void OnMouseUp()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (EnergySystem.Instance.HasEnoughEnergy(Card.Cost) && Physics.Raycast(transform.position, Vector3.forward, out RaycastHit hit, 10f, dropArea))
        {
            PlayCardGA playCardGA = new(Card);
            ActionSystem.Instance.Perform(playCardGA);
        }
        else
        {
            transform.position = dragStartPosition;
            transform.rotation = dragStartRotation;
        }
        Interactions.Instance.PlayerIsDraging = false;
    }
}
