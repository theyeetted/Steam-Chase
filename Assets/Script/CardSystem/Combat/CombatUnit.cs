using System.Collections.Generic;
using UnityEngine;

namespace Steamchasing.Combat
{
    public class CombatUnit : MonoBehaviour, IStatusEffectOwner
    {
        [Header("Health")]
        [SerializeField] private int maxHP = 80;

        [Header("Status System")]
        [SerializeField] private StatusEffectController statusEffects;

        // Fallback status storage keeps the card system usable even if a test unit
        // does not yet have StatusEffectController attached.
        private int fallbackVulnerable;
        private int fallbackAttackUp;
        private int fallbackAttackDown;
        private int fallbackAggravated;
        private int fallbackStress;
        private int fallbackShock;
        private int fallbackMark;

        // Combat-long passive used by Overheated Plating.
        private int retaliationDamage;

        // Bonus damage added to every Extra Attack triggered by this unit.
        // Multiple applications stack for the current combat.
        private int extraAttackDamageBonus;

        // Each entry represents one future Attack-card play that will create
        // an Extra Attack. The stored int is that Extra Attack's base damage.
        // A queue supports multiple Chain Chamber applications cleanly.
        private readonly Queue<int> queuedAttackCardExtraAttacks = new();

        // Timed bonuses that add Stun-gauge fill whenever this unit triggers
        // an Extra Attack. Each application tracks its own remaining duration.
        private sealed class TimedExtraAttackStunBonus
        {
            public int percentPoints;
            public int turnsRemaining;

            public TimedExtraAttackStunBonus(
                int percentPoints,
                int turnsRemaining)
            {
                this.percentPoints = percentPoints;
                this.turnsRemaining = turnsRemaining;
            }
        }

        private readonly List<TimedExtraAttackStunBonus>
            extraAttackStunBonuses = new();

        // Magnetic Field-style modifier. Each entry adds flat damage to the
        // whole Shock tick event, not per Shock stack.
        private sealed class TimedShockTickDamageBonus
        {
            public int damage;
            public int turnsRemaining;

            public TimedShockTickDamageBonus(
                int damage,
                int turnsRemaining)
            {
                this.damage = damage;
                this.turnsRemaining = turnsRemaining;
            }
        }

        private readonly List<TimedShockTickDamageBonus>
            shockTickDamageBonuses = new();

        // Timed bonus damage applied only to Follow-Up attacks.
        private sealed class TimedFollowUpDamageBonus
        {
            public int damage;
            public int turnsRemaining;

            public TimedFollowUpDamageBonus(
                int damage,
                int turnsRemaining)
            {
                this.damage = damage;
                this.turnsRemaining = turnsRemaining;
            }
        }

        private readonly List<TimedFollowUpDamageBonus>
            followUpDamageBonuses = new();

        // Timed bonus damage that applies only to Follow-Up attacks created
        // by consuming Mark. This stacks with the general Follow-Up bonus.
        private sealed class TimedMarkFollowUpDamageBonus
        {
            public int damage;
            public int turnsRemaining;

            public TimedMarkFollowUpDamageBonus(int damage, int turnsRemaining)
            {
                this.damage = damage;
                this.turnsRemaining = turnsRemaining;
            }
        }

        private readonly List<TimedMarkFollowUpDamageBonus>
            markFollowUpDamageBonuses = new();

        public int CurrentHP { get; private set; }
        public int Block { get; private set; }
        public float StunGauge { get; private set; }

        public bool IsStunned => StunGauge >= 1f;
        public bool WasAttackedThisTurn { get; private set; }

        public StatusEffectController StatusEffects => statusEffects;

        public int RetaliationDamage => retaliationDamage;
        public int ExtraAttackDamageBonus => extraAttackDamageBonus;

        public int ExtraAttackStunGaugeBonusPercent
        {
            get
            {
                int total = 0;

                foreach (TimedExtraAttackStunBonus bonus in extraAttackStunBonuses)
                    total += bonus.percentPoints;

                return total;
            }
        }

