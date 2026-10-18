using UnityEngine;

public class ManualTargetSystem : SingleTon<ManualTargetSystem>
{
    [SerializeField] private ArrowView arrowView;
    [Tooltip("Include enemy and hero collider layers. The card's Manual Target Kind filters which combatants can be selected.")]
    [SerializeField] private LayerMask targetLayerMask;

    public void StartTargeting(Vector3 startPosition)
    {
        arrowView.gameObject.SetActive(true);
        arrowView.SetupArrow(startPosition);
    }

    // Preserve the enemy-only API for existing callers.
    public EnemyView EndTargeting(Vector3 endPosition)
    {
        return EndTargeting(endPosition, ManualTargetKind.Enemy) as EnemyView;
    }

    public CombatantView EndTargeting(Vector3 endPosition, ManualTargetKind targetKind)
    {
        CancelTargeting();
        if (Physics.Raycast(endPosition, Vector3.forward, out RaycastHit hit, 10f, targetLayerMask)
            && hit.collider != null)
        {
            // Support colliders on the combatant itself or a child, using the same arrow path.
            CombatantView target = hit.collider.GetComponentInParent<CombatantView>();
            if (ManualTargetRules.IsValid(target, targetKind))
                return target;
        }

        return null;
    }

    public void CancelTargeting()
    {
        if (arrowView != null)
            arrowView.gameObject.SetActive(false);
    }
}
