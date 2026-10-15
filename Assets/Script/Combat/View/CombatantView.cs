using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatantView : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private StatusEffectsUI statusEffectsUI;

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public CombatantView AggravatedApplier { get; private set; }
    public CombatantView MarkApplier { get; private set; }
    public const int MaxMarkStacks = 5;

    private Dictionary<StatusEffectType, int> statusEffects = new();
    private readonly StatusDamageBuffs statusDamageBuffs = new();
    public bool HasTimedStatusDamageBuffs => statusDamageBuffs.HasTimedBuffs;

    protected void SetupBase(int  health, Sprite image)
    {
        // Reusing a unit for a new battle must also clear permanent damage bonuses.
        foreach (StatusEffectType type in statusDamageBuffs.Clear())
            statusEffectsUI.UpdateStatusEffectUI(type, 0);
        MaxHealth = health;
        CurrentHealth = health;
        spriteRenderer.sprite = image;
        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        healthText.text = "HP: " + CurrentHealth;
    }

    public void Damage(int damageAmount)
    {
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
        transform.DOShakePosition(0.2f, 0.5f);
        UpdateHealthText();
    }

    public void AddStatusEffect(StatusEffectType type, int stackCount, CombatantView applier = null,
        int damageBuffTurns = 0)
    {
        if (StatusDamageBuffs.IsDamageBuff(type))
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
        if (StatusDamageBuffs.IsDamageBuff(type))
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
        if (StatusDamageBuffs.IsDamageBuff(type))
            return statusDamageBuffs.GetBonus(type);
        if(statusEffects.ContainsKey(type)) return statusEffects[type];
        else return 0;
    }

    public void AdvanceStatusDamageBuffs()
    {
        // Expire only the individual grants whose timers elapsed, preserving later/permanent grants.
        foreach (StatusEffectType type in statusDamageBuffs.AdvanceTurn())
            statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

}
