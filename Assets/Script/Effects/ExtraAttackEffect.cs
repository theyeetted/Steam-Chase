using System.Collections.Generic;

[System.Serializable]
public class ExtraAttackEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new ExtraAttackGA(targets, caster);
    }
}
