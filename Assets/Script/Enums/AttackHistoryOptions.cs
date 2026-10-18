[System.Flags]
public enum AttackKind
{
    None = 0,
    Normal = 1,
    ExtraAttack = 2,
    FollowUp = 4,
    Counter = 8,
    All = Normal | ExtraAttack | FollowUp | Counter
}

public enum AttackHistoryDirection { Received, Performed }

public enum AttackHistoryWindow
{
    ThisCombat,
    // Starts when enemies begin acting and continues through both heroes' next player phase.
    SinceEnemyPhaseStart
}
