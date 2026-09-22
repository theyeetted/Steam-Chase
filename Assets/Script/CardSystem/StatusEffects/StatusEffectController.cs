using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steamchasing.Combat
{
    /// <summary>
    /// Stores and processes runtime status effects for one combat unit.
    ///
    /// Attach one StatusEffectController to every player character
    /// and every enemy that can receive statuses.
    /// </summary>
    public class StatusEffectController : MonoBehaviour
    {
        private readonly Dictionary<StatusEffectType, StatusEffectInstance>
            activeStatuses = new();

        private IStatusEffectOwner owner;

        // Attack Down loses one stack only if it actually affected
        // an attack during this unit's turn.
        private bool attackDownTriggeredThisTurn;

        /// <summary>
        /// Fired whenever a status is added or its stack amount changes.
        /// Parameters: status type, current stacks.
        /// Useful for status UI.
        /// </summary>
        public event Action<StatusEffectType, int> OnStatusChanged;

        /// <summary>
        /// Fired whenever a status disappears.
        /// Useful for status UI.
        /// </summary>
        public event Action<StatusEffectType> OnStatusRemoved;

        public IStatusEffectOwner Owner => owner;

        private void Awake()
        {
            // Unity supports retrieving a component by interface.
            owner = GetComponent<IStatusEffectOwner>();

            if (owner == null)
            {
                Debug.LogError(
                    $"{name}: StatusEffectController requires another component " +
                    $"on the same GameObject that implements IStatusEffectOwner.",
                    this
                );
            }
        }

        // =========================================================
        // APPLY / QUERY
        // =========================================================

        public void ApplyStatus(
            StatusEffectType type,
            int stacks,
            int duration = -1,
            IStatusEffectOwner source = null)
        {
            if (stacks <= 0)
                return;

            int maxStacks = GetMaxStacks(type);

            if (activeStatuses.TryGetValue(type, out StatusEffectInstance status))
            {
                status.AddStacks(stacks, maxStacks);

                if (duration > 0)
                    status.RefreshDuration(duration);
            }
            else
            {
                int startingStacks = Mathf.Min(stacks, maxStacks);

                status = new StatusEffectInstance(
                    type,
                    startingStacks,
                    duration,
                    source
                );

                activeStatuses.Add(type, status);
            }

            OnStatusChanged?.Invoke(type, status.Stacks);
        }

        public bool HasStatus(StatusEffectType type)
        {
            return activeStatuses.TryGetValue(type, out StatusEffectInstance status)
                   && !status.IsExpired();
        }

        public int GetStacks(StatusEffectType type)
        {
            if (activeStatuses.TryGetValue(type, out StatusEffectInstance status))
                return status.Stacks;

            return 0;
        }

        public StatusEffectInstance GetStatus(StatusEffectType type)
        {
            activeStatuses.TryGetValue(type, out StatusEffectInstance status);
            return status;
        }

        public IReadOnlyDictionary<StatusEffectType, StatusEffectInstance>
            GetAllStatuses()
        {
            return activeStatuses;
        }

        // =========================================================
        // REMOVE
        // =========================================================

        public void RemoveStatus(StatusEffectType type)
        {
            if (!activeStatuses.Remove(type))
                return;

            OnStatusRemoved?.Invoke(type);
        }

        public void RemoveStacks(StatusEffectType type, int amount)
        {
            if (amount <= 0)
                return;

            if (!activeStatuses.TryGetValue(type, out StatusEffectInstance status))
                return;

            status.RemoveStacks(amount);

            if (status.IsExpired())
            {
                RemoveStatus(type);
                return;
            }

            OnStatusChanged?.Invoke(type, status.Stacks);
        }

        public void SetStacks(StatusEffectType type, int amount)
        {
            if (!activeStatuses.TryGetValue(type, out StatusEffectInstance status))
                return;

            status.SetStacks(Mathf.Min(amount, GetMaxStacks(type)));

            if (status.IsExpired())
            {
                RemoveStatus(type);
                return;
            }

            OnStatusChanged?.Invoke(type, status.Stacks);
        }

        public void ClearAllStatuses()
        {
            StatusEffectType[] types =
                new StatusEffectType[activeStatuses.Count];

            activeStatuses.Keys.CopyTo(types, 0);

            foreach (StatusEffectType type in types)
                RemoveStatus(type);
        }

        // =========================================================
        // DAMAGE MODIFIERS
        // =========================================================

        /// <summary>
        /// Apply outgoing damage modifiers belonging to this unit.
        ///
        /// Current rules:
        /// Attack Up: +1 damage per stack per hit.
        /// Attack Down: -1 damage per stack per hit.
        /// Aggravated: -15% damage when attacking its forced target.
        /// </summary>
        public int ModifyOutgoingDamage(
            int baseDamage,
            StatusEffectController target)
        {
            float damage = baseDamage;

            int attackUp = GetStacks(StatusEffectType.AttackUp);
            damage += attackUp;

            int attackDown = GetStacks(StatusEffectType.AttackDown);

            if (attackDown > 0)
            {
                damage -= attackDown;
                attackDownTriggeredThisTurn = true;
            }

            if (HasStatus(StatusEffectType.Aggravated))
            {
                StatusEffectInstance aggravated =
                    GetStatus(StatusEffectType.Aggravated);

                if (aggravated.Source != null &&
                    target != null &&
                    aggravated.Source == target.owner)
                {
                    damage *= 0.85f;
                }
            }

            return Mathf.Max(0, Mathf.RoundToInt(damage));
        }

        /// <summary>
        /// Apply incoming damage modifiers belonging to this unit.
        ///
        /// Vulnerable: +25% received damage.
        /// Stunned: +20% received damage.
        /// </summary>
        public int ModifyIncomingDamage(int damage)
        {
            float finalDamage = damage;

            if (HasStatus(StatusEffectType.Vulnerable))
                finalDamage *= 1.25f;

            if (owner != null && owner.IsStunned)
                finalDamage *= 1.20f;

            return Mathf.Max(0, Mathf.RoundToInt(finalDamage));
        }

        // =========================================================
        // REACTIVE EFFECTS
        // =========================================================

        /// <summary>
        /// Call this after this unit is successfully struck by an attack.
        /// Currently processes Mark.
        /// </summary>
        public void OnAttacked(StatusEffectController attacker)
        {
            HandleMark(attacker);
        }

        private void HandleMark(StatusEffectController attacker)
        {
            if (GetStacks(StatusEffectType.Mark) <= 0)
                return;

            // One Mark is consumed whenever the marked target is hit.
            RemoveStacks(StatusEffectType.Mark, 1);

            if (attacker == null ||
                attacker.owner == null ||
                owner == null)
            {
                return;
            }

            // Base Follow-Up attack from the GDD. CombatUnit has a dedicated
            // Mark path so Mark-only Follow-Up bonuses do not affect other Follow-Ups.
            if (attacker.owner is CombatUnit combatAttacker)
                combatAttacker.TriggerMarkFollowUpAttack(owner, 4);
            else
                attacker.owner.TriggerFollowUpAttack(owner, 4);
        }

        // =========================================================
        // TURN EVENTS
        // =========================================================

        /// <summary>
        /// Call when THIS UNIT's turn ends.
        ///
        /// Processes Shock, Attack Down decay, Vulnerable decay,
        /// and normal timed statuses.
        /// </summary>
        public void OnOwnerTurnEnd()
        {
            HandleShock();
            HandleAttackDownDecay();
            HandleVulnerableDecay();
            HandleDurationDecay();
        }

        /// <summary>
        /// Call at the end of the PLAYER'S complete turn for units
        /// that can hold Stress.
        ///
        /// Stress detonates only when the affected unit is stunned.
        /// </summary>
        public void OnPlayerTurnEnd()
        {
            HandleStress();
        }

        // =========================================================
        // VULNERABLE
        // =========================================================

        private void HandleVulnerableDecay()
        {
            if (!HasStatus(StatusEffectType.Vulnerable))
                return;

            // Vulnerable stacks function as remaining turns.
            RemoveStacks(StatusEffectType.Vulnerable, 1);
        }

        // =========================================================
        // ATTACK DOWN
        // =========================================================

        private void HandleAttackDownDecay()
        {
            if (!HasStatus(StatusEffectType.AttackDown))
            {
                attackDownTriggeredThisTurn = false;
                return;
            }

            if (!attackDownTriggeredThisTurn)
                return;

            RemoveStacks(StatusEffectType.AttackDown, 1);
            attackDownTriggeredThisTurn = false;
        }

        // =========================================================
        // SHOCK
        // =========================================================

        private void HandleShock()
        {
            int shockStacks = GetStacks(StatusEffectType.Shock);

            if (shockStacks <= 0 || owner == null)
                return;

            int damage = shockStacks * 4;

            if (owner is CombatUnit combatOwner)
                damage = combatOwner.ModifyShockTickDamage(damage);

            owner.TakeDirectDamage(damage);

            // Shock loses one stack after triggering.
            RemoveStacks(StatusEffectType.Shock, 1);
        }

        /// <summary>
        /// Immediately triggers Shock without applying normal turn-end
        /// behavior. Useful for cards such as Short Circuit.
        /// </summary>
        public int TriggerShockImmediately(bool consumeAllStacks)
        {
            int stacks = GetStacks(StatusEffectType.Shock);

            if (stacks <= 0 || owner == null)
                return 0;

            int damage = stacks * 4;

            if (owner is CombatUnit combatOwner)
                damage = combatOwner.ModifyShockTickDamage(damage);

            owner.TakeDirectDamage(damage);

            if (consumeAllStacks)
                RemoveStatus(StatusEffectType.Shock);

            return stacks;
        }

        // =========================================================
        // STRESS
        // =========================================================

        private void HandleStress()
        {
            int stressStacks = GetStacks(StatusEffectType.Stress);

            if (stressStacks <= 0 ||
                owner == null ||
                !owner.IsStunned)
            {
                return;
            }

            // Stress deals 1 damage per stack and ignores ordinary
            // damage modifiers, but receives the native Stun +20%.
            int damage = Mathf.RoundToInt(stressStacks * 1.20f);

            owner.TakeDirectDamage(damage);

            // Normal Stress detonation resets the status.
            RemoveStatus(StatusEffectType.Stress);
        }

        /// <summary>
        /// Immediately detonate Stress.
        /// Useful for cards such as Watch It Blow!.
        ///
        /// Set consumeStacks=false when the card says that the
        /// detonation does not consume/reset Stress.
        /// </summary>
        public int TriggerStressImmediately(bool consumeStacks)
        {
            int stressStacks = GetStacks(StatusEffectType.Stress);

            if (stressStacks <= 0 || owner == null)
                return 0;

            float damage = stressStacks;

            // Stress ignores ordinary Attack Up / Vulnerable modifiers,
            // but receives the native +20% damage bonus while Stunned.
            if (owner.IsStunned)
                damage *= 1.20f;

            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(damage));

            owner.TakeDirectDamage(finalDamage);

            if (consumeStacks)
                RemoveStatus(StatusEffectType.Stress);

            // Watch It Blow! needs the actual damage dealt so it can convert
            // 40% of that number into Stun-gauge percentage points.
            return finalDamage;
        }

        /// <summary>
        /// Double Stress stacks, capped at 99.
        /// Useful for Stress Test!.
        /// </summary>
        public void DoubleStress()
        {
            int stress = GetStacks(StatusEffectType.Stress);

            if (stress <= 0)
                return;

            SetStacks(
                StatusEffectType.Stress,
                Mathf.Min(stress * 2, 99)
            );
        }

        // =========================================================
        // STANDARD DURATION EFFECTS
        // =========================================================

        private void HandleDurationDecay()
        {
            List<StatusEffectType> expired = new();

            foreach (KeyValuePair<StatusEffectType, StatusEffectInstance> pair
                     in activeStatuses)
            {
                StatusEffectInstance status = pair.Value;

                // These statuses use specialized decay rules.
                if (status.Type == StatusEffectType.Vulnerable ||
                    status.Type == StatusEffectType.Shock ||
                    status.Type == StatusEffectType.AttackDown ||
                    status.Type == StatusEffectType.Stress)
                {
                    continue;
                }

                if (status.Duration < 0)
                    continue;

                status.ReduceDuration();

                if (status.IsExpired())
                    expired.Add(status.Type);
            }

            foreach (StatusEffectType type in expired)
                RemoveStatus(type);
        }

        // =========================================================
        // STACK LIMITS
        // =========================================================

        private int GetMaxStacks(StatusEffectType type)
        {
            switch (type)
            {
                case StatusEffectType.Stress:
                    return 99;

                case StatusEffectType.Shock:
                    return 99;

                case StatusEffectType.Mark:
                    return 5;

                default:
                    return int.MaxValue;
            }
        }

        // =========================================================
        // AGGRAVATED
        // =========================================================

        /// <summary>
        /// Returns the unit this combatant is forced to target
        /// because of Aggravated, or null when not Aggravated.
        /// </summary>
        public IStatusEffectOwner GetForcedTarget()
        {
            if (!HasStatus(StatusEffectType.Aggravated))
                return null;

            return GetStatus(StatusEffectType.Aggravated).Source;
        }
    }
}