using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

public abstract class GameAction 
{
    // Per-execution ancestry, never shared card data. This prevents a rule's payload
    // (including nested effects) from recursively activating that same runtime grant.
    internal GameAction ParentAction { get; set; }
    internal object TriggerSource { get; set; }
    private HashSet<object> triggeredGrants;

    internal bool TryRegisterTrigger(object grant)
    {
        for (GameAction ancestor = this; ancestor != null; ancestor = ancestor.ParentAction)
        {
            if (ReferenceEquals(ancestor.TriggerSource, grant))
                return false;
        }

        triggeredGrants ??= new();
        return triggeredGrants.Add(grant);
    }

    // Ordered boundary work finishes before PRE subscribers inspect live state.
    public List<GameAction> OpeningReactions { get; private set; } = new();

    public List<GameAction> PreReactions { get; private set; } = new();

    public List<GameAction> PerformReactions { get; private set; } = new();

    public List<GameAction> PostReactions { get; private set; } = new();

    // Explicit completion work runs after every POST subscriber and its reaction tree.
    // This avoids depending on MonoBehaviour subscription order for phase boundaries.
    public List<GameAction> CompletionReactions { get; private set; } = new();
}
