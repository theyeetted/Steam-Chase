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
    public int HeroIndex { get; private set; }

    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;

    public void Setup(Card card, int heroIndex = 0) // default value of 0 keeps older calls to Setup(card) working
    {
        Card = card;
        HeroIndex = heroIndex;

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

        if (TurnSystem.Instance == null ||
            !TurnSystem.Instance.CanHeroPlayCards(HeroIndex))
            return;

        if(Card.MannualTargetEffect != null)
        {
            ManualTargetSystem.Instance.StartTargeting(transform.position);
        }
        else
        {
            Interactions.Instance.PlayerIsDraging = true;
            CardViewHoverSystem.Instance.Hide();
            Wrapper.SetActive(true);
            dragStartPosition = transform.position;
            dragStartRotation = transform.rotation;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
        }
            
    }

    void OnMouseDrag()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if(Card.MannualTargetEffect != null) return;
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    void OnMouseUp()
    {
        if (Card.MannualTargetEffect != null)
        {
            if (!Interactions.Instance.PlayerCanInteract() ||
                TurnSystem.Instance == null ||
                !TurnSystem.Instance.CanHeroPlayCards(HeroIndex))
                return;

            EnemyView target = ManualTargetSystem.Instance.EndTargeting(MouseUtil.GetMousePositionInWorldSpace(-1));
            
            if (target != null && EnergySystem.Instance.HasEnoughEnergy(Card.Cost, HeroIndex) &&
            !ActionSystem.Instance.IsPerformingAction)
            {
                ActionSystem.Instance.Perform(new PlayCardGA(Card, target, HeroIndex));
            }
        }
        if (!Interactions.Instance.PlayerCanInteract() ||
            TurnSystem.Instance == null ||
            !TurnSystem.Instance.CanHeroPlayCards(HeroIndex) ||
            ActionSystem.Instance.IsPerformingAction)
        {
            ReturnDraggedCard();
            return;
        }

        if (EnergySystem.Instance.HasEnoughEnergy(Card.Cost, HeroIndex) &&
            Physics.Raycast(transform.position, Vector3.forward,
                out RaycastHit hit, 10f, dropArea))
        {
            Interactions.Instance.PlayerIsDraging = false;
            ActionSystem.Instance.Perform(new PlayCardGA(Card, HeroIndex));
        }
        else
        {
            ReturnDraggedCard();
        }
        
    }

    private void ReturnDraggedCard()
    {
        transform.position = dragStartPosition;
        transform.rotation = dragStartRotation;

        if (Interactions.Instance != null)
            Interactions.Instance.PlayerIsDraging = false;
    }
}
