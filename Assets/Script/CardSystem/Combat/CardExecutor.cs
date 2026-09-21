using System.Collections.Generic;
using Steamchasing.Cards;
using Steamchasing.DeckSystem;
using UnityEngine;

namespace Steamchasing.Combat
{
    public class CardExecutor : MonoBehaviour
    {
        [SerializeField] private CombatManager combat;
        [SerializeField] private DeckManager deckManager;

        public bool TryPlay(
            CardInstance card,
            CombatUnit source,
            CombatUnit target)
        {
            if (card == null || card.Data == null)
                return false;

            if (combat == null)
            {
                Debug.LogError(
                    "CardExecutor: CombatManager is not assigned.",
                    this
                );
                return false;
            }

            if (!combat.TrySpendEnergy(card.CurrentCost))
                return false;

            bool disintegrateAfterResolution = false;

            foreach (CardEffect effect in card.Data.Effects)
            {
                if (effect == null)
                    continue;

                // Conditions are checked once before the effect begins resolving.
                // This is important for effects such as:
                // "If target has Vulnerable, attack another time."
                if (!CheckCondition(effect.condition, source, target))
                    continue;

                int repeatCount = Mathf.Max(1, effect.repeat);

                for (int i = 0; i < repeatCount; i++)
                {
                    // Chance is rolled for each repetition. This supports effects such
                    // as "attack 6 times; each hit has a 50% chance to apply Attack Down."
                    if (Random.value > Mathf.Clamp01(effect.chance))
                        continue;

                    if (ExecuteEffect(effect, card, source, target))
                        disintegrateAfterResolution = true;
                }
            }

            // Move the played card only after every effect has resolved.
            if (deckManager != null)
            {
                deckManager.ResolvePlayedCard(
                    card,
                    disintegrateAfterResolution
                );
            }
            else
            {
                Debug.LogWarning(
                    "CardExecutor: DeckManager is not assigned, so the played card cannot be moved to discard/disintegrated pile.",
                    this
                );
            }

            return true;
        }

        private bool ExecuteEffect(
            CardEffect effect,
            CardInstance card,
            CombatUnit source,
            CombatUnit selectedTarget)
        {
            // Effects that do not require a CombatUnit target.
            switch (effect.type)
            {
                case EffectType.Draw:
                    if (deckManager != null)
                        deckManager.Draw(Mathf.Max(0, effect.value));
                    return false;

                case EffectType.GainEnergy:
                    combat.GainEnergy(effect.value);
                    return false;

                case EffectType.ModifyCost:
                    card.AddTemporaryCostModifier(effect.value);
                    return false;

                case EffectType.Exhaust:
                case EffectType.Disintegrate:
                    return true;

                case EffectType.Discard:
                    // Reserved for discard-specific effects.
                    return false;

                case EffectType.Repeat:
                    // The CardEffect.repeat field now handles repeated resolution.
                    // This enum entry is retained for compatibility with existing assets.
                    return false;
            }

            List<CombatUnit> targets = ResolveTargets(
                effect.target,
                source,
                selectedTarget
            );

            foreach (CombatUnit resolvedTarget in targets)
            {
                if (resolvedTarget == null)
                    continue;

                ExecuteOnTarget(
                    effect,
                    source,
                    resolvedTarget
                );
            }

            return false;
        }

        private void ExecuteOnTarget(
            CardEffect effect,
            CombatUnit source,
            CombatUnit target)
        {
            switch (effect.type)
            {
                case EffectType.Damage:
                    combat.DealDamage(source, target, effect.value);
                    break;

                case EffectType.Block:
                    target.GainBlock(effect.value);
                    break;

                case EffectType.Heal:
                    target.Heal(effect.value);
                    break;

                case EffectType.ApplyVulnerable:
                    ApplyStatus(
                        target,
                        StatusEffectType.Vulnerable,
                        effect,
                        source
                    );
                    break;

                case EffectType.ApplyAggravated:
                    ApplyStatus(
                        target,
                        StatusEffectType.Aggravated,
                        effect,
                        source,
                        effect.statusDuration > 0 ? effect.statusDuration : 1
                    );
                    break;

                case EffectType.ApplyStress:
                    ApplyStatus(
                        target,
                        StatusEffectType.Stress,
                        effect,
                        source
                    );
                    break;

                case EffectType.ApplyShock:
                    ApplyStatus(
                        target,
                        StatusEffectType.Shock,
                        effect,
                        source
                    );
                    break;

                case EffectType.ApplyMark:
                    ApplyStatus(
                        target,
                        StatusEffectType.Mark,
                        effect,
                        source
                    );
                    break;

                case EffectType.ApplyAttackUp:
                    ApplyStatus(
                        target,
                        StatusEffectType.AttackUp,
                        effect,
                        source
                    );
                    break;

                case EffectType.ApplyAttackDown:
                    ApplyStatus(
                        target,
                        StatusEffectType.AttackDown,
                        effect,
                        source
                    );
                    break;

                case EffectType.FillStunGauge:
                    target.FillStunGauge(effect.value / 100f);
                    break;

                case EffectType.ExtraAttack:
                    combat.DealDamage(source, target, effect.value);
                    break;

                case EffectType.FollowUpAttack:
                    combat.DealDamage(source, target, effect.value);
                    break;
            }
        }

