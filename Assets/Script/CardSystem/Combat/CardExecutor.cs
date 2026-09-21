using Steamchasing.Cards;
using Steamchasing.DeckSystem;
using UnityEngine;

namespace Steamchasing.Combat
{
    public class CardExecutor : MonoBehaviour
    {
        [SerializeField] private CombatManager combat;
        [SerializeField] private DeckManager deckManager;

        public bool TryPlay(CardInstance card, CombatUnit source, CombatUnit target)
        {
            if (card == null || card.Data == null)
                return false;

            if (combat == null)
            {
                Debug.LogError("CardExecutor: CombatManager is not assigned.", this);
                return false;
            }

            if (!combat.TrySpendEnergy(card.CurrentCost))
                return false;

            bool disintegrateAfterResolution = false;

            foreach (CardEffect effect in card.Data.Effects)
            {
                if (effect == null)
                    continue;

                if (Random.value > effect.chance)
                    continue;

                if (ExecuteEffect(effect, card, source, target))
                    disintegrateAfterResolution = true;
            }

            // Finish moving the played CardInstance only after every effect has
            // resolved. This prevents Disintegrate from interrupting later effects.
            if (deckManager != null)
            {
                deckManager.ResolvePlayedCard(card, disintegrateAfterResolution);
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

        /// <summary>
        /// Executes one card effect.
        /// Returns true when the played card must be removed from circulation
        /// for the rest of the current combat.
        /// </summary>
        private bool ExecuteEffect(
            CardEffect effect,
            CardInstance card,
            CombatUnit source,
            CombatUnit target)
        {
            CombatUnit resolvedTarget = target != null ? target : source;

            switch (effect.type)
            {
                case EffectType.Damage:
                    combat.DealDamage(source, resolvedTarget, effect.value);
                    break;

                case EffectType.Block:
                    source?.GainBlock(effect.value);
                    break;

                case EffectType.Heal:
                    resolvedTarget?.Heal(effect.value);
                    break;

                case EffectType.GainEnergy:
                    combat.GainEnergy(effect.value);
                    break;

                case EffectType.ApplyVulnerable:
                    resolvedTarget?.ApplyVulnerable(effect.value);
                    break;

                case EffectType.ApplyAggravated:
                    // Hook your Aggravated status system here.
                    break;

                case EffectType.ApplyStress:
                    resolvedTarget?.ApplyStress(effect.value);
                    break;

                case EffectType.ApplyShock:
                    resolvedTarget?.ApplyShock(effect.value);
                    break;

                case EffectType.ApplyMark:
                    resolvedTarget?.ApplyMark(effect.value);
                    break;

                case EffectType.ApplyAttackUp:
                    source?.ApplyAttackUp(effect.value);
                    break;

                case EffectType.ApplyAttackDown:
                    resolvedTarget?.ApplyAttackDown(effect.value);
                    break;

                case EffectType.FillStunGauge:
                    resolvedTarget?.FillStunGauge(effect.value / 100f);
                    break;

                case EffectType.Draw:
                    if (deckManager != null)
                        deckManager.Draw(effect.value);
                    break;

                case EffectType.ModifyCost:
                    card.AddTemporaryCostModifier(effect.value);
                    break;

                case EffectType.ExtraAttack:
                    combat.DealDamage(source, resolvedTarget, effect.value);
                    break;

                case EffectType.FollowUpAttack:
                    combat.DealDamage(source, resolvedTarget, effect.value);
                    break;

                case EffectType.Exhaust:
                case EffectType.Disintegrate:
                    // Steamchasing's Disintegrate rule:
                    // after the card finishes resolving, remove this CardInstance
                    // from the draw/hand/discard cycle for this combat.
                    return true;

                case EffectType.Discard:
                    // Reserved for discard-specific card effects.
                    break;

                case EffectType.Repeat:
                    // Reserved for repeat-card/effect logic.
                    break;
            }

            return false;
        }
    }
}
