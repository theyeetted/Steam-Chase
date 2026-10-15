using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Char")]
public class CharData : ScriptableObject
{
    [field: SerializeField] public Sprite Image { get; private set; }
    [field: SerializeField] public int Health { get; private set; }
    [field: SerializeField] public List<CardData> Deck { get; private set; }
    [field: SerializeField, Min(1)] public int MaxStun { get; private set; } = 100;

}