        private void ApplyStatus(
            CombatUnit target,
            StatusEffectType type,
            CardEffect effect,
            CombatUnit source,
            int? overrideDuration = null)
        {
            if (target == null)
                return;

            int duration = overrideDuration ?? effect.statusDuration;

            target.ApplyStatus(
                type,
                effect.value,
                duration,
                source
            );
        }

        private bool CheckCondition(
            CardCondition condition,
            CombatUnit source,
            CombatUnit selectedTarget)
        {
            if (condition == null ||
                condition.type == CardConditionType.None)
            {
                return true;
            }

            switch (condition.subject)
            {
                case ConditionSubject.Self:
                    return CheckConditionOnUnit(condition, source);

                case ConditionSubject.SelectedTarget:
                    return CheckConditionOnUnit(
                        condition,
                        selectedTarget
                    );

                case ConditionSubject.AnyEnemy:
                    if (combat == null || combat.Enemies == null)
                        return false;

                    foreach (CombatUnit enemy in combat.Enemies)
                    {
                        if (CheckConditionOnUnit(condition, enemy))
                            return true;
                    }

                    return false;
            }

            return false;
        }

        private bool CheckConditionOnUnit(
            CardCondition condition,
            CombatUnit unit)
        {
            if (unit == null)
                return false;

            switch (condition.type)
            {
                case CardConditionType.HasStatus:
                    return unit.GetStatusStacks(condition.statusType) >=
                           Mathf.Max(1, condition.minimumStacks);

                case CardConditionType.DoesNotHaveStatus:
                    return unit.GetStatusStacks(condition.statusType) <
                           Mathf.Max(1, condition.minimumStacks);

                case CardConditionType.IsStunned:
                    return unit.IsStunned;

                case CardConditionType.WasAttackedThisTurn:
                    return unit.WasAttackedThisTurn;

                case CardConditionType.None:
                default:
                    return true;
            }
        }

        private List<CombatUnit> ResolveTargets(
            EffectTarget targetType,
            CombatUnit source,
            CombatUnit selectedTarget)
        {
            List<CombatUnit> result = new();

            switch (targetType)
            {
                case EffectTarget.Self:
                    AddUnique(result, source);
                    break;

                case EffectTarget.SelectedTarget:
                    AddUnique(result, selectedTarget);
                    break;

                case EffectTarget.AllEnemies:
                    if (combat != null && combat.Enemies != null)
                    {
                        foreach (CombatUnit enemy in combat.Enemies)
                            AddUnique(result, enemy);
                    }

                    // Useful for simple one-enemy test scenes before the enemy list
                    // has been wired into CombatManager.
                    if (result.Count == 0)
                        AddUnique(result, selectedTarget);
                    break;

                case EffectTarget.RandomEnemy:
                    if (combat != null && combat.Enemies != null)
                    {
                        List<CombatUnit> validEnemies = new();

                        foreach (CombatUnit enemy in combat.Enemies)
                        {
                            if (enemy != null)
                                validEnemies.Add(enemy);
                        }

                        if (validEnemies.Count > 0)
                        {
                            AddUnique(
                                result,
                                validEnemies[Random.Range(0, validEnemies.Count)]
                            );
                        }
                    }

                    if (result.Count == 0)
                        AddUnique(result, selectedTarget);
                    break;
            }

            return result;
        }

        private static void AddUnique(
            List<CombatUnit> list,
            CombatUnit unit)
        {
            if (unit != null && !list.Contains(unit))
                list.Add(unit);
        }
    }
}
