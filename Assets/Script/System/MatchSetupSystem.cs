using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MatchSetupSystem : MonoBehaviour
{
    [Header("Heroes")]
    [SerializeField] private CharData firstCharData;
    [SerializeField] private CharData secondCharData;

    [Header("Deck choices (Whiskey = first hero, Ade = second hero)")]
    [Tooltip("Slots 0-2 are Deck 1-3. An empty Deck 1 slot uses First Char Data for existing scenes.")]
    [SerializeField] private CharData[] whiskeyDecks = new CharData[3];
    [Tooltip("Slots 0-2 are Deck 1-3. An empty Deck 1 slot uses Second Char Data for existing scenes.")]
    [SerializeField] private CharData[] adeDecks = new CharData[3];

    [Header("Match")]
    [SerializeField] private List<EnemyData> enemyDatas;

    [Header("Difficulty encounters")]
    [Tooltip("If empty, Normal uses the existing Enemy Datas list.")]
    [SerializeField] private List<EnemyData> normalEnemyDatas = new();
    [SerializeField] private List<EnemyData> eliteEnemyDatas = new();

    public bool IsSetupComplete { get; private set; }
    public CharData ConfiguredWhiskeyData => firstCharData;
    public CharData ConfiguredAdeData => secondCharData;

    public bool IsDeckAvailable(int heroIndex, int deckNumber)
    {
        return IsDeckDataAvailable(GetDeck(heroIndex, deckNumber));
    }

    private static bool IsDeckDataAvailable(CharData data)
    {
        return data != null && data.Health > 0 && data.Deck != null &&
            data.Deck.Count > 0 && !data.Deck.Exists(card => card == null);
    }

    private CharData GetDeck(int heroIndex, int deckNumber)
    {
        if (heroIndex < 0 || heroIndex > 1 || deckNumber < 1 || deckNumber > 3) return null;
        CharData[] choices = heroIndex == 0 ? whiskeyDecks : adeDecks;
        if (choices != null && choices.Length >= deckNumber && choices[deckNumber - 1] != null)
            return choices[deckNumber - 1];
        return deckNumber == 1 ? (heroIndex == 0 ? firstCharData : secondCharData) : null;
    }

    private List<EnemyData> GetEncounter(GameManager.Difficulty difficulty)
    {
        if (difficulty == GameManager.Difficulty.Elite) return eliteEnemyDatas;
        if (difficulty != GameManager.Difficulty.Normal) return null;
        return normalEnemyDatas != null && normalEnemyDatas.Count > 0 ? normalEnemyDatas : enemyDatas;
    }

    public bool CanConfigureMatch(GameManager.Difficulty difficulty, int whiskeyDeck, int adeDeck, out string error)
    {
        return CanConfigureMatch(difficulty, GetDeck(0, whiskeyDeck), GetDeck(1, adeDeck), out error);
    }

    private bool CanConfigureMatch(GameManager.Difficulty difficulty, CharData whiskeyData,
        CharData adeData, out string error)
    {
        error = null;
        if (!IsDeckDataAvailable(whiskeyData) || !IsDeckDataAvailable(adeData))
            error = "Select a configured deck for both Whiskey and Ade. Each CharData needs positive health and a nonempty deck without missing cards.";
        List<EnemyData> encounter = GetEncounter(difficulty);
        if (error == null && (encounter == null || encounter.Count == 0 || encounter.Exists(enemy => enemy == null)))
            error = $"Assign a nonempty {difficulty} enemy list without missing EnemyData entries in MatchSetupSystem.";
        return error == null;
    }

    public bool ConfigureMatch(GameManager.Difficulty difficulty, int whiskeyDeck, int adeDeck, out string error)
    {
        return ConfigureMatch(difficulty, GetDeck(0, whiskeyDeck), GetDeck(1, adeDeck), out error);
    }

    public bool ConfigureMatch(GameManager.Difficulty difficulty, CharData whiskeyData,
        CharData adeData, out string error)
    {
        if (IsSetupComplete)
        {
            error = "Reload the fight before changing its configuration.";
            return false;
        }
        if (!CanConfigureMatch(difficulty, whiskeyData, adeData, out error)) return false;
        if (HeroSystem.Instance == null || HeroSystem.Instance.playerView == null ||
            HeroSystem.Instance.secondPlayerView == null || EnemySystem.Instance == null ||
            CardSystem.Instance == null || ActionSystem.Instance == null || EnergySystem.Instance == null)
        {
            error = "Keep both hero views and the hero, enemy, card, action, energy active and configured.";
            return false;
        }
        firstCharData = whiskeyData;
        secondCharData = adeData;
        enemyDatas = new List<EnemyData>(GetEncounter(difficulty));
        return true;
    }

    private IEnumerator Start()
    {
        // Leave the board and deck untouched until the start menu is dismissed.
        while (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            yield return null;
        }


        HeroSystem.Instance.Setup(firstCharData, secondCharData);
        EnemySystem.Instance.Setup(enemyDatas);
        
        // Independent deck
        CardSystem.Instance.Setup(0, firstCharData.Deck);
        CardSystem.Instance.Setup(1, secondCharData.Deck);

        IsSetupComplete = true;
        // Use one sequence so the busy action system does not reject the second hero's draw.
        DrawOpeningHands();
    }


    // Added --------------------

    private void DrawOpeningHands()
    {
        ActionSystem.Instance.Perform(new DrawCardsGA(5, 0), () =>
        {
            ActionSystem.Instance.Perform(new DrawCardsGA(5, 1), () =>
            {
                // Ensure both energy counters are full.
                EnergySystem.Instance.ResetEnergy();
            });
        });
    }
}