        public int ShockTickDamageBonus
        {
            get
            {
                int total = 0;

                foreach (TimedShockTickDamageBonus bonus in shockTickDamageBonuses)
                    total += bonus.damage;

                return total;
            }
        }

        public int FollowUpDamageBonus
        {
            get
            {
                int total = 0;

                foreach (TimedFollowUpDamageBonus bonus in followUpDamageBonuses)
                    total += bonus.damage;

                return total;
            }
        }

        public int MarkFollowUpDamageBonus
        {
            get
            {
                int total = 0;

                foreach (TimedMarkFollowUpDamageBonus bonus in markFollowUpDamageBonuses)
                    total += bonus.damage;

                return total;
            }
        }

        // Compatibility properties for code that still reads the old CombatUnit fields.
        public int AttackUp => GetStatusStacks(StatusEffectType.AttackUp);
        public int Vulnerable => GetStatusStacks(StatusEffectType.Vulnerable);
        public int Stress => GetStatusStacks(StatusEffectType.Stress);
        public int Shock => GetStatusStacks(StatusEffectType.Shock);
        public int Mark => GetStatusStacks(StatusEffectType.Mark);
        public int AttackDown => GetStatusStacks(StatusEffectType.AttackDown);
        public int Aggravated => GetStatusStacks(StatusEffectType.Aggravated);

        private void Awake()
        {
            CurrentHP = maxHP;
            retaliationDamage = 0;
            extraAttackDamageBonus = 0;
            queuedAttackCardExtraAttacks.Clear();
            extraAttackStunBonuses.Clear();
            shockTickDamageBonuses.Clear();
            followUpDamageBonuses.Clear();
            markFollowUpDamageBonuses.Clear();

            if (statusEffects == null)
                statusEffects = GetComponent<StatusEffectController>();
        }

        public void TakeDamage(int amount)
        {
            amount = Mathf.Max(0, amount);

            if (amount > 0)
                WasAttackedThisTurn = true;

            int damage = Mathf.Max(0, amount - Block);
            Block = Mathf.Max(0, Block - amount);
            CurrentHP = Mathf.Max(0, CurrentHP - damage);
        }

