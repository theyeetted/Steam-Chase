using UnityEngine;

/// <summary>The same authored query supplies a boolean condition or a numeric value.</summary>
[System.Serializable]
public class AttackHistoryQuery
{
    public enum Actor { Caster, Target, BothHeroes }

    [SerializeField] private Actor actor = Actor.Caster;
    [SerializeField] private AttackHistoryDirection direction = AttackHistoryDirection.Received;
    [Tooltip("Since Enemy Phase Start includes the latest enemy phase and the current shared player phase.")]
    [SerializeField] private AttackHistoryWindow window = AttackHistoryWindow.ThisCombat;
    [SerializeField] private AttackKind kinds = AttackKind.All;

    public bool TryGetCount(CombatantView target, CombatantView caster, out int count)
    {
        count = 0;
        if (HeroSystem.Instance == null)
            return false;

        int actorId = 0;
        bool bothHeroes = actor == Actor.BothHeroes;
        if (!bothHeroes)
        {
            CombatantView subject = actor switch
            {
                Actor.Caster => caster,
                Actor.Target => target,
                _ => null
            };
            // A missing/dead single subject must not pass an inverted condition.
            if (subject == null || subject.CurrentHealth <= 0)
                return false;
            actorId = subject.GetInstanceID();
        }

        // BothHeroes reads recorded membership, so an ally's earlier hits remain counted
        // after that ally dies. Historical enemies also need not remain on the board.
        count = HeroSystem.Instance.AttackHistory.Count(direction, window, kinds, actorId, bothHeroes);
        return true;
    }
}
