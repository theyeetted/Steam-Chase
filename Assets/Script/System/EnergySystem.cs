using System.Collections;
using UnityEngine;

public class EnergySystem : SingleTon<EnergySystem>
{
    [SerializeField] private EnergyUI[] energyUIs = new EnergyUI[2];

    private const int MAX_ENERGY = 3;
    private readonly int[] currentEnergy = { MAX_ENERGY, MAX_ENERGY };

    private void Start()
    {
        for (int i = 0; i < currentEnergy.Length; i++)
            UpdateEnergyUI(i);
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

    public bool HasEnoughEnergy(int energy, int heroIndex = 0)
    {
        return currentEnergy[heroIndex] >= energy;
    }

    private IEnumerator SpendEnergyPerformer(SpendEnergyGA spendEnergyGA)
    {
        int heroIndex = spendEnergyGA.HeroIndex;

        currentEnergy[heroIndex] -= spendEnergyGA.Amount;
        UpdateEnergyUI(heroIndex);
        yield return null;
    }

    private IEnumerator RefillEnergyPerformer(RefillEnergyGA refillEnergyGA)
    {
        int heroIndex = refillEnergyGA.HeroIndex;

        currentEnergy[heroIndex] = MAX_ENERGY;
        UpdateEnergyUI(heroIndex);
        yield return null;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        ActionSystem.Instance.AddReaction(new RefillEnergyGA(0));
        ActionSystem.Instance.AddReaction(new RefillEnergyGA(1));

        /*
        RefillEnergyGA refillEnergyGA = new();
        ActionSystem.Instance.AddReaction(refillEnergyGA);
        */
    }

    // Way to reset energy
    public void ResetEnergy()
    {
        for (int i = 0; i < currentEnergy.Length; i++)
        {
            currentEnergy[i] = MAX_ENERGY;
            UpdateEnergyUI(i);
        }
    }

    public void ResetEnergy(int heroIndex)
    {
        currentEnergy[heroIndex] = MAX_ENERGY;
        UpdateEnergyUI(heroIndex);
    }

    private void UpdateEnergyUI(int heroIndex)
    {
        if (energyUIs[heroIndex] != null)
            energyUIs[heroIndex].UpdateEnergyText(currentEnergy[heroIndex]);
    }

}
