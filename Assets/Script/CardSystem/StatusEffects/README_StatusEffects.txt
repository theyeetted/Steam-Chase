STEAMCHASING STATUS EFFECT SYSTEM
=================================

Put the .cs files in a Unity folder such as:

Assets/
  Scripts/
    Combat/
      StatusEffects/

FILES
-----
StatusEffectType.cs
StatusEffectInstance.cs
IStatusEffectOwner.cs
StatusEffectController.cs

SETUP
-----
1. Add StatusEffectController to every player/enemy combat GameObject.

2. The same GameObject must also contain a MonoBehaviour that implements
   IStatusEffectOwner.

Example:

public class EnemyCombatant : MonoBehaviour, IStatusEffectOwner
{
    [SerializeField] private StatusEffectController statusEffects;

    public bool IsStunned => false; // Replace with your stun system.

    public void TakeDirectDamage(int damage)
    {
        // Replace with your health system.
    }

    public void TriggerFollowUpAttack(
        IStatusEffectOwner target,
        int baseDamage)
    {
        // Replace with your Follow-Up attack system.
    }
}

3. Apply a status from your CardExecutor:

target.StatusEffects.ApplyStatus(
    StatusEffectType.Vulnerable,
    effect.value,
    source: attacker
);

4. Damage flow:

int damage = attacker.StatusEffects.ModifyOutgoingDamage(
    baseDamage,
    target.StatusEffects
);

damage = target.StatusEffects.ModifyIncomingDamage(damage);

target.TakeDamage(damage);

target.StatusEffects.OnAttacked(
    attacker.StatusEffects
);

5. At the end of a unit's turn:

unit.StatusEffects.OnOwnerTurnEnd();

6. At the end of the player's complete turn:

foreach (EnemyCombatant enemy in enemies)
{
    enemy.StatusEffects.OnPlayerTurnEnd();
}

IMPORTANT
---------
This package intentionally uses StatusEffectType instead of your Card
EffectType enum.

That keeps the runtime status system separate from immediate card effects
such as Damage, Block, Heal, StunGauge, ExtraAttack and FollowUp.

Your CardExecutor should translate card EffectType values into
StatusEffectType when applying a stored status.
