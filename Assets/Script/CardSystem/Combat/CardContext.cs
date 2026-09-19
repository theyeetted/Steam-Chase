using UnityEngine;

namespace Steamchasing.Combat
{
    public class CardContext
    {
        public CombatUnit Source { get; }
        public CombatUnit Target { get; }
        public CombatManager Combat { get; }

        public CardContext(CombatUnit source, CombatUnit target, CombatManager combat)
        {
            Source = source;
            Target = target;
            Combat = combat;
        }
    }
}
