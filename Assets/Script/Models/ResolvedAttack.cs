/// <summary>
/// One landed hit against one target, including fully blocked and lethal hits.
/// Snapshot identities survive a combatant being defeated or destroyed. No action trees
/// or scene-object references are retained in the combat history.
/// </summary>
public sealed class ResolvedAttack
{
    public long Id { get; }
    public long EnemyWindowId { get; }
    public int SourceId { get; }
    public int TargetId { get; }
    public bool SourceIsHero { get; }
    public bool TargetIsHero { get; }
    public AttackKind Kind { get; }

    internal ResolvedAttack(long id, long enemyWindowId, int sourceId, int targetId,
        bool sourceIsHero, bool targetIsHero, AttackKind kind)
    {
        Id = id;
        EnemyWindowId = enemyWindowId;
        SourceId = sourceId;
        TargetId = targetId;
        SourceIsHero = sourceIsHero;
        TargetIsHero = targetIsHero;
        Kind = kind;
    }
}
