using UnityEngine;


public class Card 
{
    public string Title => data.name;

    public int Cost { get; private set; }

    public string Description => data.description;

    public Sprite Image => data.Image;

    private readonly CardData data;

    public Card(CardData data)
    {
        this.data = data;
        Cost = data.Cost;
    }
}
