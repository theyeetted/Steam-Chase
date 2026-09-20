namespace Steamchasing.Combat
{
    /// <summary>
    /// Implement this interface on your player/enemy combat component.
    ///
    /// StatusEffectController uses this interface so it does not need
    /// to know whether its owner is a player, enemy, elite or boss.
    /// </summary>
    public interface IStatusEffectOwner
    {
        /// <summary>
        /// True while this unit is currently stunned.
        /// </summary>
        bool IsStunned { get; }

        /// <summary>
        /// Damage that bypasses normal Attack Up / Attack Down /
        /// Vulnerable calculations.
        ///
        /// Used by effects such as Shock and Stress.
        /// </summary>
        void TakeDirectDamage(int damage);

        /// <summary>
        /// Called when Mark causes an off-turn Follow-Up attack.
        /// </summary>
        void TriggerFollowUpAttack(
            IStatusEffectOwner target,
            int baseDamage
        );
    }
}
