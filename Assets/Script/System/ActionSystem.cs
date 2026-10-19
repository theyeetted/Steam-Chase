using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionSystem : SingleTon<ActionSystem>
{
    private List<GameAction> reactions = null;
    public bool IsPerformingAction { get; private set; } = false;
    private sealed class ReactionSubscription
    {
        public readonly Delegate Original;
        public readonly Action<GameAction> Wrapped;

        public ReactionSubscription(Delegate original, Action<GameAction> wrapped)
        {
            Original = original;
            Wrapped = wrapped;
        }
    }

    private static readonly Dictionary<Type, List<ReactionSubscription>> PreSubs = new();
    private static readonly Dictionary<Type, List<ReactionSubscription>> postSubs = new();
    private static readonly Dictionary<Type, Func<GameAction, IEnumerator>> performers = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        // Static registrations survive Play mode when Domain Reload is disabled.
        PreSubs.Clear();
        postSubs.Clear();
        performers.Clear();
    }

    // The one that do the action
    public void Perform(GameAction action, System.Action OnPerformFinished = null)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if(IsPerformingAction) return;
        IsPerformingAction = true;
        StartCoroutine(RunPausable(Flow(action, ()=> {
            IsPerformingAction = false;
            OnPerformFinished?.Invoke();
        }), () => GameManager.Instance != null && GameManager.Instance.IsPaused));
    }

    private static IEnumerator RunPausable(IEnumerator routine, Func<bool> isPaused)
    {
        // timeScale alone does not suspend yield-null routines or nested performers.
        // Keep the enumerators alive, advancing each only after Resume.
        try
        {
            while (true)
            {
                while (isPaused())
                    yield return null;
                if (!routine.MoveNext()) yield break;
                if (routine.Current is IEnumerator nested)
                    yield return RunPausable(nested, isPaused);
                else
                    yield return routine.Current;
            }
        }
        finally
        {
            (routine as IDisposable)?.Dispose();
        }
    }

    public void StopActions()
    {
        StopAllCoroutines();
        reactions = null;
        IsPerformingAction = false;
    }

    public void AddReaction(GameAction gameAction)
    {
        reactions?.Add(gameAction);
    }

    // Tell the system what code runs when a specific action happens
    public static void AttachPerformer<T>(Func<T, IEnumerator> performer) where T : GameAction
    {
        Type type = typeof(T);
        IEnumerator wrappedPerformer(GameAction action) => performer((T)action);
        if(performers.ContainsKey(type)) performers[type] = wrappedPerformer;
        else performers.Add(type, wrappedPerformer);

    }

    private IEnumerator Flow(GameAction action, Action OnFlowFinished = null, GameAction parent = null)
    {
        action.ParentAction = parent;
        reactions = action.OpeningReactions;
        yield return PerformReactions(action.OpeningReactions, action);

        // Do stuff before action (early reaction)
        reactions = action.PreReactions;
        PerformSubscribers(action, PreSubs);
        yield return PerformReactions(action.PreReactions, action);  // Recursive

        // Immediate reaction
        reactions = action.PerformReactions;
        yield return PerformPerformer(action);
        yield return PerformReactions(action.PerformReactions, action);

        // Reaction after action finished
        reactions = action.PostReactions;
        PerformSubscribers(action, postSubs);
        yield return PerformReactions(action.PostReactions, action);

        reactions = action.CompletionReactions;
        yield return PerformReactions(action.CompletionReactions, action);

        OnFlowFinished?.Invoke(); // Mark as complete
    }

    // For other code to call perform() // do the action
    private IEnumerator PerformPerformer(GameAction action) 
    {
        Type type = action.GetType();
        if(performers.ContainsKey(type))
        {
            yield return performers[type](action);
        }
    }


    private void PerformSubscribers(GameAction action, Dictionary<Type, List<ReactionSubscription>> subs)
    {
        Type type = action.GetType();
        if(subs.ContainsKey(type))
        {
            foreach(var sub in subs[type])
            {
                sub.Wrapped(action);
            }

        }

    }

    private IEnumerator PerformReactions(List<GameAction> queuedReactions, GameAction parent)
    {
        foreach(var reaction in queuedReactions)
        {
            yield return Flow(reaction, parent: parent);
        }
    }

    // Remove
    public static void DetachPerformer<T>() where T : GameAction
    {
        Type type = typeof(T);
        if(performers.ContainsKey(type)) performers.Remove(type);
    }

    // Listen and run
    public static void SubscribeReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        var subs = timing == ReactionTiming.PRE ? PreSubs : postSubs;
        Type type = typeof(T);
        if (!subs.TryGetValue(type, out var subscriptions))
        {
            subscriptions = new();
            subs.Add(type, subscriptions);
        }

        if (subscriptions.Exists(subscription => subscription.Original.Equals(reaction))) return;

        // Keep the original delegate so unsubscription can find its exact wrapper.
        subscriptions.Add(new ReactionSubscription(reaction, action => reaction((T)action)));
    }

    public static void UnsubscribeReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        var subs = timing == ReactionTiming.PRE ? PreSubs : postSubs;
        Type type = typeof(T);
        if (subs.TryGetValue(type, out var subscriptions))
        {
            subscriptions.RemoveAll(subscription => subscription.Original.Equals(reaction));
            if (subscriptions.Count == 0) subs.Remove(type);
        }
    }
}
