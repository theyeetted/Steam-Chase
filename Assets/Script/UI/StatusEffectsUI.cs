using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;

public class StatusEffectsUI : MonoBehaviour
{
    [SerializeField] private StatusEffectUI statusEffectUIPrefab;
    // Add status effect here vvv
    [SerializeField] private Sprite vulnerableSprite, shockSprite, attackUpSprite, BlockSprite;
    [SerializeField] private Sprite attackDownSprite;
    [SerializeField] private Sprite aggravatedSprite;
    [SerializeField] private Sprite followUpAttackSprite;
    [SerializeField] private Sprite markSprite;
    [SerializeField] private Sprite extraAttackSprite;
    [SerializeField] private Sprite stressSprite;
    [SerializeField] private Sprite counterSprite;
    [Tooltip("Got 'em icon. Its stack count shows cards drawn per enemy Vulnerable application.")]
    [SerializeField] private Sprite drawOnVulnerableSprite;
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
            StatusEffectType.ATTACKUP => attackUpSprite,
            StatusEffectType.AttackDown => attackDownSprite,
            StatusEffectType.Aggravated => aggravatedSprite,
            StatusEffectType.FollowUpAttack => followUpAttackSprite,
            StatusEffectType.Mark => markSprite,
            StatusEffectType.ExtraAttack => extraAttackSprite,
            StatusEffectType.Stress => stressSprite,
            // Counter's count is its total base damage, not remaining turns or charges.
            StatusEffectType.Counter => counterSprite,
            StatusEffectType.DrawOnVulnerable => drawOnVulnerableSprite,
            // Damage-buff icons show bonus damage points, independently of attack charges.
            StatusEffectType.MarkFollowUpDamageUp => markSprite,
            StatusEffectType.FollowUpAttackDamageUp => followUpAttackSprite,
            StatusEffectType.ExtraAttackDamageUp => extraAttackSprite,
            StatusEffectType.BLOCK => BlockSprite,
            _ => null,
        };
    }
}
