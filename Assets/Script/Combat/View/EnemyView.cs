using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class EnemyView : CombatantView
{
    [SerializeField] private TMP_Text attackText;

    public int AttackPower {  get; set; }
    public List<EnemySkill> Skills { get; private set; } = new();

    private readonly HashSet<EnemySkill> usedBuffSkills = new();
    

    public void Setup(EnemyData enemyData)
    {
        AttackPower = enemyData.AttackPower;
        Skills = enemyData.Skills ?? new List<EnemySkill>();
        usedBuffSkills.Clear();
        UpdateAttackText();
        SetupBase(enemyData.Health, enemyData.Image, enemyData.MaxStun);
    }

    // For tracking buff skills already used for EnemyAI decision
    public bool HasUsedBuff(EnemySkill skill) =>
        usedBuffSkills.Contains(skill);

    public void MarkBuffUsed(EnemySkill skill) =>
        usedBuffSkills.Add(skill);


    private void UpdateAttackText()
    {
        if (attackText != null) { attackText.text = "ATK: " + AttackPower; }
    }
}
