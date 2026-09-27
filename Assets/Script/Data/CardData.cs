using UnityEngine;
using System.Collections.Generic;
using SerializeReferenceEditor;

[CreateAssetMenu(menuName = "CardData")]

public class CardData : ScriptableObject
{

    [field: SerializeField] public string description { get; private set; }

    [field: SerializeField] public Sprite Image { get; private set; }

    [field: SerializeField] public int Cost { get; private set; }

    [field: SerializeReference, SR] public List<Effect> Effects { get; private set; }
}
