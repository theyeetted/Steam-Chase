using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays the enemy's planned effects at the start of the player's turn.
/// Values are base damage or stacks per target, before combat modifiers.
/// Attach to the existing ActionUI row with its HorizontalLayoutGroup.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class ActionDisplayUI : MonoBehaviour
{
    [Header("Row")]
    [Tooltip("ActionWrapper prefab containing one icon and one TMP text component.")]
    [SerializeField] private GameObject actionUIPrefab;
    [Tooltip("Defaults to this object's RectTransform.")]
    [SerializeField] private RectTransform rowRoot;
    [Tooltip("Size of each action slot in Canvas units.")]
    [SerializeField] private Vector2 itemSize = new(64f, 64f);

    [Header("Icons")]
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private Sprite blockingSprite;
    [SerializeField] private Sprite buffSprite;
    [SerializeField] private Sprite debuffSprite;
    [Tooltip("Optional icon for other effects, including conditions evaluated later. Their value is shown as ?.")]
    [SerializeField] private Sprite otherActionSprite;

    private readonly List<GameObject> items = new();

    protected virtual void Awake()
    {
        if (rowRoot == null)
            rowRoot = (RectTransform)transform;
    }

    public void ShowAttack(int damage)
    {
        Clear();
        AddItem(attackSprite, damage.ToString(), "Attack");
    }

    /// <summary>
    /// Each entry represents one successfully planned repeat. Separate effects get
    /// separate icons, so attack + Block displays both. Repeats show e.g. 10 x 2.
    /// </summary>
    public void ShowEffects(IReadOnlyList<EnemySkillEffect> queuedEffects)
    {
        Clear();
        if (queuedEffects == null)
            return;

        List<EnemySkillEffect> orderedEffects = new();
        Dictionary<EnemySkillEffect, int> repeatCounts = new();
        foreach (EnemySkillEffect entry in queuedEffects)
        {
            if (entry == null || entry.Effect == null)
                continue;

            // Keep the first occurrence order and separate authored effects separate.
            if (!repeatCounts.ContainsKey(entry))
            {
                orderedEffects.Add(entry);
                repeatCounts.Add(entry, 0);
            }
            repeatCounts[entry]++;
        }

        foreach (EnemySkillEffect entry in orderedEffects)
        {
            Sprite sprite;
            string value;
            string label;
            switch (entry.Effect)
            {
                case DealDamageEffect damage:
                    sprite = attackSprite;
                    value = damage.DamageAmount.ToString();
                    label = "Attack";
                    break;
                case AddStatusEffect status:
                    sprite = GetStatusSprite(status.StatusEffectType);
                    value = status.StackCount.ToString();
                    label = status.StatusEffectType.ToString();
                    break;
                default:
                    // Display never executes effects or rerolls chances/targets.
                    sprite = otherActionSprite;
                    value = "?";
                    label = entry.Effect.GetType().Name;
                    break;
            }

            int repeats = repeatCounts[entry];
            if (repeats > 1)
                value += " x " + repeats;
            AddItem(sprite, value, label);
        }
    }

    public void Clear()
    {
        // Own only the generated slots, not other children of the existing row.
        foreach (GameObject item in items)
        {
            if (item == null)
                continue;
            item.SetActive(false); // Remove from layout before deferred destruction.
            Destroy(item);
        }
        items.Clear();
    }

    protected virtual void OnDestroy()
    {
        Clear();
    }

    private Sprite GetStatusSprite(StatusEffectType type)
    {
        return type switch
        {
            StatusEffectType.BLOCK => blockingSprite,
            StatusEffectType.VULNERABLE or StatusEffectType.SHOCK or
            StatusEffectType.AttackDown or StatusEffectType.Mark or
            StatusEffectType.Stun or StatusEffectType.Aggravated or
            StatusEffectType.Stress => debuffSprite,
            StatusEffectType.ATTACKUP or StatusEffectType.ExtraAttack or
            StatusEffectType.FollowUpAttack or StatusEffectType.DefenseUp or
            StatusEffectType.MarkFollowUpDamageUp or StatusEffectType.FollowUpAttackDamageUp or
            StatusEffectType.ExtraAttackDamageUp or StatusEffectType.Counter => buffSprite,
            _ => otherActionSprite,
        };
    }

    private void AddItem(Sprite sprite, string value, string label)
    {
        if (actionUIPrefab == null || sprite == null)
        {
            Debug.LogWarning($"{nameof(ActionDisplayUI)}: Assign the action prefab and icon for {label}.", this);
            return;
        }

        if (rowRoot == null)
            rowRoot = (RectTransform)transform;

        // The supplied ActionWrapper has a plain Transform. A RectTransform slot
        // lets the existing HorizontalLayoutGroup arrange it without prefab edits.
        GameObject slot = new(label + " Action", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
        slot.layer = rowRoot.gameObject.layer;
        RectTransform slotRect = (RectTransform)slot.transform;
        slotRect.SetParent(rowRoot, false);
        Vector2 size = new(Mathf.Max(1f, itemSize.x), Mathf.Max(1f, itemSize.y));
        slotRect.sizeDelta = size;
        UnityEngine.UI.LayoutElement layout = slot.GetComponent<UnityEngine.UI.LayoutElement>();
        layout.minWidth = layout.preferredWidth = size.x;
        layout.minHeight = layout.preferredHeight = size.y;
        layout.flexibleWidth = layout.flexibleHeight = 0f;
        items.Add(slot);

        GameObject visual = Instantiate(actionUIPrefab, slotRect, false);
        visual.SetActive(true);
        UnityEngine.UI.Image image = visual.GetComponentInChildren<UnityEngine.UI.Image>(true);
        SpriteRenderer spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);
        TMP_Text text = visual.GetComponentInChildren<TMP_Text>(true);
        if ((image == null && spriteRenderer == null) || text == null)
        {
            Debug.LogWarning($"{nameof(ActionDisplayUI)}: {actionUIPrefab.name} needs an Image or SpriteRenderer and TMP text.", this);
            slot.SetActive(false);
            Destroy(slot);
            items.Remove(slot);
            return;
        }

        text.text = value;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        if (image != null)
        {
            image.sprite = sprite;
            image.raycastTarget = false;
        }
        else
        {
            spriteRenderer.sprite = sprite;
            if (text is TextMeshPro worldText)
            {
                worldText.sortingLayerID = spriteRenderer.sortingLayerID;
                worldText.sortingOrder = spriteRenderer.sortingOrder + 1;
            }
        }

        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        if (image != null && visual.transform is RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            // Support the current SpriteRenderer + 3D TMP prefab on the world-space
            // Canvas. Fit its existing icon/text arrangement to the Canvas slot.
            text.ForceMeshUpdate(true);
            FitWorldSpaceVisual(visual.transform, size);
        }
    }

    private static void FitWorldSpaceVisual(Transform visual, Vector2 size)
    {
        Bounds bounds = default;
        bool hasBounds = false;
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled)
                continue;

            Bounds local = renderer.localBounds;
            Matrix4x4 toRoot = visual.worldToLocalMatrix * renderer.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = toRoot.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                    new Vector3((corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));
                if (!hasBounds)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(point);
            }
        }

        if (!hasBounds || bounds.size.x <= 0f || bounds.size.y <= 0f)
            return;

        float scale = Mathf.Min(size.x / bounds.size.x, size.y / bounds.size.y);
        visual.localScale = Vector3.one * scale;
        visual.localPosition = -bounds.center * scale;
    }
}
