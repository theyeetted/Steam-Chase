using UnityEngine;

public class EnemyanimationController : MonoBehaviour
{
    [SerializeField] private EnemyView enemyView;
    [SerializeField] private Animator animator;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int BuffTrigger = Animator.StringToHash("Buff");
    private static readonly int HurtTrigger = Animator.StringToHash("Hurt");

    private void Awake()
    {
        // Support attaching this script to the enemy root or its animated child.
        if (enemyView == null)
            enemyView = GetComponentInParent<EnemyView>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null && enemyView != null)
            animator = enemyView.GetComponentInChildren<Animator>(true);

        if (enemyView == null || animator == null)
        {
            Debug.LogWarning($"{nameof(EnemyanimationController)} needs an EnemyView and Animator.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        ActionSystem.SubscribeReaction<AttackHeroGA>(OnAttack, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<PerformEffectGA>(OnSkillEffect, ReactionTiming.PRE);
    }

    private void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<AttackHeroGA>(OnAttack, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<PerformEffectGA>(OnSkillEffect, ReactionTiming.PRE);
    }

    private void OnAttack(AttackHeroGA action)
    {
        if (enemyView == null || action.Attacker != enemyView || enemyView.CurrentHealth <= 0)
            return;

        CombatantView target = StatusEffectSystem.GetAggravatedTarget(enemyView);
        if (target == null)
            target = action.Target;
        if (target == null && HeroSystem.Instance != null)
            target = HeroSystem.Instance.playerView;
        if (target != null && target.CurrentHealth > 0)
            PlayTrigger(AttackTrigger);
    }

    private void OnSkillEffect(PerformEffectGA action)
    {
        if (enemyView == null || action.Caster != enemyView || enemyView.CurrentHealth <= 0 ||
            action.Targets == null || !action.Targets.Exists(target => target != null && target.CurrentHealth > 0))
            return;

        // React to execution, never to the next-turn intent preview. Container
        // effects publish their children separately, so don't animate them twice.
        switch (action.Effect)
        {
            case DealDamageEffect _:
                PlayTrigger(AttackTrigger);
                break;
            case AddStatusEffect _:
            case AddStatusDamageBuffEffect _:
            case HealEffect _:
                // Block, buffs, debuffs and healing share the casting animation.
                PlayTrigger(BuffTrigger);
                break;
        }
    }

    public void TriggerHurt()
    {
        PlayTrigger(HurtTrigger);
    }

    private void PlayTrigger(int trigger)
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return;

        animator.SetTrigger(trigger);
    }
}
