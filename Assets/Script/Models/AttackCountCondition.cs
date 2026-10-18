using UnityEngine;

[System.Serializable]
public class AttackCountCondition : EffectCondition
{
    [SerializeField] private AttackHistoryQuery history = new();
    [SerializeField, Min(1)] private int minimumCount = 1;
    [Tooltip("Require fewer than Minimum Count. Missing or dead single subjects never pass.")]
    [SerializeField] private bool invert;

    public override bool IsMet(CombatantView target, CombatantView caster)
    {
        if (history == null || !history.TryGetCount(target, caster, out int count))
            return false;

        bool enough = count >= Mathf.Max(1, minimumCount);
        return invert ? !enough : enough;
    }
}
