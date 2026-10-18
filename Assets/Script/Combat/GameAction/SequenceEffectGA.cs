using System.Collections.Generic;

public class SequenceEffectGA : GameAction, IHaveCaster
{
    public IReadOnlyList<Effect> Effects { get; }
    public EffectContext Context { get; }
    public CombatantView Caster => Context.Caster;

    public SequenceEffectGA(List<Effect> effects, EffectContext context)
    {
        // Snapshot the list per execution; do not keep a mutable reference to card data.
        Effects = effects == null ? new List<Effect>() : new List<Effect>(effects);
        Context = context;
    }
}
