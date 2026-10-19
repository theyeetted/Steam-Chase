using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class EnemyView : CombatantView
{
    //[SerializeField] private TMP_Text attackText;
    [SerializeField] private ActionDisplayUI actionDisplayUI;
    [SerializeField] private Animator imageAnimator;

    public int AttackPower {  get; set; }
    public List<EnemySkill> Skills { get; private set; } = new();

    private readonly HashSet<EnemySkill> usedBuffSkills = new();



    private void Awake()
    {
        if (actionDisplayUI == null)
            actionDisplayUI = GetComponentInChildren<ActionDisplayUI>(true);
    }
    

    public void Setup(EnemyData enemyData)
    {
        AttackPower = enemyData.AttackPower;
        Skills = enemyData.Skills ?? new List<EnemySkill>();
        usedBuffSkills.Clear();
        ClearActionDisplay();
        //UpdateAttackText();
        SetupBase(enemyData.Health, enemyData.Image, enemyData.MaxStun);
        SetupAnimator(enemyData.Animator);
    }

    private void SetupAnimator(RuntimeAnimatorController controller)
    {
        if (imageAnimator == null)
        {
            Transform image = transform.Find("Image");
            if (image != null)
                imageAnimator = image.GetComponent<Animator>();
        }

        if (imageAnimator != null)
        {
            // Also clear a previous enemy's controller when this data has none.
            imageAnimator.runtimeAnimatorController = controller;
        }
        else if (controller != null)
        {
            Debug.LogWarning($"{nameof(EnemyView)} needs an Animator on its Image child to play {controller.name}.", this);
        }
    }

    // For tracking buff skills already used for EnemyAI decision
    public bool HasUsedBuff(EnemySkill skill) =>
        usedBuffSkills.Contains(skill);

    public void MarkBuffUsed(EnemySkill skill) =>
        usedBuffSkills.Add(skill);

    public void ClearActionDisplay()
    {
        if (actionDisplayUI != null)
            actionDisplayUI.Clear();
    }

    public void ShowSkillActions(IReadOnlyList<EnemySkillEffect> queuedEffects)
    {
        if (actionDisplayUI != null)
            actionDisplayUI.ShowEffects(queuedEffects);
    }

    public void ShowNormalAttack()
    {
        if (actionDisplayUI != null)
            actionDisplayUI.ShowAttack(AttackPower);
    }


    //private void UpdateAttackText()
    //{
    //    if (attackText != null) { attackText.text = "ATK: " + AttackPower; }
    //}
}
