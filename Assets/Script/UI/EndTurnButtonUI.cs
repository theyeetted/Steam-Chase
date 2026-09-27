using UnityEngine;

public class EndTurnButtonUI : MonoBehaviour
{
    public void Onclick()
    {
        EnemyTurnGA enemyTurnGA = new();
        ActionSystem.Instance.Perform(enemyTurnGA);
    }
}
