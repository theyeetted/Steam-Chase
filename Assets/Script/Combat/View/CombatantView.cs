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

    private Dictionary<StatusEffectType, int> statusEffects = new();
    protected void SetupBase(int  health, Sprite image)
    {
        MaxHealth = health;
        CurrentHealth = health;
        spriteRenderer.sprite = image;
        UpdateHealthText();

        Debug.Log("Combatant setup Complete.");
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

    public void AddStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type)) 
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
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] -= stackCount;
            if (statusEffects[type] <= 0)
            {
                statusEffects.Remove(type);
            }
        }
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

    public int GetStatusEffectStacks(StatusEffectType type)
    {
        if(statusEffects.ContainsKey(type)) return statusEffects[type];
        else return 0;
    }

    // Reset, used in MatchSetupSystem.cs
    public void ResetCombatant()
    {
        CurrentHealth = MaxHealth;
        statusEffects.Clear();

        UpdateHealthText();

        foreach (StatusEffectType type in System.Enum.GetValues(typeof(StatusEffectType)))
        {
            statusEffectsUI.UpdateStatusEffectUI(type, 0);
        }
    }

}
