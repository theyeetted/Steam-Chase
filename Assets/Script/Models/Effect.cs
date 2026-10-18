using System.Collections.Generic;

[System.Serializable]
public abstract class Effect 
{
    public abstract GameAction GetGameAction(List<CombatantView> targets, CombatantView caster);

    // Existing effects keep their original API. Compositions override this overload
    // only when they need the manual selection as well as their payload recipients.
    public virtual GameAction GetGameAction(EffectContext context)
    {
        return GetGameAction(context.ResolveTargets(), context.Caster);
    }

}
