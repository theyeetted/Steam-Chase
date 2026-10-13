using UnityEngine;

public class Interactions : SingleTon<Interactions>
{
    public bool PlayerIsDraging { get; set; }

    public bool PlayerCanInteract() 
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return false;
        if (!ActionSystem.Instance.IsPerformingAction) return true;
        else return false;
    }

    public bool PlayerCanHover()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return false;
        if(PlayerIsDraging) return false;
        return true;
    }
}
