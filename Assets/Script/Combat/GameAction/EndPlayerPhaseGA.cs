/// <summary>Delivers player-phase-end jobs before the enemy turn's PRE reactions.</summary>
public sealed class EndPlayerPhaseGA : GameAction
{
    internal bool Delivered { get; set; }
}
