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
    [SerializeField] private SpriteRenderer CharCard;
    [SerializeField] private SpriteRenderer CardRarity;

    [Header("Character Sprites")]
    [SerializeField] private Sprite whiskeySprite;
    [SerializeField] private Sprite adeSprite;

    [Header("Rarity Sprites")]
    [SerializeField] private Sprite baseRaritySprite;
    [SerializeField] private Sprite commonRaritySprite;
    [SerializeField] private Sprite rareRaritySprite;
    [SerializeField] private Sprite legendaryRaritySprite;

    public Card Card { get; private set; }
    public int HeroIndex { get; private set; }

    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;
    private bool isManualTargeting;
    private bool isDragging;

    public void Setup(Card card, int heroIndex = 0)
    {
        Card = card;
        HeroIndex = heroIndex;

        Title.text = card.Title;
        Description.text = card.Description;
        Cost.text = card.Cost.ToString();
        CardImage.sprite = card.Image;

        ApplyIdentitySprites(card);
    }

    private void ApplyIdentitySprites(Card card)
    {
        if (CharCard != null)
            CharCard.sprite = GetCharacterSprite(card.Character);

        if (CardRarity != null)
            CardRarity.sprite = GetRaritySprite(card.Rarity);
    }

    private Sprite GetCharacterSprite(Character character)
    {
        switch (character)
        {
            case Character.Whiskey:
                return whiskeySprite;
            case Character.Ade:
                return adeSprite;
            default:
                return null;
        }
    }

    private Sprite GetRaritySprite(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Base:
                return baseRaritySprite;
            case Rarity.Common:
                return commonRaritySprite;
            case Rarity.Rare:
                return rareRaritySprite;
            case Rarity.Legendary:
                return legendaryRaritySprite;
            default:
                return null;
        }
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

        if (Card.MannualTargetEffect != null)
        {
            ManualTargetSystem.Instance.StartTargeting(transform.position);
            isManualTargeting = true;
        }
        else
        {
            isDragging = true;
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
        if (!isDragging) return;
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (Card.MannualTargetEffect != null) return;
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    void OnMouseUp()
    {
        if (Card.MannualTargetEffect != null)
        {
            if (!isManualTargeting)
                return;

            isManualTargeting = false;
            CombatantView target = ManualTargetSystem.Instance.EndTargeting(
                MouseUtil.GetMousePositionInWorldSpace(-1), Card.ManualTargetKind);

            if (!Interactions.Instance.PlayerCanInteract() ||
                TurnSystem.Instance == null ||
                !TurnSystem.Instance.CanHeroPlayCards(HeroIndex))
                return;

            if (target != null && EnergySystem.Instance.HasEnoughEnergy(Card.Cost, HeroIndex) &&
                !ActionSystem.Instance.IsPerformingAction)
            {
                TriggerAttackAnimation();
                ActionSystem.Instance.Perform(new PlayCardGA(Card, target, HeroIndex));
            }
            // Targeted cards never fall through into the untargeted drop-area path.
            // An invalid hero/enemy selection leaves the card and its Energy untouched.
            return;
        }

        if (!isDragging) return;
        isDragging = false;
        if (!Interactions.Instance.PlayerCanInteract() ||
            TurnSystem.Instance == null ||
            !TurnSystem.Instance.CanHeroPlayCards(HeroIndex) ||
            ActionSystem.Instance.IsPerformingAction)
        {
            TriggerAttackAnimation();
            ReturnDraggedCard();
            return;
        }

        if (EnergySystem.Instance.HasEnoughEnergy(Card.Cost, HeroIndex) &&
            Physics.Raycast(transform.position, Vector3.forward,
                out RaycastHit hit, 10f, dropArea))
        {
            TriggerAttackAnimation();
            Interactions.Instance.PlayerIsDraging = false;
            ActionSystem.Instance.Perform(new PlayCardGA(Card, HeroIndex));
        }
        else
        {
            ReturnDraggedCard();
        }
    }

    private void OnDisable()
    {
        CancelInteraction();
    }

    public void CancelInteraction()
    {
        if (isManualTargeting && ManualTargetSystem.Instance != null)
            ManualTargetSystem.Instance.CancelTargeting();
        isManualTargeting = false;
        if (isDragging) ReturnDraggedCard();
        isDragging = false;
        if (Wrapper != null) Wrapper.SetActive(true);
    }

    private void ReturnDraggedCard()
    {
        transform.position = dragStartPosition;
        transform.rotation = dragStartRotation;

        if (Interactions.Instance != null)
            Interactions.Instance.PlayerIsDraging = false;
    }

    private void TriggerAttackAnimation()
    {
        if (Card == null || Card.Type != CardType.Attack || HeroSystem.Instance == null)
            return;

        PlayerView player = HeroSystem.Instance.GetHero(HeroIndex);
        PlayerAnimationController animationController =
            player != null
                ? player.GetComponentInChildren<PlayerAnimationController>(true)
                : null;

        animationController?.TriggerAttack();
    }
}
