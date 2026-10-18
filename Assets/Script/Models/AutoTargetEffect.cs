using SerializeReferenceEditor;
using UnityEngine;

public enum RepeatCountMode
{
    NoRepeat,
    FixedNumber,
    AttackCount
}

[System.Serializable]

public class AutoTargetEffect
{
    [field: SerializeReference, SR] public TargetMode TargetMode {  get; private set; }
    [field: SerializeReference, SR] public Effect Effect { get; private set; }
    [field: SerializeField] public RepeatCountMode RepeatMode { get; private set; } = RepeatCountMode.NoRepeat;
    [field: SerializeField, Min(1)] public int RepeatCount { get; private set; } = 1;
    [SerializeField] private AttackHistoryQuery attackCountQuery = new();
    [field: SerializeField, Range(0, 100)] public int ChancePercent { get; private set; } = 100;

    public int ResolveRepeatCount(EffectContext context)
    {
        switch (RepeatMode)
        {
            case RepeatCountMode.FixedNumber:
                return Mathf.Max(1, RepeatCount);
            case RepeatCountMode.AttackCount:
                if (context == null || attackCountQuery == null)
                    return 0;

                CombatantView target = context.ManualTarget;
                if (target == null)
                {
                    var targets = context.ResolveTargets();
                    if (targets.Count == 1)
                        target = targets[0];
                }

                return attackCountQuery.TryGetCount(target, context.Caster, out int count)
                    ? Mathf.Max(0, count)
                    : 0;
            default:
                return 1;
        }
    }
}
    
