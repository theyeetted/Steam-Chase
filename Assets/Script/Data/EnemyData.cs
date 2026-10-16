using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu (menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [field:SerializeField] public Sprite Image {  get; private set; }
    [field: SerializeField] public int Health { get; private set; }
    [field: SerializeField] public int AttackPower { get; private set; }
    [field: SerializeField, Min(1)] public int MaxStun { get; private set; } = 100;
    [field: SerializeField] public EnemyRank Rank { get; private set; }
    [field: SerializeField] public List<EnemySkill> Skills { get; private set; } = new List<EnemySkill>();
}
