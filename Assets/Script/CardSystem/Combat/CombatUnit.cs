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

        public int CurrentHP { get; private set; }
        public int Block { get; private set; }
        public float StunGauge { get; private set; }

        public bool IsStunned => StunGauge >= 1f;
        public bool WasAttackedThisTurn { get; private set; }

        public StatusEffectController StatusEffects => statusEffects;

        public int RetaliationDamage => retaliationDamage;

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

        public void TriggerFollowUpAttack(IStatusEffectOwner target, int baseDamage)
        {
            if (target == null)
                return;

            if (target is CombatUnit combatTarget)
                combatTarget.TakeDamage(baseDamage);
            else
                target.TakeDirectDamage(baseDamage);
        }
    }
}
