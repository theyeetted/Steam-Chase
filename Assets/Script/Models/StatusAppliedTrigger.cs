using UnityEngine;

/// <summary>Matches the status, source ownership, and recipient of a successful application.</summary>
[System.Serializable]
public sealed class StatusAppliedTrigger : EffectTrigger
{
    public enum RecipientScope { Any, Owner, Enemy }

    [SerializeField] private StatusEffectType statusEffectType = StatusEffectType.VULNERABLE;
    [Tooltip("Only applications made by the hero who owns this rule can trigger it.")]
    [SerializeField] private bool requireOwnerSource = true;
    [SerializeField] private RecipientScope recipient = RecipientScope.Enemy;

    public StatusAppliedTrigger() { }

    public StatusAppliedTrigger(StatusEffectType type, bool ownerSource, RecipientScope recipientScope)
    {
        statusEffectType = type;
        requireOwnerSource = ownerSource;
        recipient = recipientScope;
    }

    public override bool IsMet(GameAction combatEvent, CombatantView owner)
    {
        if (!(combatEvent is StatusAppliedGA application) || application.AppliedStacks <= 0 ||
            application.StatusEffectType != statusEffectType || owner == null)
            return false;

        if (requireOwnerSource && application.Caster != owner)
            return false;

        // The event records an application that already succeeded. Later reactions may
        // have killed the recipient; do not reinterpret that earlier success as a failure.
        return recipient switch
        {
            RecipientScope.Any => true,
            RecipientScope.Owner => application.Target == owner,
            RecipientScope.Enemy => application.Target is EnemyView,
            _ => false,
        };
    }
}
