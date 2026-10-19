using UnityEngine;

public class DeckHUDUI : MonoBehaviour
{
    [Header("Pile Counts")]
    [SerializeField] private TMPro.TMP_Text drawPileText;
    [SerializeField] private TMPro.TMP_Text discardPileText;
    [SerializeField] private MatchSetupSystem matchSetup;

    private int displayedDrawCount = -1;
    private int displayedDiscardCount = -1;

    private void Awake()
    {
        if (matchSetup == null)
            matchSetup = FindFirstObjectByType<MatchSetupSystem>();

        // Resolve the existing labels when this component is attached to HUD/Deck.
        // Searching by label name also tolerates the discard wrapper's trailing space.
        foreach (TMPro.TMP_Text label in GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (drawPileText == null && label.name == "DrawDeckText")
                drawPileText = label;
            if (discardPileText == null && label.name == "DiscardDeckText")
                discardPileText = label;
        }

        if (drawPileText == null || discardPileText == null)
            Debug.LogWarning("DeckHUDUI needs the draw and discard pile text labels. Assign them in the Inspector or name them DrawDeckText and DiscardDeckText under Deck.", this);
    }

    private void OnEnable()
    {
        // Refresh after the HUD is shown again, including after a pause.
        displayedDrawCount = -1;
        displayedDiscardCount = -1;
    }

    private void LateUpdate()
    {
        // Wait for the actual decks, not just the transition out of the menus.
        if (matchSetup == null || !matchSetup.IsSetupComplete)
            return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        CardSystem cards = CardSystem.Instance;
        if (cards == null)
            return;

        // Read after selection and card coroutines update the piles. This also
        // covers recycling, hidden-hand changes and cards played by effects.
        int heroIndex = cards.ActiveHeroIndex;
        UpdateCount(drawPileText, cards.GetDrawPileCount(heroIndex), ref displayedDrawCount);
        UpdateCount(discardPileText, cards.GetDiscardPileCount(heroIndex), ref displayedDiscardCount);
    }

    private static void UpdateCount(TMPro.TMP_Text label, int count, ref int displayedCount)
    {
        if (label == null || count == displayedCount)
            return;

        label.text = count.ToString();
        displayedCount = count;
    }
}
