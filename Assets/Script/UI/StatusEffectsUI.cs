using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;

public class StatusEffectsUI : MonoBehaviour
{
    [SerializeField] private StatusEffectUI statusEffectUIPrefab;
    [SerializeField] private Sprite vulnerableSprite, shockSprite;
    private Dictionary<StatusEffectType, StatusEffectUI> statusEffectUIs = new();

    public void UpdateStatusEffectUI(StatusEffectType statusEffectType, int stackCount)
    {
        if (stackCount == 0)
        {
            if (statusEffectUIs.ContainsKey(statusEffectType))
            {
                var statusEffectUI = statusEffectUIs[statusEffectType];
                statusEffectUIs.Remove(statusEffectType);
                Destroy(statusEffectUI.gameObject);
            }
        }
        else
        {
            if (!statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI = Instantiate(statusEffectUIPrefab, transform);
                statusEffectUIs.Add(statusEffectType, statusEffectUI);
            }
            Sprite sprite = GetSpriteByType(statusEffectType);
            statusEffectUIs[statusEffectType].Set(sprite, stackCount);
        }
    }

    private Sprite GetSpriteByType(StatusEffectType statusEffectType)
    {
        return statusEffectType switch
        {
            StatusEffectType.VULNERABLE => vulnerableSprite,
            StatusEffectType.SHOCK => shockSprite,
            _ => null,
        };
    }
}
