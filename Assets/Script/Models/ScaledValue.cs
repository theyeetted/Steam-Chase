using SerializeReferenceEditor;
using UnityEngine;

/// <summary>Nonnegative base + multiplier * input, bounded to the integer amount range.</summary>
[System.Serializable]
public class ScaledValue : EffectValue
{
    [SerializeField, Min(0)] private int baseValue;
    [SerializeField, Min(0)] private int multiplier = 1;
    [Tooltip("Empty supplies zero, leaving only Base Value.")]
    [SerializeReference, SR] private EffectValue input;

    public override int GetValue(EffectContext context)
    {
        return TryGetValue(context, out int value) ? value : 0;
    }

    public override bool TryGetValue(EffectContext context, out int value)
    {
        value = 0;
        int inputValue = 0;
        if (input != null && !input.TryGetValue(context, out inputValue))
            return false;
        long result = (long)Mathf.Max(0, baseValue) +
            (long)Mathf.Max(0, multiplier) * Mathf.Max(0, inputValue);
        value = (int)System.Math.Min(int.MaxValue, result);
        return true;
    }
}
