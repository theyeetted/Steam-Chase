using System.Collections.Generic;

/// <summary>
/// Named results local to one effect execution (usually a SequenceEffect). Independent
/// card effects and triggered reactions start with their own store, never a global last hit.
/// </summary>
public sealed class EffectResults
{
    private readonly Dictionary<string, List<DamageResult>> damage = new();

    public List<DamageResult> BeginDamage(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        // Replace at execution even if the producer later skips. Do not reuse an older
        // successful result when a repeated producer has no living target this time.
        List<DamageResult> results = new();
        damage[key] = results;
        return results;
    }

    public bool TryGetDamage(string key, out IReadOnlyList<DamageResult> results)
    {
        results = null;
        if (string.IsNullOrWhiteSpace(key) || !damage.TryGetValue(key, out var found))
            return false;
        results = found;
        return true;
    }
}
