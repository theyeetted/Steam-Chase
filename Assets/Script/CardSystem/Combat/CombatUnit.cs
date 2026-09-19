using UnityEngine;

namespace Steamchasing.Combat
{
    public class CombatUnit : MonoBehaviour
    {
        [SerializeField] private int maxHP = 80;

        public int CurrentHP { get; private set; }
        public int Block { get; private set; }
        public int AttackUp { get; private set; }
        public int Vulnerable { get; private set; }
        public int Stress { get; private set; }
        public int Shock { get; private set; }
        public int Mark { get; private set; }
        public int AttackDown { get; private set; }
        public float StunGauge { get; private set; }

        private void Awake() => CurrentHP = maxHP;

        public void TakeDamage(int amount)
        {
            int damage = Mathf.Max(0, amount - Block);
            Block = Mathf.Max(0, Block - amount);
            CurrentHP = Mathf.Max(0, CurrentHP - damage);
        }

        public void GainBlock(int amount) => Block += Mathf.Max(0, amount);
        public void Heal(int amount) => CurrentHP = Mathf.Min(maxHP, CurrentHP + Mathf.Max(0, amount));

        public void ApplyVulnerable(int amount) => Vulnerable += amount;
        public void ApplyStress(int amount) => Stress = Mathf.Clamp(Stress + amount, 0, 99);
        public void ApplyShock(int amount) => Shock = Mathf.Clamp(Shock + amount, 0, 99);
        public void ApplyMark(int amount) => Mark = Mathf.Clamp(Mark + amount, 0, 5);
        public void ApplyAttackUp(int amount) => AttackUp += amount;
        public void ApplyAttackDown(int amount) => AttackDown += amount;
        public void FillStunGauge(float percent) => StunGauge = Mathf.Clamp01(StunGauge + percent);
    }
}
