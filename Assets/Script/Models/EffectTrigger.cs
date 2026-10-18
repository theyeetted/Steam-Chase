/// <summary>Read-only filter for a resolved combat event. No per-combat state belongs here.</summary>
[System.Serializable]
public abstract class EffectTrigger
{
    public abstract bool IsMet(GameAction combatEvent, CombatantView owner);
}
