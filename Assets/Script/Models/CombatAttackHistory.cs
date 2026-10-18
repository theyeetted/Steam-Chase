using System.Collections.Generic;

/// <summary>Runtime combat facts owned by HeroSystem, never by shared card definitions.</summary>
public sealed class CombatAttackHistory
{
    private readonly List<ResolvedAttack> attacks = new();
    private long nextAttackId;
    private long enemyWindowId;
    public long LastAttackId => nextAttackId;

    internal void BeginEnemyPhase()
    {
        // One explicit call from EnemyTurnPerformer, after player-end PRE reactions finish.
        // Do not reset when switching heroes or when the subsequent player phase begins.
        enemyWindowId++;
    }

    internal ResolvedAttack RecordHit(CombatantView source, CombatantView target,
        AttackKind kind, bool sourceIsHero, bool targetIsHero)
    {
        // Target health may already be zero: a killing hit still happened.
        if (target == null || kind == AttackKind.None)
            return null;

        ResolvedAttack attack = new(++nextAttackId, enemyWindowId,
            source != null ? source.GetInstanceID() : 0, target.GetInstanceID(),
            sourceIsHero, targetIsHero, kind);
        attacks.Add(attack);
        return attack;
    }

    public int Count(AttackHistoryDirection direction, AttackHistoryWindow window,
        AttackKind kinds, int actorId, bool bothHeroes)
    {
        if (kinds == AttackKind.None ||
            (direction != AttackHistoryDirection.Received && direction != AttackHistoryDirection.Performed) ||
            (window != AttackHistoryWindow.ThisCombat && window != AttackHistoryWindow.SinceEnemyPhaseStart))
            return 0;

        int count = 0;
        foreach (ResolvedAttack attack in attacks)
        {
            if ((attack.Kind & kinds) == AttackKind.None ||
                (window == AttackHistoryWindow.SinceEnemyPhaseStart && attack.EnemyWindowId != enemyWindowId))
                continue;

            bool performed = direction == AttackHistoryDirection.Performed;
            bool matches = bothHeroes
                ? performed ? attack.SourceIsHero : attack.TargetIsHero
                : actorId != 0 && actorId == (performed ? attack.SourceId : attack.TargetId);
            if (matches)
                count++;
        }
        return count;
    }

    public void Clear()
    {
        attacks.Clear();
        nextAttackId = 0;
        enemyWindowId = 0;
    }
}
