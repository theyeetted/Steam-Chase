
using System;
using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

public enum EnemyRank { Common, Elite, Boss }

public enum EnemySkillCategory { Basic, Ultimate }

public enum EnemySkillPurpose { Attack, Heal, Buff, Other }

[Serializable]
public class EnemySkill
{
    [field: SerializeField] public string SkillName { get; private set; }

    [field: SerializeField, TextArea] public string Description { get; private set; }

    [field: SerializeField] public EnemySkillCategory Category { get; private set; }

    [field: SerializeField] public EnemySkillPurpose Purpose { get; private set; }

    [field: SerializeField] public List<EnemySkillEffect> Effects { get; private set; } = new List<EnemySkillEffect>();
}

[Serializable]
public class EnemySkillEffect
{
    [field: SerializeReference, SR]
    public TargetMode TargetMode { get; private set; }

    [field: SerializeReference, SR]
    public Effect Effect { get; private set; }

    [field: SerializeField, Min(1)]
    public int RepeatCount { get; private set; } = 1;

    [field: SerializeField, Range(0, 100)]
    public int ChancePercent { get; private set; } = 100;
}
