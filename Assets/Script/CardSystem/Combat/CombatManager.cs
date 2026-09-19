using System.Collections.Generic;
using Steamchasing.Cards;
using UnityEngine;

namespace Steamchasing.Combat
{
    public class CombatManager : MonoBehaviour
    {
        [SerializeField] private int energy = 3;

        public int Energy => energy;

        public void ResetEnergy() => energy = 3;

        public bool TrySpendEnergy(int amount)
        {
            if (amount > energy)
                return false;

            energy -= amount;
            return true;
        }

        public void GainEnergy(int amount) => energy += amount;

        public void DealDamage(CombatUnit source, CombatUnit target, int amount)
        {
            target.TakeDamage(amount);
        }

        public void DealToAll(CombatUnit source, IReadOnlyList<CombatUnit> targets, int amount)
        {
            foreach (var target in targets)
                if (target != null)
                    DealDamage(source, target, amount);
        }
    }
}
