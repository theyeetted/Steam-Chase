using System.Collections.Generic;
using UnityEngine;


public class Card 
{
    public string Title => data.name;

    public int Cost { get; private set; }

    public bool Disintegrate => data.Disintegrate;

    public string Description => data.description;

    public Effect MannualTargetEffect => data.MannualTargetEffect;

    public List<AutoTargetEffect> OtherEffects => data.OtherTargetEffects;

    public Sprite Image => data.Image;

    private readonly CardData data;

    public Card(CardData data)
    {
        this.data = data;
        Cost = data.Cost;
    }
}
