namespace Steamchasing.Combat
{
    /// <summary>
    /// Runtime status effects that can remain on a combat unit.
    /// Immediate card effects such as Damage, Block, Heal, Draw,
    /// StunGauge, ExtraAttack and FollowUp do not belong here.
    /// </summary>
    public enum StatusEffectType
    {
        Vulnerable,
        AttackUp,
        AttackDown,
        Aggravated,
        Stress,
        Shock,
        Mark
    }
}
