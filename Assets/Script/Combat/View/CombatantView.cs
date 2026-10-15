using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatantView : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private StatusEffectsUI statusEffectsUI;
    [SerializeField] private StunBarUI stunBarUI;

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    public int MaxStun { get; private set; } = 100;
    public int CurrentStun { get; private set; }
    public bool IsStunned { get; private set; }
    public bool IsStunOnCooldown { get; private set; }

    private Dictionary<StatusEffectType, int> statusEffects = new();

    protected void SetupBase(int  health, Sprite image, int maxStun)
    {
        MaxHealth = health;
        CurrentHealth = health;
        spriteRenderer.sprite = image;
        SetupStun(maxStun);
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

        CurrentStun = Mathf.Min(CurrentStun + amount, MaxStun);
        IsStunned = CurrentStun >= MaxStun;

        UpdateStunUI();

        Debug.Log(
            $"{name}: Stun {CurrentStun}/{MaxStun}" +
            (IsStunned ? " — STUNNED!" : "")
        );
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

    //----------------------------------------------------

    // Reset, used in MatchSetupSystem.cs
    public void ResetCombatant()
    {
        CurrentHealth = MaxHealth;
        statusEffects.Clear();

        SetupStun(MaxStun);
        UpdateHealthText();

        foreach (StatusEffectType type in System.Enum.GetValues(typeof(StatusEffectType)))
        {
            statusEffectsUI.UpdateStatusEffectUI(type, 0);
        }
    }

}
