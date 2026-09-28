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
        GameAction effecAction = performEffectGA.Effect.GetGameAction(performEffectGA.Targets, HeroSystem.Instance.playerView);
        ActionSystem.Instance.AddReaction(effecAction);
        yield return null;
    }
}
