using UnityEngine;

public class EnemyTurnGA : GameAction
{
    public EnemyTurnGA()
    {
        // Finish scheduled cards before discard, status snapshots, or enemy actions.
        // Every entry point that creates EnemyTurnGA uses this boundary.
        OpeningReactions.Add(new EndPlayerPhaseGA());
        // Both the UI and TurnSystem create this action, so both use one completion point.
        CompletionReactions.Add(new StartPlayerPhaseGA());
    }
}
