using System.Collections;
using UnityEngine;

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageVFX;
    [SerializeField] private GameObject gameOverPanel;

    private bool gameOverTriggered;

    private void Start()
    {
        // Only trigger game over once
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
    }

    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        foreach (var target in dealDamageGA.Targets)
        {
            if (gameOverTriggered) { yield break; }

            target.Damage(dealDamageGA.Amount);

            if (damageVFX != null)
            {
                Instantiate(
                    damageVFX,
                    target.transform.position,
                    Quaternion.identity
                );
            }
            yield return new WaitForSeconds(0.15f);

            if (target.CurrentHealth <= 0)
            {
                if (target is EnemyView enemyView)
                {
                    KillEnemyGA killEnemyGA = new(enemyView);
                    ActionSystem.Instance.AddReaction(killEnemyGA);
                }
                else if (target is PlayerView)
                {
                    CheckGameOver();
                }
            }
        }
    }

    private void CheckGameOver()
    {
        HeroSystem heroSystem = HeroSystem.Instance;

        bool firstHeroDead =
            heroSystem.playerView == null ||
            heroSystem.playerView.CurrentHealth <= 0;

        bool secondHeroDead =
            heroSystem.secondPlayerView == null ||
            heroSystem.secondPlayerView.CurrentHealth <= 0;

        // One defeated hero is okay. Both defeated means Game Over.
        if (firstHeroDead && secondHeroDead && !gameOverTriggered)
        {
            gameOverTriggered = true;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            Debug.Log("GAME OVER: Both heroes have been defeated.");
        }
    }

    public void ResetGameOverState()
    {
        gameOverTriggered = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}
