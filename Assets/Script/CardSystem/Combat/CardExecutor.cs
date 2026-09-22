using Steamchasing.Cards;
using Steamchasing.DeckSystem;
using UnityEngine;

namespace Steamchasing.Combat
{
    public class CardExecutor : MonoBehaviour
    {
        [SerializeField] private CombatManager combat;
        [SerializeField] private DeckManager deckManager;

        // Tracks the first Attack card successfully played during the current player turn.
        private CardData firstAttackPlayedThisTurn;

        // Prevents Repeat effects from recursively repeating themselves forever.
        private bool isRepeatingCard;

        /// <summary>
        /// Call this at the start of every player turn.
        /// </summary>
        public void ResetTurnHistory()
        {
            firstAttackPlayedThisTurn = null;
            isRepeatingCard = false;
        }

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

            RegisterCardPlayed(card);

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
        /// Records the first Attack card played during this turn.
        /// Later Attack cards do not overwrite it.
        /// </summary>
        private void RegisterCardPlayed(CardInstance card)
        {
            if (card == null || card.Data == null)
                return;

            if (firstAttackPlayedThisTurn != null)
                return;

            if (card.Data.CardType != CardType.Attack)
                return;

            firstAttackPlayedThisTurn = card.Data;

            Debug.Log(
                $"CardExecutor: First Attack this turn is {card.Data.CardName}.",
                this
            );
        }

        /// <summary>
        /// Replays the effects of the first Attack card played this turn.
        /// This does NOT spend the repeated Attack's energy and does NOT move
        /// another physical card through the deck/discard/disintegrate piles.
        /// </summary>
        private void RepeatFirstAttack(CombatUnit source, CombatUnit target)
        {
            if (isRepeatingCard)
                return;

            if (firstAttackPlayedThisTurn == null)
            {
                Debug.Log(
                    "Following Up: No Attack has been played this turn.",
                    this
                );
                return;
            }

            isRepeatingCard = true;

            try
            {
                Debug.Log(
                    $"Following Up repeats {firstAttackPlayedThisTurn.CardName}.",
                    this
                );

                // Temporary runtime instance used only so existing effect logic
                // can safely receive a CardInstance (for effects such as ModifyCost).
                // It is never inserted into the player's deck or hand.
                CardInstance repeatedCard =
                    new CardInstance(firstAttackPlayedThisTurn);

                foreach (CardEffect effect in firstAttackPlayedThisTurn.Effects)
                {
                    if (effect == null)
                        continue;

                    if (Random.value > effect.chance)
                        continue;

                    // Ignore the returned Disintegrate/Exhaust flag here.
                    // The replay is not a real second card instance in the deck cycle.
                    ExecuteEffect(
                        effect,
                        repeatedCard,
                        source,
                        target
                    );
                }
            }
            finally
            {
                isRepeatingCard = false;
            }
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
                    resolvedTarget?.ApplyAggravated(
                        effect.value,
                        effect.statusDuration > 0 ? effect.statusDuration : 1,
                        source
                    );
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

                case EffectType.TriggerStressDetonation:
                    if (resolvedTarget != null)
                    {
                        // Detonate all current Stress immediately WITHOUT consuming it.
                        int stressDamage =
                            resolvedTarget.TriggerStressImmediately(false);

                        // effect.value is the percentage of the detonation damage
                        // converted into Stun-gauge percentage points.
                        //
                        // Example: 20 damage * 40% = 8 percentage points = 0.08 gauge.
                        float stunGaugePercentPoints =
                            stressDamage * (effect.value / 100f);

                        resolvedTarget.FillStunGauge(
                            stunGaugePercentPoints / 100f
                        );
                    }
                    break;

                case EffectType.Draw:
                    if (deckManager != null)
                        deckManager.Draw(effect.value);
                    break;

                case EffectType.ModifyCost:
                    card?.AddTemporaryCostModifier(effect.value);
                    break;

                case EffectType.ExtraAttack:
                    combat.DealDamage(source, resolvedTarget, effect.value);
                    break;

                case EffectType.FollowUpAttack:
                    combat.DealDamage(source, resolvedTarget, effect.value);
                    break;

                case EffectType.Retaliate:
                    source?.ApplyRetaliation(effect.value);
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
                    // Following Up:
                    // replay the first Attack card used during this turn.
                    RepeatFirstAttack(source, target);
                    break;
                case EffectType.DoubleStress:
                    resolvedTarget?.DoubleStress();
                    break;
            }

            return false;
        }
    }
}
