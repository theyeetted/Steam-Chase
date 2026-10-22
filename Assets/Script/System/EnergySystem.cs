using System.Collections;
using UnityEngine;

public class EnergySystem : SingleTon<EnergySystem>
{
    [SerializeField] private EnergyUI[] energyUIs = new EnergyUI[2];

    // This is a refill amount, not a cap on Energy granted by cards.
    private const int TURN_START_ENERGY = 3;
    private readonly int[] currentEnergy = { TURN_START_ENERGY, TURN_START_ENERGY };

    private void Start()
    {
        for (int i = 0; i < currentEnergy.Length; i++)
            UpdateEnergyUI(i);
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendEnergyGA>(SpendEnergyPerformer);
        ActionSystem.AttachPerformer<RefillEnergyGA>(RefillEnergyPerformer);
        ActionSystem.AttachPerformer<GainEnergyGA>(GainEnergyPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendEnergyGA>();
        ActionSystem.DetachPerformer<RefillEnergyGA>();
        ActionSystem.DetachPerformer<GainEnergyGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    public bool HasEnoughEnergy(int energy, int heroIndex = 0)
    {
        return currentEnergy[heroIndex] >= energy;
    }

    public int GainEnergy(CombatantView recipient, int amount)
    {
        // Resolve the recipient again at execution. Never award the active hero by default,
        // and skip a hero who died or left the registered party while the action was queued.
        if (amount <= 0 || recipient == null || recipient.CurrentHealth <= 0 ||
            HeroSystem.Instance == null ||
            !HeroSystem.Instance.TryGetHeroIndex(recipient, out int heroIndex) ||
            heroIndex < 0 || heroIndex >= currentEnergy.Length)
            return 0;

        int gained = (int)System.Math.Min((long)amount, (long)int.MaxValue - currentEnergy[heroIndex]);
        if (gained <= 0)
            return 0;

        currentEnergy[heroIndex] += gained;
        UpdateEnergyUI(heroIndex);
        return gained;
    }

    private IEnumerator GainEnergyPerformer(GainEnergyGA action)
    {
        GainEnergy(action.Caster, action.Amount);
        yield break;
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

        currentEnergy[heroIndex] = TURN_START_ENERGY;
        UpdateEnergyUI(heroIndex);
        yield return null;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        ActionSystem.Instance.AddReaction(new RefillEnergyGA(0));
        ActionSystem.Instance.AddReaction(new RefillEnergyGA(1));
    }

    // Way to reset energy
    public void ResetEnergy()
    {
        for (int i = 0; i < currentEnergy.Length; i++)
        {
            currentEnergy[i] = TURN_START_ENERGY;
            UpdateEnergyUI(i);
        }
    }

    public void ResetEnergy(int heroIndex)
    {
        currentEnergy[heroIndex] = TURN_START_ENERGY;
        UpdateEnergyUI(heroIndex);
    }

    private void UpdateEnergyUI(int heroIndex)
    {
        if (energyUIs != null && heroIndex >= 0 && heroIndex < energyUIs.Length && energyUIs[heroIndex] != null)
            energyUIs[heroIndex].UpdateEnergyText(currentEnergy[heroIndex]);
    }

}
