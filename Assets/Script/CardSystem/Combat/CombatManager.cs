using System.Collections.Generic;
using UnityEngine;

namespace Steamchasing.Combat
{
    public class CombatManager : MonoBehaviour
    {
        [SerializeField] private int energy = 3;

        [Header("Enemies")]
        [SerializeField] private List<CombatUnit> enemies = new();

        public int Energy => energy;
        public IReadOnlyList<CombatUnit> Enemies => enemies;

        public void ResetEnergy() => energy = 3;

        public bool TrySpendEnergy(int amount)
        {
            if (amount > energy)
                return false;

            energy -= Mathf.Max(0, amount);
            return true;
        }

        public void GainEnergy(int amount)
        {
            energy += Mathf.Max(0, amount);
        }

        public void RegisterEnemy(CombatUnit enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public void UnregisterEnemy(CombatUnit enemy)
        {
            if (enemy != null)
                enemies.Remove(enemy);
        }

        public void DealDamage(CombatUnit source, CombatUnit target, int amount)
        {
            if (target == null)
                return;

            int damage = Mathf.Max(0, amount);

            // Prefer the runtime status system when it is present.
            if (source != null && source.StatusEffects != null)
            {
                damage = source.StatusEffects.ModifyOutgoingDamage(
                    damage,
                    target.StatusEffects
                );
            }
            else if (source != null)
            {
                // Fallback for simple test units without StatusEffectController.
                damage += source.AttackUp;
                damage -= source.AttackDown;
                damage = Mathf.Max(0, damage);
            }

            if (target.StatusEffects != null)
            {
                damage = target.StatusEffects.ModifyIncomingDamage(damage);
            }
            else
            {
                if (target.Vulnerable > 0)
                    damage = Mathf.RoundToInt(damage * 1.25f);

                if (target.IsStunned)
                    damage = Mathf.RoundToInt(damage * 1.20f);
            }

            target.TakeDamage(damage);

            if (target.StatusEffects != null)
            {
                target.StatusEffects.OnAttacked(
                    source != null ? source.StatusEffects : null
                );
            }
        }

        public void DealToAll(
            CombatUnit source,
            IReadOnlyList<CombatUnit> targets,
            int amount)
        {
            if (targets == null)
                return;

            foreach (CombatUnit target in targets)
            {
                if (target != null)
                    DealDamage(source, target, amount);
            }
        }
    }
}