        // Used by Shock / Stress. This bypasses Block and normal attack modifiers.
        public void TakeDirectDamage(int damage)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - Mathf.Max(0, damage));
        }

        public void GainBlock(int amount)
        {
            Block += Mathf.Max(0, amount);
        }

        public void Heal(int amount)
        {
            CurrentHP = Mathf.Min(maxHP, CurrentHP + Mathf.Max(0, amount));
        }

        public void FillStunGauge(float percent)
        {
            StunGauge = Mathf.Clamp01(StunGauge + Mathf.Max(0f, percent));
        }

        public void ResetStunGauge()
        {
            StunGauge = 0f;
        }

        public void ResetTurnFlags()
        {
            WasAttackedThisTurn = false;

            // Advance Concussive Caliber-style temporary bonuses once per
            // player turn. A 2-turn bonus applied this turn remains active
            // for the rest of this turn and the following turn.
            for (int i = extraAttackStunBonuses.Count - 1; i >= 0; i--)
            {
                extraAttackStunBonuses[i].turnsRemaining--;

                if (extraAttackStunBonuses[i].turnsRemaining <= 0)
                    extraAttackStunBonuses.RemoveAt(i);
            }

            // Magnetic Field lasts through two enemy Shock ticks when applied
            // during the player's turn: first tick this round, second next round.
            for (int i = shockTickDamageBonuses.Count - 1; i >= 0; i--)
            {
                shockTickDamageBonuses[i].turnsRemaining--;

                if (shockTickDamageBonuses[i].turnsRemaining <= 0)
                    shockTickDamageBonuses.RemoveAt(i);
            }

            // Follow-Up damage bonuses use the same turn-based duration model.
            // A 2-turn bonus applied during this turn remains active for this
            // turn and the following turn.
            for (int i = followUpDamageBonuses.Count - 1; i >= 0; i--)
            {
                followUpDamageBonuses[i].turnsRemaining--;

                if (followUpDamageBonuses[i].turnsRemaining <= 0)
                    followUpDamageBonuses.RemoveAt(i);
            }


            for (int i = markFollowUpDamageBonuses.Count - 1; i >= 0; i--)
            {
                markFollowUpDamageBonuses[i].turnsRemaining--;

                if (markFollowUpDamageBonuses[i].turnsRemaining <= 0)
                    markFollowUpDamageBonuses.RemoveAt(i);
            }
        }

        /// <summary>
        /// Adds combat-long retaliation damage.
        /// Playing multiple retaliation effects stacks their damage.
        /// </summary>
        public void ApplyRetaliation(int damage)
        {
            retaliationDamage += Mathf.Max(0, damage);
        }

        /// <summary>
        /// Adds damage to every Extra Attack triggered by this unit.
        /// Multiple applications stack until combat passives are cleared.
        /// </summary>
        public void ApplyExtraAttackDamageBonus(int damage)
        {
            extraAttackDamageBonus += Mathf.Max(0, damage);
        }

        /// <summary>
        /// Adds temporary damage to every Follow-Up attack triggered by this unit.
        /// Multiple active bonuses stack.
        /// </summary>
        public void ApplyFollowUpDamageBonus(
            int damage,
            int durationTurns)
        {
            damage = Mathf.Max(0, damage);
            durationTurns = Mathf.Max(1, durationTurns);

            if (damage <= 0)
                return;

            followUpDamageBonuses.Add(
                new TimedFollowUpDamageBonus(
                    damage,
                    durationTurns
                )
            );
        }


        public void ApplyMarkFollowUpDamageBonus(int damage, int durationTurns)
        {
            damage = Mathf.Max(0, damage);
            durationTurns = Mathf.Max(1, durationTurns);

            if (damage <= 0)
                return;

            markFollowUpDamageBonuses.Add(
                new TimedMarkFollowUpDamageBonus(damage, durationTurns)
            );
        }

        /// <summary>
        /// Adds temporary Stun-gauge fill to every Extra Attack triggered by
        /// this unit. percentPoints uses card-text percentage points, so 25
        /// means +25% of the Stun gauge.
        /// </summary>
        public void ApplyExtraAttackStunGaugeBonus(
            int percentPoints,
            int durationTurns)
        {
            percentPoints = Mathf.Max(0, percentPoints);
            durationTurns = Mathf.Max(1, durationTurns);

            if (percentPoints <= 0)
                return;

            extraAttackStunBonuses.Add(
                new TimedExtraAttackStunBonus(
                    percentPoints,
                    durationTurns
                )
            );
        }

        /// <summary>
        /// Adds flat bonus damage to each Shock tick event on this unit.
        /// Example: 3 Shock normally deals 12. With +4, it deals 16.
        /// </summary>
        public void ApplyShockTickDamageBonus(
            int damage,
            int durationTurns)
        {
            damage = Mathf.Max(0, damage);
            durationTurns = Mathf.Max(1, durationTurns);

            if (damage <= 0)
                return;

            shockTickDamageBonuses.Add(
                new TimedShockTickDamageBonus(
                    damage,
                    durationTurns
                )
            );
        }

        /// <summary>
        /// Called by the Shock tick resolver after calculating its normal
        /// stack-based damage.
        /// </summary>
        public int ModifyShockTickDamage(int baseDamage)
        {
            return Mathf.Max(
                0,
                baseDamage + ShockTickDamageBonus
            );
        }

        /// <summary>
        /// Queues an Extra Attack for each of the next N Attack cards
        /// actually played by this unit.
        /// </summary>
        public void QueueExtraAttackOnNextAttackCards(
            int attackCardCount,
            int extraAttackBaseDamage)
        {
            attackCardCount = Mathf.Max(0, attackCardCount);
            extraAttackBaseDamage = Mathf.Max(0, extraAttackBaseDamage);

            for (int i = 0; i < attackCardCount; i++)
                queuedAttackCardExtraAttacks.Enqueue(extraAttackBaseDamage);
        }

        /// <summary>
        /// Consumes one queued Attack-card trigger.
        /// Returns false when no Chain Chamber-style trigger is armed.
        /// </summary>
        public bool TryConsumeAttackCardExtraAttack(out int baseDamage)
        {
            if (queuedAttackCardExtraAttacks.Count <= 0)
            {
                baseDamage = 0;
                return false;
            }

            baseDamage = queuedAttackCardExtraAttacks.Dequeue();
            return true;
        }

        public int QueuedAttackCardExtraAttackCount =>
            queuedAttackCardExtraAttacks.Count;

        /// <summary>
        /// Called after this unit is attacked.
        /// Retaliation still triggers when Block absorbs the incoming attack.
        /// </summary>
        public void TriggerRetaliation(
            CombatUnit attacker,
            CombatManager combat)
        {
            if (retaliationDamage <= 0)
                return;

            if (attacker == null || combat == null)
                return;

            // A defeated unit does not retaliate.
            if (CurrentHP <= 0)
                return;

            combat.DealRetaliationDamage(
                this,
                attacker,
                retaliationDamage
            );
        }

        /// <summary>
        /// Call when combat ends if CombatUnit objects persist between battles.
        /// </summary>
        public void ClearCombatPassives()
        {
            retaliationDamage = 0;
            extraAttackDamageBonus = 0;
            queuedAttackCardExtraAttacks.Clear();
            extraAttackStunBonuses.Clear();
            shockTickDamageBonuses.Clear();
            followUpDamageBonuses.Clear();
            markFollowUpDamageBonuses.Clear();
        }

        public int GetStatusStacks(StatusEffectType type)
        {
            if (statusEffects != null)
                return statusEffects.GetStacks(type);

            switch (type)
            {
                case StatusEffectType.Vulnerable: return fallbackVulnerable;
                case StatusEffectType.AttackUp: return fallbackAttackUp;
                case StatusEffectType.AttackDown: return fallbackAttackDown;
                case StatusEffectType.Aggravated: return fallbackAggravated;
                case StatusEffectType.Stress: return fallbackStress;
                case StatusEffectType.Shock: return fallbackShock;
                case StatusEffectType.Mark: return fallbackMark;
                default: return 0;
            }
        }

        public bool HasStatus(StatusEffectType type, int minimumStacks = 1)
        {
            return GetStatusStacks(type) >= Mathf.Max(1, minimumStacks);
        }

        public void ApplyStatus(
            StatusEffectType type,
            int stacks,
            int duration = -1,
            CombatUnit source = null)
        {
            stacks = Mathf.Max(0, stacks);
            if (stacks == 0)
                return;

            if (statusEffects != null)
            {
                statusEffects.ApplyStatus(type, stacks, duration, source);
                return;
            }

            switch (type)
            {
                case StatusEffectType.Vulnerable:
                    fallbackVulnerable += stacks;
                    break;

                case StatusEffectType.AttackUp:
                    fallbackAttackUp += stacks;
                    break;

                case StatusEffectType.AttackDown:
                    fallbackAttackDown += stacks;
                    break;

                case StatusEffectType.Aggravated:
                    fallbackAggravated += stacks;
                    break;

                case StatusEffectType.Stress:
                    fallbackStress = Mathf.Clamp(fallbackStress + stacks, 0, 99);
                    break;

                case StatusEffectType.Shock:
                    fallbackShock = Mathf.Clamp(fallbackShock + stacks, 0, 99);
                    break;

                case StatusEffectType.Mark:
                    fallbackMark = Mathf.Clamp(fallbackMark + stacks, 0, 5);
                    break;
            }
        }

        // Compatibility helpers used by older card-execution code.
        public void ApplyVulnerable(int amount) =>
            ApplyStatus(StatusEffectType.Vulnerable, amount);

        public void ApplyAttackUp(int amount) =>
            ApplyStatus(StatusEffectType.AttackUp, amount);

        public void ApplyAttackDown(int amount) =>
            ApplyStatus(StatusEffectType.AttackDown, amount);

        public void ApplyAggravated(int amount, int duration = 1, CombatUnit source = null) =>
            ApplyStatus(StatusEffectType.Aggravated, amount, duration, source);

        public void ApplyStress(int amount) =>
            ApplyStatus(StatusEffectType.Stress, amount);

        public void DoubleStress()
        {
            int currentStress = GetStatusStacks(StatusEffectType.Stress);

            if (currentStress <= 0 || currentStress >= 99)
                return;

            int doubledStress = Mathf.Min(currentStress * 2, 99);
            int additionalStress = doubledStress - currentStress;

            ApplyStatus(StatusEffectType.Stress, additionalStress);
        }

        public void ApplyShock(int amount) =>
            ApplyStatus(StatusEffectType.Shock, amount);

        /// <summary>
        /// Immediately detonates current Stress and returns the actual damage dealt.
        /// When consumeStacks is false, Stress remains unchanged.
        /// Includes the native +20% Stress damage bonus if this unit is Stunned.
        /// </summary>
        public int TriggerStressImmediately(bool consumeStacks)
        {
            if (statusEffects != null)
                return statusEffects.TriggerStressImmediately(consumeStacks);

            int stressStacks = Mathf.Max(0, fallbackStress);

            if (stressStacks <= 0)
                return 0;

            float damage = stressStacks;

            if (IsStunned)
                damage *= 1.20f;

            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(damage));

            TakeDirectDamage(finalDamage);

            if (consumeStacks)
                fallbackStress = 0;

            return finalDamage;
        }

        public void ApplyMark(int amount) =>
            ApplyStatus(StatusEffectType.Mark, amount);


        public int ConsumeMark(int amount)
        {
            int currentMark = GetStatusStacks(StatusEffectType.Mark);
            if (currentMark <= 0 || amount <= 0) return 0;

            int consumed = Mathf.Min(currentMark, amount);
            if (statusEffects != null)
                statusEffects.RemoveStacks(StatusEffectType.Mark, consumed);
            else
                fallbackMark = Mathf.Max(0, fallbackMark - consumed);

            return consumed;
        }

        public int TriggerShockImmediately(bool consumeAllStacks)
        {
            if (statusEffects != null)
                return statusEffects.TriggerShockImmediately(consumeAllStacks);

            int stacks = Mathf.Max(0, fallbackShock);
            if (stacks <= 0) return 0;

            int damage = ModifyShockTickDamage(stacks * 4);
            TakeDirectDamage(damage);

            if (consumeAllStacks) fallbackShock = 0;
            return stacks;
        }

        public void TriggerFollowUpAttack(IStatusEffectOwner target, int baseDamage)
        {
            if (target == null)
                return;

            int followUpDamage =
                Mathf.Max(0, baseDamage) +
                FollowUpDamageBonus;

            if (target is CombatUnit combatTarget)
                combatTarget.TakeDamage(followUpDamage);
            else
                target.TakeDirectDamage(followUpDamage);
        }


        public void TriggerMarkFollowUpAttack(IStatusEffectOwner target, int baseDamage)
        {
            if (target == null) return;

            int followUpDamage = Mathf.Max(0, baseDamage) +
                                 FollowUpDamageBonus +
                                 MarkFollowUpDamageBonus;

            if (target is CombatUnit combatTarget)
                combatTarget.TakeDamage(followUpDamage);
            else
                target.TakeDirectDamage(followUpDamage);
        }
    }
}