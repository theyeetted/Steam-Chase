using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int HurtTrigger = Animator.StringToHash("Hurt");

    public void TriggerAttack()
    {
        if (animator != null)
            animator.SetTrigger(AttackTrigger);
    }

    public void TriggerHurt()
    {
        if (animator != null)
            animator.SetTrigger(HurtTrigger);
    }
}
