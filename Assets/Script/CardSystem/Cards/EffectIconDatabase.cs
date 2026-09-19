using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steamchasing.Cards
{
    [System.Serializable]
    public class EffectIconEntry
    {
        public EffectType effectType;
        public Sprite icon;
    }

    [CreateAssetMenu(fileName = "EffectIconDatabase", menuName = "Steamchasing/Cards/Effect Icon Database")]
    public class EffectIconDatabase : ScriptableObject
    {
        [SerializeField] private List<EffectIconEntry> effectIcons = new();

        private Dictionary<EffectType, Sprite> lookup;

        public void Initialize()
        {
            lookup = new Dictionary<EffectType, Sprite>();

            foreach (var entry in effectIcons)
            {
                if (entry.icon == null)
                {
                    Debug.LogWarning($"No icon assigned for effect type: {entry.effectType}", this);
                    continue;
                }

                if (!lookup.TryAdd(entry.effectType, entry.icon))
                    Debug.LogWarning($"Duplicate entry for effect type: {entry.effectType}", this);
            }
        }

        public Sprite GetIcon(EffectType effectType)
        {
            if (lookup == null) Initialize();

            if (lookup.TryGetValue(effectType, out var icon))
                return icon;

            Debug.LogWarning($"No icon found for effect type: {effectType}", this);
            return null;
        }
    }
}
