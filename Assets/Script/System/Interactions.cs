using UnityEngine;

public class Interactions : SingleTon<Interactions>
{
    public bool PlayerIsDraging { get; set; }

    public bool PlayerCanInteract() 
    {
        if (!ActionSystem.Instance.IsPerformingAction) return true;
        else return false;
    }

    public bool PlayerCanHover()
    {
        if(PlayerIsDraging) return false;
        return true;
    }
}
