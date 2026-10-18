using SerializeReferenceEditor;
using UnityEngine;

/// <summary>Nonnegative integer ratio, rounded down. For 40%, use 40 / 100.</summary>
[System.Serializable]
public class RatioValue : EffectValue
{
    [SerializeField, Min(0)] private int numerator = 40;
    [SerializeField, Min(1)] private int denominator = 100;
    [SerializeReference, SR] private EffectValue input;

    public override int GetValue(EffectContext context)
    {
        return TryGetValue(context, out int value) ? value : 0;
    }

    public override bool TryGetValue(EffectContext context, out int value)
    {
        value = 0;
        if (input == null || !input.TryGetValue(context, out int inputValue))
            return false;
        long result = (long)Mathf.Max(0, inputValue) * Mathf.Max(0, numerator) /
            Mathf.Max(1, denominator);
        value = (int)System.Math.Min(int.MaxValue, result);
        return true;
    }
}
