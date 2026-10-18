using UnityEngine;

/// <summary>One resolved hit received by the rule's owner, including fully blocked hits.</summary>
[System.Serializable]
public sealed class AttackReceivedTrigger : EffectTrigger
{
    [SerializeField] private AttackKind kinds = AttackKind.All;

    public override bool IsMet(GameAction combatEvent, CombatantView owner)
    {
        return owner != null && combatEvent is AttackResolvedGA resolved && resolved.Attack != null &&
            resolved.Attack.TargetId == owner.GetInstanceID() &&
            (resolved.Attack.Kind & kinds) != AttackKind.None;
    }
}
