public class GainEnergyGA : GameAction, IHaveCaster
{
    public int Amount { get; }
    public CombatantView Caster { get; }

    public GainEnergyGA(int amount, CombatantView caster)
    {
        Amount = amount;
        Caster = caster;
    }
}
