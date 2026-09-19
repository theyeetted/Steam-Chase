using Steamchasing.Cards;
using UnityEngine;

namespace Steamchasing.Combat
{
    public class CardExecutor : MonoBehaviour
    {
        [SerializeField] private CombatManager combat;

        public bool TryPlay(CardInstance card, CombatUnit source, CombatUnit target)
        {
            if (card == null || card.Data == null)
                return false;

            if (!combat.TrySpendEnergy(card.CurrentCost))
                return false;

            foreach (CardEffect effect in card.Data.Effects)
            {
                if (Random.value > effect.chance)
                    continue;

                ExecuteEffect(effect, card, source, target);
            }

            return true;
        }

        private void ExecuteEffect(
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
                    source.GainBlock(effect.value);
                    break;

                case EffectType.Heal:
                    resolvedTarget.Heal(effect.value);
                    break;

                case EffectType.GainEnergy:
                    combat.GainEnergy(effect.value);
                    break;

                case EffectType.ApplyVulnerable:
                    resolvedTarget.ApplyVulnerable(effect.value);
                    break;

                case EffectType.ApplyAggravated:
                    // Hook your Aggravated status system here.
                    break;

                case EffectType.ApplyStress:
                    resolvedTarget.ApplyStress(effect.value);
                    break;

                case EffectType.ApplyShock:
                    resolvedTarget.ApplyShock(effect.value);
                    break;

                case EffectType.ApplyMark:
                    resolvedTarget.ApplyMark(effect.value);
                    break;

                case EffectType.ApplyAttackUp:
                    source.ApplyAttackUp(effect.value);
                    break;

                case EffectType.ApplyAttackDown:
                    resolvedTarget.ApplyAttackDown(effect.value);
                    break;

                case EffectType.FillStunGauge:
                    resolvedTarget.FillStunGauge(effect.value / 100f);
                    break;

                case EffectType.Draw:
                    // Connect to DeckManager.Draw(effect.value).
                    break;

                case EffectType.Exhaust:
                    // Move this CardInstance to the exhaust pile.
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
            }
        }
    }
}
