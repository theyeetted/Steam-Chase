using System.Runtime.CompilerServices;
using UnityEngine;

public class AttackHeroGA : GameAction, IHaveCaster
{
    public EnemyView Attacker { get; private set; }
    public CombatantView Caster { get; private set;}
    public PlayerView Target { get; private set; }

    public AttackHeroGA(EnemyView attacker, PlayerView target = null)
    {
        Attacker = attacker;
        Caster = attacker;
        Target = target;
    }
}