using UnityEngine;

[CreateAssetMenu(menuName = "CardData")]

public class CardData : ScriptableObject
{

    [field: SerializeField] public string description { get; private set; }

    [field: SerializeField] public Sprite Image { get; private set; }

    [field: SerializeField] public int Cost { get; private set; }
}
