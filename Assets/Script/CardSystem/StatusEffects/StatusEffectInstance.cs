namespace Steamchasing.Combat
{
    /// <summary>
    /// Runtime data for one active status effect.
    /// </summary>
    public class StatusEffectInstance
    {
        public StatusEffectType Type { get; private set; }
        public int Stacks { get; private set; }

        /// <summary>
        /// -1 means that the status does not use a normal duration timer.
        /// </summary>
        public int Duration { get; private set; }

        /// <summary>
        /// Unit that applied this status.
        /// Used by statuses such as Aggravated.
        /// </summary>
        public IStatusEffectOwner Source { get; private set; }

        public StatusEffectInstance(
            StatusEffectType type,
            int stacks,
            int duration = -1,
            IStatusEffectOwner source = null)
        {
            Type = type;
            Stacks = stacks;
            Duration = duration;
            Source = source;
        }

        public void AddStacks(int amount, int maxStacks = int.MaxValue)
        {
            Stacks += amount;

            if (Stacks > maxStacks)
                Stacks = maxStacks;
        }

        public void SetStacks(int amount)
        {
            Stacks = amount < 0 ? 0 : amount;
        }

        public void RemoveStacks(int amount)
        {
            Stacks -= amount;

            if (Stacks < 0)
                Stacks = 0;
        }

        public void SetDuration(int duration)
        {
            Duration = duration;
        }

        public void RefreshDuration(int duration)
        {
            if (duration > Duration)
                Duration = duration;
        }

        public void ReduceDuration()
        {
            if (Duration > 0)
                Duration--;
        }

        public bool IsExpired()
        {
            if (Stacks <= 0)
                return true;

            return Duration == 0;
        }
    }
}
