using UnityEngine;

public enum StatusEffectType
{ 
    VULNERABLE,
    SHOCK,
    BLOCK,
    ATTACKUP,
    AttackDown,
    Mark,
    ExtraAttack,
    FollowUpAttack,
    DefenseUp,
    Stun,
    Aggravated,
    // Bonus damage points, separate from the attack's charges or Mark's stacks.
    MarkFollowUpDamageUp,
    FollowUpAttackDamageUp,
    ExtraAttackDamageUp,
    Stress,
    Counter,
    // Got 'em: cards drawn per enemy Vulnerable application by this hero, for this combat.
    DrawOnVulnerable
}
