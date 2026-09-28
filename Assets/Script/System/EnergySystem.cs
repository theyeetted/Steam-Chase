using System.Collections;
using UnityEngine;

public class EnergySystem : SingleTon<EnergySystem>
{
    [SerializeField] EnergyUI energyUI;
    private const int MAX_ENERGY = 3;
    private int currentEnergy = MAX_ENERGY;

    private void Start()
    {
        energyUI.UpdateEnergyText(currentEnergy);
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendEnergyGA>(SpendEnergyPerformer);
        ActionSystem.AttachPerformer<RefillEnergyGA>(RefillEnergyPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendEnergyGA>();
        ActionSystem.DetachPerformer<RefillEnergyGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    public bool HasEnoughEnergy(int energy)
    {
        return currentEnergy >= energy;
    }

    private IEnumerator SpendEnergyPerformer(SpendEnergyGA spendEnergyGA)
    {
        currentEnergy-= spendEnergyGA.Amount;
        energyUI.UpdateEnergyText(currentEnergy);
        yield return null;
    }
    private IEnumerator RefillEnergyPerformer(RefillEnergyGA refillEnergyGA)
    {
        currentEnergy = MAX_ENERGY;
        energyUI.UpdateEnergyText(currentEnergy);
        yield return null;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        RefillEnergyGA refillEnergyGA = new();
        ActionSystem.Instance.AddReaction(refillEnergyGA);
    }
}
