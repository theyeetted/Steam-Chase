/// <summary>Free execution from the owner's hand, without recording another manual play.</summary>
public sealed class PlayRandomAttackCardGA : GameAction
{
    public CombatantView Owner { get; }

    public PlayRandomAttackCardGA(CombatantView owner)
    {
        Owner = owner;
    }
}
