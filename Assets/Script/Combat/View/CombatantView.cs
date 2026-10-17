using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatantView : MonoBehaviour
{
    //[SerializeField] private TMP_Text healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private StatusEffectsUI statusEffectsUI;
    [SerializeField] private StunBarUI stunBarUI;
    [SerializeField] private HealthBarUI HealthBarUI;


    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    public int MaxStun { get; private set; } = 100;
    public int CurrentStun { get; private set; }
    public bool IsStunned { get; private set; }
    public bool IsStunOnCooldown { get; private set; }
    public CombatantView AggravatedApplier { get; private set; }
    public CombatantView MarkApplier { get; private set; }
    public const int MaxMarkStacks = 5;
    public const int MaxStressStacks = 99;

    public bool WasAttackedLastTurn { get; private set; }
    public bool WasAttackedThisCombat { get; private set; }
    private bool wasAttackedThisTurn;

    private Dictionary<StatusEffectType, int> statusEffects = new();
    private readonly StatusDamageBuffs statusDamageBuffs = new();
    private readonly CounterBuffs counterBuffs = new();
    public bool HasTimedStatusDamageBuffs => statusDamageBuffs.HasTimedBuffs;
    public bool HasTimedCounter => counterBuffs.HasTimedBuffs;


    protected void SetupBase(int  health, Sprite image, int maxStun)
    {
        ResetAttackHistory();
        if (counterBuffs.Damage > 0)
            RemoveStatusEffect(StatusEffectType.Counter, counterBuffs.Damage);
        // Reusing a unit for a new battle must also clear permanent damage bonuses.
        foreach (StatusEffectType type in statusDamageBuffs.Clear())
            statusEffectsUI.UpdateStatusEffectUI(type, 0);
        // Stress has no turn decay, but must not carry into a new combat on a reused enemy.
        int stressStacks = GetStatusEffectStacks(StatusEffectType.Stress);
        if (stressStacks > 0)
            RemoveStatusEffect(StatusEffectType.Stress, stressStacks);
        MaxHealth = health;
        CurrentHealth = health;
        spriteRenderer.sprite = image;
        SetupStun(maxStun);
        UpdateHealthBar();

        Debug.Log("Combatant setup Complete.");
    }

    // Record resolved attacks, including fully blocked hits. Status damage must not call this.
    public void RecordAttack()
    {
        if (!(this is EnemyView) || CurrentHealth <= 0)
            return;

        wasAttackedThisTurn = true;
        WasAttackedThisCombat = true;
    }

    public void AdvanceAttackHistory()
    {
        // Replace the snapshot even after an unattacked turn; never accumulate across turns.
        WasAttackedLastTurn = wasAttackedThisTurn;
        wasAttackedThisTurn = false;
    }

    private void ResetAttackHistory()
    {
        wasAttackedThisTurn = false;
        WasAttackedLastTurn = false;
        WasAttackedThisCombat = false;
    }

    private void UpdateHealthBar()
    {
        HealthBarUI.UpdateBar(CurrentHealth, MaxHealth);
    }

    public void Damage(int damageAmount)
    {
        Damage(damageAmount, null);
    }

    public void Damage(int damageAmount, CombatantView caster)
    {
        int healthBeforeDamage = CurrentHealth;
        int remainingDamage = damageAmount;
        int currentBlock = GetStatusEffectStacks(StatusEffectType.BLOCK);
        if (currentBlock > 0)
        {
            if (currentBlock >= damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.BLOCK, remainingDamage);
                remainingDamage = 0;
            }
            else if (currentBlock < damageAmount)
            {
                {
                    RemoveStatusEffect(StatusEffectType.BLOCK, currentBlock);
                    remainingDamage -= currentBlock;
                }
            }
        }
        if (remainingDamage > 0)
        {
            CurrentHealth -= remainingDamage;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
        }
        string casterName = caster != null ? caster.name : "Unknown caster";
        Debug.Log($"{casterName} deal {healthBeforeDamage - CurrentHealth} Damages to {name}", this);
        transform.DOShakePosition(0.2f, 0.5f);
        UpdateHealthBar();
    }


    // STUN ----------------------------------------------

    protected void SetupStun(int maxStun)
    {
        MaxStun = Mathf.Max(1, maxStun);
        CurrentStun = 0;
        IsStunned = false;
        IsStunOnCooldown = false;
        UpdateStunUI();
    }

    public void AddStun(int amount)
    {
        if (CurrentHealth <= 0 || IsStunned || amount <= 0 || IsStunOnCooldown )
            return;

        // Cap the increment before adding, so a large gauge fill cannot overflow.
        CurrentStun += Mathf.Min(amount, MaxStun - CurrentStun);
        IsStunned = CurrentStun >= MaxStun;

        UpdateStunUI();

        Debug.Log(
            $"{name}: Stun {CurrentStun}/{MaxStun}" +
            (IsStunned ? " — STUNNED!" : "")
        );
    }

    public void AddStunPercent(int percent)
    {
        percent = Mathf.Clamp(percent, 0, 100);
        // Round up exactly and use a wide intermediate for large MaxStun values.
        // AddStun owns the cap, Stunned state, cooldown/death guards, and gauge UI refresh.
        int amount = (int)(((long)MaxStun * percent + 99L) / 100L);
        AddStun(amount);
    }

    public bool ConsumeStun()
    {
        if (!IsStunned) { return false; }

        IsStunned = false;
        IsStunOnCooldown = true;
        CurrentStun = 0;

        UpdateStunUI();

        Debug.Log($"{name}: Stun consumed; cooldown started.");
        return true;
    }

    // Call when this unit's next turn begins after recovering.
    public void EndStunCooldown()
    {
        IsStunOnCooldown = false;
        Debug.Log($"{name}: Stun cooldown ended.");
        UpdateStunUI();
    }

    private void UpdateStunUI()
    {
        if (stunBarUI != null)
            stunBarUI.UpdateBar(CurrentStun, MaxStun);
    }

    // Testing Stun: In PlayerView (character) Inspector
    [ContextMenu("TEST: Add 25 Stun")]
    private void TestAdd25Stun()
    {
        AddStun(25);
    }

    [ContextMenu("TEST: Add 1 Stun")]
    private void TestAdd1Stun()
    {
        AddStun(1);
    }

    // --------------------------------------------------

    // -------------- Status Effect stack --------------

    public void AddStatusEffect(StatusEffectType type, int stackCount, CombatantView applier = null,
        int damageBuffTurns = 0)
    {
        if (type == StatusEffectType.Counter)
        {
            // Counter belongs to heroes; stacks are damage, with independent grant durations.
            if (!(this is PlayerView) || CurrentHealth <= 0 || !counterBuffs.Add(stackCount, damageBuffTurns))
                return;
        }
        else if (StatusDamageBuffs.IsDamageBuff(type))
        {
            if (CurrentHealth <= 0 || !statusDamageBuffs.Add(type, stackCount, damageBuffTurns))
                return;
        }
        else if (type == StatusEffectType.Aggravated)
        {
            // Aggravated is enemy-only and always lasts one turn, even when reapplied.
            if (!(this is EnemyView) || CurrentHealth <= 0 || stackCount <= 0 ||
                applier == null || applier == this || applier.CurrentHealth <= 0)
                return;

            AggravatedApplier = applier;
            statusEffects[type] = 1;
        }
        else if (type == StatusEffectType.Stress)
        {
            if (!(this is EnemyView) || CurrentHealth <= 0 || stackCount <= 0)
                return;

            // Add only the available capacity, avoiding overflow even for a very large grant.
            int currentStacks = Mathf.Clamp(GetStatusEffectStacks(type), 0, MaxStressStacks);
            statusEffects[type] = currentStacks + Mathf.Min(stackCount, MaxStressStacks - currentStacks);
        }
        else if (type == StatusEffectType.Mark)
        {
            if (CurrentHealth <= 0 || stackCount <= 0 || applier == null || applier.CurrentHealth <= 0)
                return;

            // Reapplication transfers all remaining stacks to the latest applier.
            // Limit the increment before adding, so even a very large grant cannot overflow.
            MarkApplier = applier;
            int currentStacks = Mathf.Clamp(GetStatusEffectStacks(type), 0, MaxMarkStacks);
            statusEffects[type] = currentStacks + Mathf.Min(stackCount, MaxMarkStacks - currentStacks);
        }
        else if (statusEffects.ContainsKey(type)) 
        {
            statusEffects[type] += stackCount;
        }
        else
        {
            statusEffects.Add(type, stackCount);
        }
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

    public void RemoveStatusEffect(StatusEffectType type, int stackCount)
    {
        if (type == StatusEffectType.Counter)
        {
            counterBuffs.Remove(stackCount);
        }
        else if (StatusDamageBuffs.IsDamageBuff(type))
        {
            statusDamageBuffs.Remove(type, stackCount);
        }
        else if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] -= stackCount;
            if (statusEffects[type] <= 0)
            {
                statusEffects.Remove(type);
                if (type == StatusEffectType.Aggravated)
                    AggravatedApplier = null;
                if (type == StatusEffectType.Mark)
                    MarkApplier = null;
            }
        }
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

    public int GetStatusEffectStacks(StatusEffectType type)
    {
        if (type == StatusEffectType.Counter)
            return counterBuffs.Damage;
        if (StatusDamageBuffs.IsDamageBuff(type))
            return statusDamageBuffs.GetBonus(type);
        if(statusEffects.ContainsKey(type)) return statusEffects[type];
        else return 0;
    }

    public void AdvanceCounterTurn()
    {
        counterBuffs.AdvanceTurn();
        statusEffectsUI.UpdateStatusEffectUI(StatusEffectType.Counter, counterBuffs.Damage);
    }

    public void AdvanceStatusDamageBuffs()
    {
        // Expire only the individual grants whose timers elapsed, preserving later/permanent grants.
        foreach (StatusEffectType type in statusDamageBuffs.AdvanceTurn())
            statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

    //----------------------------------------------------

    // Reset, used in MatchSetupSystem.cs
    public void ResetCombatant()
    {
        ResetAttackHistory();
        counterBuffs.Clear();
        CurrentHealth = MaxHealth;
        statusEffects.Clear();

        SetupStun(MaxStun);
        UpdateHealthBar();

        foreach (StatusEffectType type in System.Enum.GetValues(typeof(StatusEffectType)))
        {
            statusEffectsUI.UpdateStatusEffectUI(type, 0);
        }
    }

}
