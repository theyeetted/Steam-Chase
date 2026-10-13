# Adding a status effect

Follow this workflow for each status-effect request. Read [the video reference](VIDEO_REFERENCE.md) for evidence and use [the specification template](EFFECT_SPEC_TEMPLATE.md) for the new effect's decisions.

## User-defined implementation boundary

For status-effect implementation, the agent creates or modifies **code and enum files only**. The user exclusively makes Unity Editor changes and runs tests. This instruction overrides any operational setup or verification steps described below or demonstrated in the videos.

- Do not operate the Unity Editor, enter Play Mode, compile, or run tests.
- Do not modify scenes, prefabs, card/data assets, Inspector assignments, package configuration, or other Editor settings. Do not install Editor tooling.
- Do not use scripts or command-line tools to automate those user-owned changes.
- Read relevant source and serialized assets as needed to understand integration. Review the code diff for consistency.
- Provide a concrete handoff listing the Editor setup the user needs to perform and the checks with expected results. State that tests were not run by the agent.
- Reuse an existing enum member when present. Change these boundaries only when the user explicitly requests it.

The architecture below comes from the three supplied videos. The specification and verification steps are implementation checklists derived from that architecture, not additional tutorial claims. The project implementation was not inspected when these instructions were written; match tutorial names to actual code only when inspection is permitted by the current task.

## 1. Define the behavior

Record the effect's type, stack meaning, targets, application source, trigger, result, consumption/decay, and icon. State the exact timing and the order of gameplay and stack changes.

Default to the demonstrated additive stack model when that matches the request. Do not assume that every status expires at turn end, ticks every frame, bypasses Armor, or decays like Burn. Caps, refresh rules, duration, cleansing, immunity, and persistence need their own specification if relevant. Resolve material gameplay ambiguities before implementing the affected behavior.

## 2. Reuse the shared application path

The videos establish this flow:

```text
Card data: effect type + stack count + target mode
  -> AddStatusEffectEffect.GetGameAction(targets, caster)
  -> AddStatusEffectGA(type, stackCount, targets)
  -> StatusEffectSystem.AddStatusEffectPerformer
  -> each target's CombatantView.AddStatusEffect
  -> StatusEffectsUI.UpdateStatusEffectUI(type, resultingStacks)
```

- Add the new member to `StatusEffectType` using the project's established naming convention.
- Reuse `AddStatusEffectEffect` and `AddStatusEffectGA` for ordinary stack application. A different status type alone does not require another application action or card-effect class.
- Configure the intended target mode. The demonstrated self-target mode is `HeroTM`; Firestorm targets all enemies.
- Keep stack ownership on the combatant. Read through `GetStatusEffectStacks`, add through `AddStatusEffect`, and consume through `RemoveStatusEffect` rather than changing the dictionary from effect systems.
- Preserve the demonstrated behavior: absent types read as zero; repeated application adds stacks; removal deletes a type at zero or below; both mutation paths refresh the UI using the resulting count.

These shared components may already exist. Extend them where necessary instead of rebuilding the tutorial setup for every effect.

## 3. Implement the gameplay at its trigger

Applying stacks and executing their gameplay are separate responsibilities. Choose the integration point required by the effect.

### Damage interaction: Armor example

Armor is handled inside `CombatantView.Damage(int damageAmount)`. It consumes up to the incoming damage amount, then passes only positive remaining damage to the existing health-damage logic. The UI changes through the shared removal method. Fully absorbed damage does not enter the tutorial's health-hit feedback block.

Use this as the example for an effect that changes an existing operation. Do not create an unrelated turn reaction for a damage-time rule, and do not add an Armor expiration rule absent a requirement.

### Triggered execution: Burn example

Burn stacks are applied with `AddStatusEffectGA`. `ApplyBurnGA` instead represents one execution of Burn and carries the damage and target. `BurnSystem` performs that action.

For an analogous triggered effect:

1. At the specified trigger, read the relevant combatant's stacks and do nothing when there are none.
2. Create an execution action with the data required for that occurrence.
3. Queue it through `ActionSystem.Instance.AddReaction` at the intended point in the existing action sequence.
4. In its performer, apply the gameplay result and change stacks in the specified order using the shared combatant methods.
5. Include any required presentation and coroutine pacing in that performer.

