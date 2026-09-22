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


        // Tracks the first Attack card successfully played during the current player turn.
        private CardData firstAttackPlayedThisTurn;


        // Prevents Repeat effects from recursively repeating themselves forever.
        private bool isRepeatingCard;


        // Tracks which combat units have triggered at least one Extra Attack
        // during the current player turn. Per-source tracking prevents one
        // character's Extra Attack from satisfying another character's condition.
        private readonly HashSet<CombatUnit> extraAttackSourcesThisTurn = new();


        // Cards whose Overdrive Engine discount must be removed at turn reset.
        private readonly HashSet<CardInstance> turnCostModifiedCards = new();


        private struct EveryCardExtraAttackTrigger
        {
            public CombatUnit source;
            public int baseDamage;


            public EveryCardExtraAttackTrigger(
                CombatUnit source,
                int baseDamage)
            {
                this.source = source;
                this.baseDamage = baseDamage;
            }
        }


        // Each active entry causes every subsequently played real card this turn
        // to create one Extra Attack from the unit that activated the effect.
        private readonly List<EveryCardExtraAttackTrigger>
            everyCardExtraAttackTriggers = new();


        /// <summary>
        /// Call this at the start of every player turn.
        /// </summary>
        public void ResetTurnHistory()
        {
            firstAttackPlayedThisTurn = null;
            isRepeatingCard = false;
            extraAttackSourcesThisTurn.Clear();
            everyCardExtraAttackTriggers.Clear();


            foreach (CardInstance modifiedCard in turnCostModifiedCards)
            {
                modifiedCard?.ClearTurnCostModifiers();
            }


            turnCostModifiedCards.Clear();
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


            // Snapshot effects that were active BEFORE this card resolved.
            // If this card itself enables "every card this turn", it should not
            // retroactively trigger from its own play.
            int everyCardTriggerCountBeforePlay =
                everyCardExtraAttackTriggers.Count;


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


            // Chain Chamber-style effects trigger only from a real Attack card
            // being played through TryPlay(). Repeated/replayed card effects do
            // not pass through this step, so they do not consume charges.
            if (card.Data.CardType == CardType.Attack)
                TryTriggerQueuedAttackCardExtraAttack(source, target);


            // Overdrive Engine:
            // every real card played AFTER Overdrive has resolved this turn
            // triggers one Extra Attack for each active Overdrive-style effect.
            TriggerEveryCardExtraAttacks(
                everyCardTriggerCountBeforePlay,
                target
            );


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
        /// Consumes one queued Chain Chamber-style trigger after a real Attack
        /// card is played, then performs an Extra Attack.
        /// </summary>
        private void TryTriggerQueuedAttackCardExtraAttack(
            CombatUnit source,
            CombatUnit selectedTarget)
        {
            if (source == null)
                return;


            if (!source.TryConsumeAttackCardExtraAttack(out int baseDamage))
                return;


            CombatUnit extraAttackTarget =
                ResolveQueuedExtraAttackTarget(selectedTarget);


            if (extraAttackTarget == null)
                return;


            int finalBaseDamage =
                Mathf.Max(0, baseDamage) +
                source.ExtraAttackDamageBonus;


            // This is a real Extra Attack, so it counts for conditions such as
            // "If an Extra Attack was triggered this turn".
            extraAttackSourcesThisTurn.Add(source);


            combat.DealDamage(
                source,
                extraAttackTarget,
                finalBaseDamage
            );


            ApplyExtraAttackSecondaryEffects(
                source,
                extraAttackTarget
            );
        }


        /// <summary>
        /// Applies secondary effects that belong specifically to an Extra Attack.
        /// Concussive Caliber uses this to add Stun-gauge fill to the enemy
        /// actually struck by the Extra Attack.
        /// </summary>
        private void ApplyExtraAttackSecondaryEffects(
            CombatUnit source,
            CombatUnit target)
        {
            if (source == null || target == null)
                return;


            int stunBonusPercent =
                source.ExtraAttackStunGaugeBonusPercent;


            if (stunBonusPercent > 0)
            {
                target.FillStunGauge(
                    stunBonusPercent / 100f
                );
            }
        }


        /// <summary>
        /// Single-target Attack cards reuse their selected target.
        /// If an Attack card has no selected target (for example an AOE card),
        /// choose a random living enemy for the generated Extra Attack.
        /// </summary>
        private CombatUnit ResolveQueuedExtraAttackTarget(
            CombatUnit selectedTarget)
        {
            if (selectedTarget != null &&
                selectedTarget.CurrentHP > 0)
            {
                return selectedTarget;
            }


            if (combat == null || combat.Enemies == null)
                return null;


            List<CombatUnit> livingEnemies = new();


            foreach (CombatUnit enemy in combat.Enemies)
            {
                if (enemy != null && enemy.CurrentHP > 0)
                    livingEnemies.Add(enemy);
            }


            if (livingEnemies.Count == 0)
                return null;


            return livingEnemies[
                Random.Range(0, livingEnemies.Count)
            ];
        }


        /// <summary>
        /// Activates an Extra Attack for every subsequently played real card
        /// during the current player turn.
        /// </summary>
        private void AddEveryCardExtraAttackTrigger(
            CombatUnit source,
            int baseDamage)
        {
            if (source == null)
                return;


            everyCardExtraAttackTriggers.Add(
                new EveryCardExtraAttackTrigger(
                    source,
                    Mathf.Max(0, baseDamage)
                )
            );
        }


        /// <summary>
        /// Resolves only the trigger entries that existed before the current
        /// card began resolving. This prevents Overdrive Engine from
        /// triggering itself on the same play that activated it.
        /// </summary>
        private void TriggerEveryCardExtraAttacks(
            int activeTriggerCount,
            CombatUnit selectedTarget)
        {
            activeTriggerCount = Mathf.Clamp(
                activeTriggerCount,
                0,
                everyCardExtraAttackTriggers.Count
            );


            for (int i = 0; i < activeTriggerCount; i++)
            {
                EveryCardExtraAttackTrigger trigger =
                    everyCardExtraAttackTriggers[i];


                if (trigger.source == null)
                    continue;


                CombatUnit extraAttackTarget =
                    ResolveQueuedExtraAttackTarget(selectedTarget);


                if (extraAttackTarget == null)
                    continue;


                int finalBaseDamage =
                    trigger.baseDamage +
                    trigger.source.ExtraAttackDamageBonus;


                extraAttackSourcesThisTurn.Add(trigger.source);


                combat.DealDamage(
                    trigger.source,
                    extraAttackTarget,
                    finalBaseDamage
                );


                ApplyExtraAttackSecondaryEffects(
                    trigger.source,
                    extraAttackTarget
                );
            }
        }


        /// <summary>
        /// Reduces the cost of distinct random cards currently in hand.
        /// The played card is excluded because it is already being resolved.
        /// Discounts are stored as turn-only modifiers and are cleared by
        /// ResetTurnHistory().
        /// </summary>
        private void ReduceRandomCardsInHandForTurn(
            CardInstance playedCard,
            int cardCount,
            int costReduction)
        {
            if (deckManager == null)
            {
                Debug.LogWarning(
                    "Overdrive Engine: DeckManager is not assigned.",
                    this
                );
                return;
            }


            List<CardInstance> candidates =
                GetCurrentHandCards(playedCard);


            if (candidates.Count == 0)
                return;


            // Fisher-Yates shuffle so cards are selected without replacement.
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);


                CardInstance temp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temp;
            }


            int amount = Mathf.Max(0, costReduction);
            int count = Mathf.Min(
                Mathf.Max(0, cardCount),
                candidates.Count
            );


            for (int i = 0; i < count; i++)
            {
                CardInstance selected = candidates[i];


                selected.AddTurnCostModifier(-amount);
                turnCostModifiedCards.Add(selected);
            }
        }


        /// <summary>
        /// Gets the current DeckManager hand without hard-coding a specific
        /// public API. This supports the common existing names Hand, hand,
        /// CurrentHand/currentHand, and CardsInHand/cardsInHand.
        /// </summary>
        private List<CardInstance> GetCurrentHandCards(
            CardInstance excludedCard)
        {
            List<CardInstance> result = new();


            if (deckManager == null)
                return result;


            System.Type type = deckManager.GetType();


            string[] propertyNames =
            {
                "Hand",
                "CurrentHand",
                "CardsInHand"
            };


            foreach (string propertyName in propertyNames)
            {
                System.Reflection.PropertyInfo property =
                    type.GetProperty(
                        propertyName,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic
                    );


                if (property == null)
                    continue;


                AddCardInstancesFromEnumerable(
                    property.GetValue(deckManager)
                        as System.Collections.IEnumerable,
                    excludedCard,
                    result
                );


                if (result.Count > 0)
                    return result;
            }


            string[] fieldNames =
            {
                "hand",
                "currentHand",
                "cardsInHand"
            };


            foreach (string fieldName in fieldNames)
            {
                System.Reflection.FieldInfo field =
                    type.GetField(
                        fieldName,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic
                    );


                if (field == null)
                    continue;


                AddCardInstancesFromEnumerable(
                    field.GetValue(deckManager)
                        as System.Collections.IEnumerable,
                    excludedCard,
                    result
                );


                if (result.Count > 0)
                    return result;
            }


            Debug.LogWarning(
                "Overdrive Engine: Could not find the current hand on DeckManager. " +
                "Expose it as Hand/CurrentHand/CardsInHand, or rename the private " +
                "hand field to hand/currentHand/cardsInHand.",
                this
            );


            return result;
        }


        private void AddCardInstancesFromEnumerable(
            System.Collections.IEnumerable enumerable,
            CardInstance excludedCard,
            List<CardInstance> output)
        {
            if (enumerable == null)
                return;


            foreach (object item in enumerable)
            {
                if (item is not CardInstance card)
                    continue;


                if (card == null || card == excludedCard)
                    continue;


                if (!output.Contains(card))
                    output.Add(card);
            }
        }


        /// <summary>
        /// Returns the total Shock stacks currently active across all living enemies.
        /// </summary>
        private int GetTotalEnemyShockStacks()
        {
            if (combat == null || combat.Enemies == null)
                return 0;


            int totalShock = 0;


            foreach (CombatUnit enemy in combat.Enemies)
            {
                if (enemy == null || enemy.CurrentHP <= 0)
                    continue;


                totalShock += Mathf.Max(0, enemy.Shock);
            }


            return totalShock;
        }


        /// <summary>
        /// Discards random cards from the current hand, excluding the card that is
        /// presently resolving. Used by EffectType.Discard.
        /// </summary>
        private void DiscardRandomCardsFromHand(
            CardInstance playedCard,
            int amount)
        {
            if (deckManager == null ||
                deckManager.RuntimeDeck == null ||
                amount <= 0)
            {
                return;
            }


            List<CardInstance> candidates = new();


            foreach (CardInstance handCard in deckManager.RuntimeDeck.Hand)
            {
                if (handCard != null && handCard != playedCard)
                    candidates.Add(handCard);
            }


            int discardCount = Mathf.Min(amount, candidates.Count);


            for (int i = 0; i < discardCount; i++)
            {
                int index = Random.Range(0, candidates.Count);
                CardInstance selected = candidates[index];
                candidates.RemoveAt(index);
                deckManager.DiscardCard(selected);
            }
        }


        /// <summary>
        /// Consumes Mark from the effect's target. A value <= 0 means consume all
        /// Mark stacks. AllEnemies is supported for Focus Fire-style effects.
        /// </summary>
        private int ConsumeMarkEffect(
            CardEffect effect,
            CombatUnit source,
            CombatUnit selectedTarget)
        {
            int totalConsumed = 0;
            int requested = effect.value > 0 ? effect.value : int.MaxValue;


            if (effect.target == EffectTarget.AllEnemies &&
                combat != null && combat.Enemies != null)
            {
                foreach (CombatUnit enemy in combat.Enemies)
                {
                    if (enemy == null || enemy.CurrentHP <= 0)
                        continue;


                    int current = enemy.Mark;
                    if (current <= 0)
                        continue;


                    totalConsumed += enemy.ConsumeMark(
                        requested == int.MaxValue ? current : requested
                    );
                }


                return totalConsumed;
            }


            CombatUnit markTarget =
                effect.target == EffectTarget.Self
                    ? source
                    : (selectedTarget != null ? selectedTarget : source);


            if (markTarget == null)
                return 0;


            int available = markTarget.Mark;
            if (available <= 0)
                return 0;


            return markTarget.ConsumeMark(
                requested == int.MaxValue ? available : requested
            );
        }


        /// <summary>
        /// Immediately resolves Shock and consumes all Shock stacks. AllEnemies is
        /// supported for Short Circuit-style effects. Returns stacks consumed.
        /// </summary>
        private int TriggerShockEffect(
            CardEffect effect,
            CombatUnit source,
            CombatUnit selectedTarget)
        {
            int totalStacks = 0;


            if (effect.target == EffectTarget.AllEnemies &&
                combat != null && combat.Enemies != null)
            {
                foreach (CombatUnit enemy in combat.Enemies)
                {
                    if (enemy == null || enemy.CurrentHP <= 0)
                        continue;


                    totalStacks += enemy.TriggerShockImmediately(true);
                }


                return totalStacks;
            }


            CombatUnit shockTarget =
                effect.target == EffectTarget.Self
                    ? source
                    : (selectedTarget != null ? selectedTarget : source);


            if (shockTarget == null)
                return 0;


            return shockTarget.TriggerShockImmediately(true);
        }


        /// <summary>
        /// Checks whether an effect's condition is currently satisfied.
        /// A null/None condition always passes.
        /// </summary>
        private bool IsConditionMet(
            CardCondition condition,
            CombatUnit source,
            CombatUnit selectedTarget)
        {
            if (condition == null ||
                condition.type == CardConditionType.None)
            {
                return true;
            }


            // This condition intentionally checks the acting unit that owns/plays
            // the card. It is turn history, not a status on the target.
            if (condition.type ==
                CardConditionType.ExtraAttackTriggeredThisTurn)
            {
                return source != null &&
                       extraAttackSourcesThisTurn.Contains(source);
            }


            if (condition.subject == ConditionSubject.AnyEnemy)
            {
                if (combat == null || combat.Enemies == null)
                    return false;


                foreach (CombatUnit enemy in combat.Enemies)
                {
                    if (enemy != null &&
                        IsUnitConditionMet(condition, enemy))
                    {
                        return true;
                    }
                }


                return false;
            }


            CombatUnit subject =
                condition.subject == ConditionSubject.Self
                    ? source
                    : selectedTarget;


            return IsUnitConditionMet(condition, subject);
        }


        private bool IsUnitConditionMet(
            CardCondition condition,
            CombatUnit unit)
        {
            if (unit == null)
                return false;


            switch (condition.type)
            {
                case CardConditionType.HasStatus:
                    return unit.HasStatus(
                        condition.statusType,
                        condition.minimumStacks
                    );


                case CardConditionType.DoesNotHaveStatus:
                    return !unit.HasStatus(
                        condition.statusType,
                        condition.minimumStacks
                    );


                case CardConditionType.IsStunned:
                    return unit.IsStunned;


                case CardConditionType.WasAttackedThisTurn:
                    return unit.WasAttackedThisTurn;


                default:
                    return true;
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
            if (!IsConditionMet(effect.condition, source, target))
                return false;


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


                case EffectType.HealPerTotalEnemyShock:
                    if (source != null)
                    {
                        int totalShock = GetTotalEnemyShockStacks();
                        int healAmount =
                            totalShock * Mathf.Max(0, effect.value);


                        source.Heal(healAmount);
                    }
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


                case EffectType.ShockTickDamageUp:
                    resolvedTarget?.ApplyShockTickDamageBonus(
                        effect.value,
                        effect.statusDuration > 0
                            ? effect.statusDuration
                            : 2
                    );
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
                    int extraAttackDamage = effect.value;


                    if (source != null)
                    {
                        extraAttackDamage += source.ExtraAttackDamageBonus;
                        extraAttackSourcesThisTurn.Add(source);
                    }


                    combat.DealDamage(
                        source,
                        resolvedTarget,
                        extraAttackDamage
                    );


                    ApplyExtraAttackSecondaryEffects(
                        source,
                        resolvedTarget
                    );
                    break;


                case EffectType.ExtraAttackDamageUp:
                    source?.ApplyExtraAttackDamageBonus(effect.value);
                    break;


                case EffectType.ExtraAttackOnNextAttackCards:
                    source?.QueueExtraAttackOnNextAttackCards(
                        Mathf.Max(1, effect.repeat),
                        effect.value
                    );
                    break;


                case EffectType.ReduceRandomHandCostThisTurn:
                    ReduceRandomCardsInHandForTurn(
                        card,
                        Mathf.Max(1, effect.repeat),
                        Mathf.Abs(effect.value)
                    );
                    break;


                case EffectType.ExtraAttackEveryCardThisTurn:
                    AddEveryCardExtraAttackTrigger(
                        source,
                        effect.value
                    );
                    break;


                case EffectType.ExtraAttackStunGaugeUp:
                    source?.ApplyExtraAttackStunGaugeBonus(
                        effect.value,
                        effect.statusDuration > 0
                            ? effect.statusDuration
                            : 2
                    );
                    break;


                case EffectType.FollowUpDamageUp:
                    source?.ApplyFollowUpDamageBonus(
                        effect.value,
                        effect.statusDuration > 0
                            ? effect.statusDuration
                            : 2
                    );
                    break;


                case EffectType.MarkFollowUpDamageUp:
                    source?.ApplyMarkFollowUpDamageBonus(
                        effect.value,
                        effect.statusDuration > 0
                            ? effect.statusDuration
                            : 2
                    );
                    break;


                case EffectType.ConsumeMark:
                    ConsumeMarkEffect(effect, source, target);
                    break;


                case EffectType.FollowUpAttack:
                    int followUpDamage =
                        effect.value +
                        (source != null
                            ? source.FollowUpDamageBonus
                            : 0);


                    combat.DealDamage(
                        source,
                        resolvedTarget,
                        followUpDamage
                    );
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
                    DiscardRandomCardsFromHand(
                        card,
                        Mathf.Max(1, effect.value)
                    );
                    break;


                case EffectType.Repeat:
                    // Following Up:
                    // replay the first Attack card used during this turn.
                    RepeatFirstAttack(source, target);
                    break;
                case EffectType.DoubleStress:
                    resolvedTarget?.DoubleStress();
                    break;


                case EffectType.TriggerShock:
                    TriggerShockEffect(effect, source, target);
                    break;
            }


            return false;
        }
    }
}