/// <summary>Shared validation for pointer selection and accepted card plays.</summary>
public static class ManualTargetRules
{
    public static bool IsValid(CombatantView target, ManualTargetKind kind)
    {
        if (target == null || target.CurrentHealth <= 0)
            return false;

        return kind switch
        {
            ManualTargetKind.Enemy => target is EnemyView,
            // Either registered living hero may be selected, including the caster.
            ManualTargetKind.Hero => target is PlayerView && HeroSystem.Instance != null &&
                HeroSystem.Instance.TryGetHeroIndex(target, out _),
            _ => false
        };
    }
}