For Burn specifically, the performer creates the VFX at the target, calls the target's damage method, removes one `BURN` stack, and waits one second. Damage uses the pre-decay stack amount supplied by the trigger. The one-second wait is presentation pacing, not the status's duration. Because it uses the ordinary damage method, Armor absorption applies through the Episode 23 path; this is a deduction from the two implementations, not a separate demonstrated immunity rule.

The enemy trigger is in `EnemySystem.EnemyTurnPerformer`, which queues each enemy's Burn before its attack reaction. The hero trigger is `HeroSystem.EnemyTurnPostReaction`, subscribed to `EnemyTurnGA` with `ReactionTiming.POST`; it queues hero Burn before the draw-card reaction. Preserve those queue locations for Burn. Choose the phase independently for a new effect rather than labeling both merely “turn end.”

## 4. Register and place the behavior

- Pair performer attachment in `OnEnable` with detachment in `OnDisable`, following the ActionSystem API actually present in the permitted project context.
- If a system subscribes to an action phase, pair the subscription and unsubscription too. Keep the phase explicit.
- If a reaction moves between systems, move its lifecycle registrations and preserve the pre-existing behavior. Episode 24 moves the hero's enemy-turn reactions from `CardSystem` to `HeroSystem` so they can handle more than card operations.
- Tell the user which required system component to add to the scene and which references to assign, only when a system is new or missing. The user performs these Editor changes.

Reviewing for duplicate registrations and repeated execution is a verification safeguard derived from this lifecycle pattern.

## 5. Connect the icon and data

- Add a sprite reference and a matching `GetSpriteByType` mapping in `StatusEffectsUI`, or extend the equivalent existing mapping.
- Reuse `StatusEffectUI` for the icon and TMP stack count. Keep one UI instance per type on each combatant.
- Preserve zero-stack behavior: remove the UI's dictionary entry and destroy its object; do nothing if it is already absent.
- List the icon, UI prefab, text/image references, and combatant status UI references the user must assign. Include both hero and enemy views if both can receive the effect.
- If the request includes a card, give the user its effect type, amount, target mode, and any test-deck setup to configure in the Editor.
- Follow the existing layout. The video's world-space canvas scale and icon size are setup examples, not dimensions to force on every project.

## 6. Hand off verification to the user

Provide applicable cases from the specification template, with concrete expected values, for the user to run. Do not run them yourself. These are checks derived from the videos rather than claims that all were demonstrated there.

| Check | Expected result |
| --- | --- |
| First application | Correct target, stacks, icon, and count |
| Reapplication | Specified stacking rule; no duplicate icon for that type |
| Multiple targets | Independent stack totals and UI for each combatant |
| Trigger | Exactly the intended combatant(s), action phase, and execution count |
| Consumption | Correct remainder; zero/over-removal leaves no active entry or icon |
| Unaffected type | Other statuses and their icons remain correct |
| Re-enable a callback-owning system | One execution per trigger; no duplicate registrations |
| Scene/prefab wiring | Required components and references assigned |

Useful regression examples, when those effects are involved:

- **Armor:** with 5 Armor, a 3-damage hit leaves 2 Armor and costs 0 HP; a 5-damage hit leaves 0 Armor and costs 0 HP; an 8-damage hit leaves 0 Armor and costs 3 HP.
- **Burn:** with 3 Burn and no Armor, one execution deals 3 damage and leaves 2 stacks; later executions deal 2 then 1 and remove the icon. Reapplying stacks must use the shared additive path.
- **Burn with Armor:** verify the ordinary damage path absorbs damage as expected while Burn still consumes its one stack.

The user runs compilation, gameplay, and presentation checks. The agent reviews code changes without executing compilation or tests, and reports verification as pending user testing. Do not claim checks passed without user-provided results.

## 7. Report the result

Summarize the implemented behavior, trigger and stack rules, and code/enum files changed. List required Editor work and concrete tests for the user. State that no Editor changes or tests were performed by the agent. Identify any intentional departure from the tutorial pattern and any unverified integration detail; do not describe the feature as fully integrated while user setup or verification remains pending.
