using System.Collections;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    //performer
    void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerfromEffectPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
    }

    private IEnumerator PerfromEffectPerformer(PerformEffectGA performEffectGA)
    {
        GameAction effecAction = performEffectGA.Effect.GetGameAction();
        ActionSystem.Instance.AddReaction(effecAction);
        yield return null;
    }
}
