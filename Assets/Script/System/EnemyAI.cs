
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI
{
    private sealed class PlannedTurn
    {
        public readonly List<PerformEffectGA> Effects = new();
        public readonly List<EnemySkillEffect> DisplayEffects = new();
        public bool IsNormalAttack;
        public PlayerView AttackTarget;
        public EnemySkill BuffToMarkUsed;
    }

    private readonly Dictionary<EnemyView, PlannedTurn> plannedTurns = new();

    // Selecting an intent must not execute effects or consume a once-per-fight buff.
    public void PlanTurn(EnemyView enemy, List<PlayerView> heroes)
    {
        if (enemy == null)
            return;

        ForgetPlan(enemy);
        enemy.ClearActionDisplay();
        if (enemy.CurrentHealth <= 0 || enemy.IsStunned || heroes == null || heroes.Count == 0)
            return;

        PlannedTurn plan = new();
        if (TryChooseSkill(enemy, heroes, plan))
        {
            enemy.ShowSkillActions(plan.DisplayEffects);
        }
        else
        {
            plan.IsNormalAttack = true;
            plan.AttackTarget = heroes[Random.Range(0, heroes.Count)];
            enemy.ShowNormalAttack();
        }
        plannedTurns[enemy] = plan;
    }

    public void ExecutePlannedTurn(EnemyView enemy)
    {
        if (enemy == null)
            return;
        if (enemy.CurrentHealth <= 0 || enemy.IsStunned)
        {
            ForgetPlan(enemy);
            enemy.ClearActionDisplay();
            return;
        }

        // Covers enemies added after the player-turn preview was prepared.
        if (!plannedTurns.TryGetValue(enemy, out PlannedTurn plan))
        {
            PlanTurn(enemy, GetLivingHeroes());
            if (!plannedTurns.TryGetValue(enemy, out plan))
                return;
        }
        plannedTurns.Remove(enemy);

        if (plan.IsNormalAttack)
        {
            PlayerView target = plan.AttackTarget;
            if (target == null || target.CurrentHealth <= 0)
                target = GetLivingHeroes().FirstOrDefault();
            if (target != null)
                ActionSystem.Instance.AddReaction(new AttackHeroGA(enemy, target));
            return;
        }

        bool queuedAny = false;
        foreach (PerformEffectGA effect in plan.Effects)
        {
            // The player may have killed a selected target after seeing the intent.
            // Keep the chosen effects and successful rolls; never pick a new skill here.
            List<CombatantView> targets = effect.Targets
                .Where(target => target != null && target.CurrentHealth > 0).ToList();
            if (targets.Count == 0)
                continue;
            ActionSystem.Instance.AddReaction(new PerformEffectGA(effect.Effect, targets, enemy));
            queuedAny = true;
        }
        if (queuedAny && plan.BuffToMarkUsed != null)
            enemy.MarkBuffUsed(plan.BuffToMarkUsed);
    }

    public void ForgetPlan(EnemyView enemy)
    {
        if (enemy != null)
            plannedTurns.Remove(enemy);
    }

    public void ClearPlans() => plannedTurns.Clear();

    private void Log(string message) //For debug purpose
    {
        Debug.Log($"[EnemyAI] {message}");
    }

    private bool TryChooseSkill(EnemyView enemy, List<PlayerView> heroes, PlannedTurn plan)
    {
        if (enemy == null || heroes == null || heroes.Count == 0) { 
            Log("Cannot act: enemy or living heroes are missing.");
            return false; 
        }

        Log($"--- Planning {enemy.DisplayName}'s next turn | HP: {enemy.CurrentHealth}/{enemy.MaxHealth} ---");

        // 1. 30% chance to use a random skill.
        float randomSkillRoll = Random.value;
        Log($"Priority 1: Random skill chance = 30%, roll = {randomSkillRoll:P1}");

        if (randomSkillRoll < 0.30f)
        {
            Log("Random skill roll SUCCESS. Trying a random skill.");

            if (TrySkill(enemy, enemy.Skills, plan))
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

            if (TrySkill(enemy, Skills(enemy, EnemySkillPurpose.Attack), plan, weak))
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

                if (TrySkill(enemy, Skills(enemy, EnemySkillPurpose.Heal), plan))
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


        // 4. Attack a stunned hero.
        PlayerView stunned = heroes.FirstOrDefault(IsStunned);

        if (stunned != null)
        {
            Log($"Priority 4: Stunned hero found: {stunned.name}. Trying an attack skill.");

            if (TrySkill(enemy, Skills(enemy, EnemySkillPurpose.Attack), plan, stunned))
                return true;
        }
        else
        {
            Log("Priority 4: No stunned hero found.  Go next >>>");
        }



        // 5. Use a buff skill not already used.
        EnemySkill buff = Skills(enemy, EnemySkillPurpose.Buff)
            .FirstOrDefault(s => !enemy.HasUsedBuff(s));

        if (buff != null)
        {
            Log($"Priority 5: Trying buff skill '{buff.SkillName}'.");

            if (TrySkill(enemy, new List<EnemySkill> { buff }, plan))
            {
                plan.BuffToMarkUsed = buff;
                Log($"Buff skill '{buff.SkillName}' is planned; it will be marked used when queued.");
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

            if (TrySkill(enemy, Skills(enemy, EnemySkillPurpose.Attack), plan))
                return true;

            Log("Attack skill attempt failed: no usable attack skill.  Go next >>>");
        }
        else
        {
            Log("Attack skill roll FAILED.  Go next >>>");
        }

        Log($"{enemy.DisplayName} did not use a skill. Normal attack fallback will be used.");
        return false;
    }


    // METHODS -----------------------------------

    private bool TrySkill(EnemyView enemy, List<EnemySkill> skills, PlannedTurn plan, PlayerView forcedTarget = null)
    {
        if (skills == null || skills.Count == 0) { 
            Log($"{enemy.DisplayName}: No skills available for this decision.");
            return false; 
        }

        // Try skills in random order, so a skill with no valid targets
        // doesn't prevent another skill from being considered.
        foreach (EnemySkill skill in skills.OrderBy(_ => Random.value))
        {
            if (skill == null || skill.Effects == null || skill.Effects.Count == 0) { continue; }

            Log($"Checking skill: '{skill.SkillName}'");

            // Resolve chance and targets once while planning, without queuing combat.
            List<EnemySkillEffect> selectedEffects = new();
            List<PerformEffectGA> plannedEffects = new();

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

                    // Ignore targets already dead when this effect is planned.
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

                    plannedEffects.Add(new PerformEffectGA(effect.Effect, targets, enemy));

                    selectedEffects.Add(effect);
                    Log($"Planned {effect.Effect.GetType().Name}.");
                }
            }

            if (selectedEffects.Count > 0)
            {
                plan.Effects.AddRange(plannedEffects);
                plan.DisplayEffects.AddRange(selectedEffects);
                Log($"Skill '{skill.SkillName}' planned successfully.");
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
        return hero != null && hero.CurrentHealth > 0 && hero.IsStunned;
    }

    public List<PlayerView> GetLivingHeroes()
    {
        if (HeroSystem.Instance == null)
            return new List<PlayerView>();

        var heroes = new List<PlayerView>
        {
            HeroSystem.Instance.playerView,
            HeroSystem.Instance.secondPlayerView
        };

        heroes.RemoveAll(h => h == null || h.CurrentHealth <= 0);
        return heroes;
    }
}
