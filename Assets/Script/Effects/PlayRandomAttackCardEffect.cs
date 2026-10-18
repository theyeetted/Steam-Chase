using System.Collections.Generic;

/// <summary>
/// Execute a random Attack from the caster's current hand for free. Enemy selection
/// happens at execution; area and self effects retain their authored target modes.
/// </summary>
[System.Serializable]
public sealed class PlayRandomAttackCardEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new PlayRandomAttackCardGA(caster);
    }
}
