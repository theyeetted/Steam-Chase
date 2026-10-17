/// <summary>
/// Compatibility type for existing serialized card effects. Also exposes the status-trigger options.
/// Defaults preserve the original Shock tick: all stacks for damage, consuming one stack.
/// Use TriggerStatusEffect when configuring a new effect.
/// </summary>
[System.Serializable]
public class TriggerShockEffect : TriggerStatusEffect
{
}
