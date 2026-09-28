
using UnityEngine;

public class HeroSystem : SingleTon<HeroSystem>
{
    [field: SerializeField] public PlayerView playerView {  get; private set; }

    public void Setup(CharData charData)
    {
        playerView.Setup(charData);
    }
}
