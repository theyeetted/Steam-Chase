using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
        GameAction effecAction = performEffectGA.Effect.GetGameAction(performEffectGA.Targets, performEffectGA.Caster);
        ActionSystem.Instance.AddReaction(effecAction);
        yield return null;
    }
}
