/// <summary>Read-only amount calculation. Implementations must not retain per-play state.</summary>
[System.Serializable]
public abstract class EffectValue
{
    public abstract int GetValue(EffectContext context);

    // A missing producer differs from a successful zero result. Consumers skip on false.
    public virtual bool TryGetValue(EffectContext context, out int value)
    {
        value = GetValue(context);
        return true;
    }
}
