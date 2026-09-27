using System;
using System.Collections.Generic;
using System.Linq;
using static CombatManager;

// Standalone rule checks; compile with CombatManager.cs and the project's Unity assemblies.
internal static class CombatManagerChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    private static List<Card> Deck(int count, Effect effect = Effect.Damage, int amount = 3) =>
        Enumerable.Range(0, count).Select(i => new Card("Card " + i, effect, amount)).ToList();

    public static void Main()
    {
        var deck = Deck(12);
        var battle = new Battle(deck, 100, 100, 2, 42);
        Check(battle.Round == 1 && battle.Hand.Count == 5 && battle.DrawPile.Count == 7,
            "Initial round must draw five cards.");
        Check(!battle.ActEnemy() && !battle.PlaySelectedCard(Target.Enemy), "Out-of-order actions rejected.");
        Check(!battle.SelectCard(-1) && !battle.SelectCard(5), "Invalid indexes rejected.");
        Check(battle.SelectCard(0) && battle.EnemyHp == 100, "Selection must not apply damage.");
        Check(!battle.PlaySelectedCard((Target)99), "Unknown targets rejected.");
        Check(battle.SelectCard(1) && battle.SelectedIndex == 1, "Change selected card before targeting.");
        Check(battle.EnemyHp == 100 && battle.PlayerHp == 100, "Changing selection does not apply effects.");
        Check(battle.PlaySelectedCard(Target.Enemy) && battle.EnemyHp == 97, "Targeted damage.");
        Check(battle.PlayerActed && battle.Hand.Count == 0 && battle.DiscardPile.Count == 5,
            "Exactly one action and all cards discarded.");
        Check(!battle.SelectCard(0) && !battle.PlaySelectedCard(Target.Enemy), "Double play rejected.");
        Check(battle.ActEnemy() && battle.PlayerHp == 98 && battle.Round == 2 && !battle.PlayerActed,
            "Enemy attack starts next round and resets player action.");
        Check(battle.EnemyActed && !battle.ActEnemy(), "Enemy acts once.");
        for (int round = 0; round < 15; round++)
        {
            Check(battle.Hand.Count == 5, "Draw five across discard recycling.");
            var all = battle.Hand.Concat(battle.DrawPile).Concat(battle.DiscardPile).ToArray();
            Check(all.Length == 12 && all.Distinct().Count() == 12, "Cards conserved without duplication.");
            battle.SelectCard(0);
            battle.PlaySelectedCard(Target.Enemy);
            Check(!battle.EnemyActed, "Enemy action reset at start of enemy turn.");
            battle.ActEnemy();
        }
        Check(deck.Count == 12 && deck.All(c => c.amount == 3), "Authored deck is preserved.");
        var shortDeck = new Battle(Deck(2), 30, 30, 0, 1);
        Check(shortDeck.Hand.Count == 2, "Short decks draw available cards.");
        shortDeck.SelectCard(0);
        shortDeck.PlaySelectedCard(Target.Enemy);
        shortDeck.ActEnemy();
        Check(shortDeck.Hand.Count == 2 && shortDeck.DiscardPile.Count == 0, "Short deck recycles.");

        var win = new Battle(Deck(5, Effect.Damage, int.MaxValue), 10, 10, 10);
        win.SelectCard(0);
        win.PlaySelectedCard(Target.Enemy);
        Check(win.State == Combat.WIN && win.EnemyHp == 0 && win.CurrentTurn == Turn.Finished,
            "Lethal damage ends combat with clamped HP.");
        Check(!win.ActEnemy() && !win.SelectCard(0) && win.PlayerHp == 10 && win.Round == 1,
            "No retaliation or new round after victory.");
        var lose = new Battle(Deck(1, Effect.Damage, 0), 10, 10, 10);
        lose.SelectCard(0);
        lose.PlaySelectedCard(Target.Enemy);
        lose.ActEnemy();
        Check(lose.State == Combat.LOSE && lose.PlayerHp == 0 && lose.Round == 1 && !lose.ActEnemy(),
            "Enemy lethal attack ends combat without another round.");
        var self = new Battle(Deck(1, Effect.Damage, 10), 10, 10, 1);
        self.SelectCard(0);
        self.PlaySelectedCard(Target.Self);
        Check(self.State == Combat.LOSE && !self.ActEnemy(), "Self-targeted lethal damage.");
        var heal = new Battle(Deck(1, Effect.Heal, int.MaxValue), 10, 10, 2);
        heal.SelectCard(0);
        heal.PlaySelectedCard(Target.Self);
        Check(heal.PlayerHp == 10, "Healing capped without integer overflow.");
        heal.ActEnemy();
        heal.SelectCard(0);
        heal.PlaySelectedCard(Target.Self);
        Check(heal.PlayerHp == 10, "Healing restores missing HP.");
        var fresh = new Battle(deck, 100, 100, 2, 42);
        Check(fresh.Round == 1 && fresh.PlayerHp == 100 && fresh.DiscardPile.Count == 0,
            "New combat resets state.");
        foreach (var invalid in new[] { new List<Card>(), new List<Card> { null }, Deck(1, Effect.Damage, -1) })
        {
            bool rejected = false;
            try { new Battle(invalid, 10, 10, 1); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "Invalid deck must be rejected.");
        }
        Console.WriteLine($"PASS: {checks} combat rule checks.");
    }
}
