
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI
{
    private void Log(string message) //For debug purpose
    {
        Debug.Log($"[EnemyAI] {message}");
    }

    public bool TryAct(EnemyView enemy, List<PlayerView> heroes)
    {
        if (enemy == null || heroes == null || heroes.Count == 0) { 
            Log("Cannot act: enemy or living heroes are missing.");
            return false; 
        }

        Log($"--- {enemy.name}'s turn | HP: {enemy.CurrentHealth}/{enemy.MaxHealth} ---");

        // 1. 30% chance to use a random skill.
        float randomSkillRoll = Random.value;
        Log($"Priority 1: Random skill chance = 30%, roll = {randomSkillRoll:P1}");

        if (randomSkillRoll < 0.30f)
        {
            Log("Random skill roll SUCCESS. Trying a random skill.");

            if (TrySkill(enemy, enemy.Skills))
                return true;

            Log("Random skill attempt failed: no usable skill.  Go next >>>");
        }
        else
        {
            Log("Random skill roll FAILED. Go next >>>");
        }


        // 2. Attack the hero with less than 10% HP.
        PlayerView weak = heroes
            .Where(h => Ratio(h) < 0.10f)
            .OrderBy(Ratio)
            .FirstOrDefault();

        if (weak != null)
        {
            Log($"Priority 2: Low-HP target found: {weak.name} " +
                $"({weak.CurrentHealth}/{weak.MaxHealth} HP). Trying an attack skill.");

            if (TrySkill(enemy, Skills(enemy, EnemySkillPurpose.Attack), weak))
                return true;

            Log("No usable attack skill for the low-HP target.  Go next >>>");
        }
        else
        {
            Log("Priority 2: No hero below 10% HP.  Go next >>>");
        }


        // 3. 80% chance to heal below 15% HP.
        if (Ratio(enemy) < 0.15f)
        {
            float healRoll = Random.value;
            Log($"Priority 3: Enemy below 15% HP. Heal chance = 80%, " +
                $"roll = {healRoll:P1}");

            if (healRoll < 0.80f)
            {
                Log("Heal roll SUCCESS. Trying a healing skill.");

                if (TrySkill(enemy, Skills(enemy, EnemySkillPurpose.Heal)))
                    return true;

                Log("Heal attempt failed: no usable healing skill.  Go next >>>");
            }
            else
            {
                Log("Heal roll FAILED.  Go next >>>");
            }
        }
        else
        {
            Log("Priority 3: Enemy HP is not below 15%; skipping heal.  Go next >>>");
        }


        // 4. Attack a stunned hero (placeholder until stun exists).
        PlayerView stunned = heroes.FirstOrDefault(IsStunned);

        if (stunned != null)
        {
            Log($"Priority 4: Stunned hero found: {stunned.name}. Trying an attack skill.");

            if (TrySkill(enemy, Skills(enemy, EnemySkillPurpose.Attack), stunned))
                return true;
        }
        else
        {
            Log("Priority 4: No stunned hero found (stun detection is not implemented yet).  Go next >>>");
        }



        // 5. Use a buff skill not already used.
        EnemySkill buff = Skills(enemy, EnemySkillPurpose.Buff)
            .FirstOrDefault(s => !enemy.HasUsedBuff(s));

        if (buff != null)
        {
            Log($"Priority 5: Trying buff skill '{buff.SkillName}'.");

            if (TrySkill(enemy, new List<EnemySkill> { buff }))
            {
                enemy.MarkBuffUsed(buff);
                Log($"Buff skill '{buff.SkillName}' was used and marked as used.");
                return true;
            }

            Log($"Buff skill '{buff.SkillName}' could not be used.  Go next >>>");
        }
        else
        {
            Log("Priority 5: No unused buff skill available.  Go next >>>");
        }



        // 6. 40% chance to use an attack skill.
        float attackRoll = Random.value;
        Log($"Priority 6: Attack skill chance = 40%, roll = {attackRoll:P1}");

        if (attackRoll < 0.40f)
        {
            Log("Attack skill roll SUCCESS. Trying an attack skill.");

            if (TrySkill(enemy, Skills(enemy, EnemySkillPurpose.Attack)))
                return true;

            Log("Attack skill attempt failed: no usable attack skill.  Go next >>>");
        }
        else
        {
            Log("Attack skill roll FAILED.  Go next >>>");
        }

        Log($"{enemy.name} did not use a skill. Normal attack fallback will be used.");
        return false;
    }


    // METHODS -----------------------------------

    private bool TrySkill(EnemyView enemy, List<EnemySkill> skills, PlayerView forcedTarget = null)
    {
        if (skills == null || skills.Count == 0) { 
            Log($"{enemy.name}: No skills available for this decision.");
            return false; 
        }

        // Try skills in random order, so a skill with no valid targets
        // doesn't prevent another skill from being considered.
        foreach (EnemySkill skill in skills.OrderBy(_ => Random.value))
        {
            if (skill == null || skill.Effects == null || skill.Effects.Count == 0) { continue; }

            Log($"Checking skill: '{skill.SkillName}'");

            bool queuedAny  = false;

            // Find the largest repeat count among this skill's effects.
            int maxRepeats = skill.Effects
                .Where(e => e != null)
                .Select(e => e.RepeatCount)
                .DefaultIfEmpty(0)
                .Max();

            // Process one repeat round at a time.
            for (int repeat = 0; repeat < maxRepeats; repeat++)
            {
                Log($"'{skill.SkillName}': Repeat round {repeat + 1}/{maxRepeats}");

                // Preserve the effect order configured in the Inspector.
                foreach (EnemySkillEffect effect in skill.Effects)
                {
                    if (effect == null ||
                        effect.Effect == null ||
                        effect.TargetMode == null)
                    {
                        Log("Skipping effect: Effect or TargetMode is missing.");
                        continue;
                    }

                    // This effect has no more repeats.
                    if (repeat >= effect.RepeatCount)
                        continue;

                    // Roll independently on every repeat.
                    int roll = Random.Range(0, 100);
                    bool passed = roll < effect.ChancePercent;

                    Log(
                        $"Effect {effect.Effect.GetType().Name} | " +
                        $"Repeat {repeat + 1}/{effect.RepeatCount} | " +
                        $"Chance {effect.ChancePercent}% | Roll {roll} | " +
                        $"{(passed ? "SUCCESS" : "FAILED")}"
                    );

                    if (!passed)
                        continue;

                    List<CombatantView> targets;

                    if (forcedTarget != null && effect.TargetMode is OneHeroTM)
                    {
                        targets = new List<CombatantView>();

                        if (forcedTarget.CurrentHealth > 0)
                            targets.Add(forcedTarget);
                    }
                    else
                    {
                        targets = effect.TargetMode.GetTargets(enemy);
                    }

                    if (targets == null)
                        continue;

                    // Ignore targets already dead when this effect is queued.
                    targets.RemoveAll(t => t == null || t.CurrentHealth <= 0);

                    if (targets.Count == 0)
                    {
                        Log("No living targets. Effect skipped.");
                        continue;
                    }

                    foreach (CombatantView target in targets)
                    {
                        Log($"Target: {target.name} " +
                            $"({target.CurrentHealth}/{target.MaxHealth} HP)");
                    }

                    ActionSystem.Instance.AddReaction(
                        new PerformEffectGA(effect.Effect, targets, enemy)
                    );

                    queuedAny = true;
                    Log($"Queued {effect.Effect.GetType().Name}.");
                }
            }

            if (queuedAny)
            {
                Log($"Skill '{skill.SkillName}' queued successfully.");
                return true;
            }

            Log($"Skill '{skill.SkillName}' had no successful effects.");
        }

        return false;
    }

    private static List<EnemySkill> Skills(EnemyView enemy, EnemySkillPurpose purpose) 
        => enemy.Skills?
            .Where(s => s != null && s.Purpose == purpose)
            .ToList() ?? new List<EnemySkill>();


    private static float Ratio(CombatantView unit) => unit.MaxHealth > 0
            ? (float)unit.CurrentHealth / unit.MaxHealth : 0f;


    private bool IsStunned(PlayerView hero)
    {
        // TODO: Connect to the actual stun gauge/stunned state.
        return false;
    }

    public List<PlayerView> GetLivingHeroes()
    {
        var heroes = new List<PlayerView>
        {
            HeroSystem.Instance.playerView,
            HeroSystem.Instance.secondPlayerView
        };

        heroes.RemoveAll(h => h == null || h.CurrentHealth <= 0);
        return heroes;
    }
}
