using System.Collections;
using UnityEngine;

public class ShockSystem : MonoBehaviour
{
    [SerializeField] private GameObject ShockVFX;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<ApplyShockGA>(ApplyShockPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<ApplyShockGA>();
    }

    private IEnumerator ApplyShockPerformer(ApplyShockGA applyShockGA)
    {
        CombatantView target = applyShockGA.Target;
        Instantiate(ShockVFX, target.transform.position, Quaternion.identity);
        target.Damage(applyShockGA.ShockDamage);
        target.RemoveStatusEffect(StatusEffectType.SHOCK, 1);
        yield return new WaitForSeconds(1f);
    }
}
