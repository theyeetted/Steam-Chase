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

    public void Perform(GameAction action, System.Action OnPerformFinished = null)
    {
        if(IsPerformingAction) return;
        IsPerformingAction = true;
        StartCoroutine(Flow(action, ()=> {
            IsPerformingAction = false;
            OnPerformFinished?.Invoke();
        }));
    }

    public void AddReaction(GameAction gameAction)
    {
        reactions?.Add(gameAction);
    }
    public static void AttachPerformer<T>(Func<T, IEnumerator> performer) where T : GameAction
    {
        Type type = typeof(T);
        IEnumerator wrappedPerformer(GameAction action) => performer((T)action);
        if(performers.ContainsKey(type)) performers[type] = wrappedPerformer;
        else performers.Add(type, wrappedPerformer);

    }

    private IEnumerator Flow(GameAction action, Action OnFlowFinished = null)
    {
        reactions = action.PreReactions;
        PerformSubscribers(action, PreSubs);
        yield return PerformReactions();

        reactions = action.PerformReactions;
        yield return PerformPerformer(action);
        yield return PerformReactions();

        reactions = action.PostReactions;
        PerformSubscribers(action, postSubs);
        yield return PerformReactions();

        OnFlowFinished?.Invoke();
    }

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

    private IEnumerator PerformReactions() 
    {
        foreach(var reaction in reactions)
        {
            yield return Flow(reaction);
        }
    }

    public static void DetachPerformer<T>() where T : GameAction
    {
        Type type = typeof(T);
        if(performers.ContainsKey(type)) performers.Remove(type);
    }

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
