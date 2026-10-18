/// <summary>One resolved hit. Store values, not a live target whose state can later change.</summary>
public sealed class DamageResult
{
    public int TargetId { get; }
    public int DamageBeforeBlock { get; }
    public int HealthLost { get; }
    public bool ConditionMatched { get; }

    public DamageResult(int targetId, int damageBeforeBlock, int healthLost, bool conditionMatched)
    {
        TargetId = targetId;
        DamageBeforeBlock = System.Math.Max(0, damageBeforeBlock);
        HealthLost = System.Math.Max(0, healthLost);
        ConditionMatched = conditionMatched;
    }
}
