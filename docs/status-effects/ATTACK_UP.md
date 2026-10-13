# Attack Up

## Behavior

- Enum: the existing `StatusEffectType.ATTACKUP`.
- Any positive stack count increases each outgoing `DealDamageGA` hit by 25%.
- Stacks represent duration; additional stacks do not increase the percentage.
- Reapplication adds stacks through the existing `AddStatusEffect` card effect and `AddStatusEffectGA`.
- Round damage down, following the existing Vulnerable calculation.
- Apply Attack Up first, then the target's Vulnerable modifier, then its Block absorption.
- Remove one stack at the end of the affected unit's own turn, using the shared stack/UI removal path.
- The current hero-turn boundary is the `EnemyTurnGA` PRE phase. An enemy's turn currently consists of its `AttackHeroGA` and child reactions; decay occurs in that action's POST phase.
- Status tick damage sent directly to `CombatantView.Damage`, such as Shock, is outside the outgoing attack-action path.

## Implementation

1. Reuse the existing enum, sprite mapping, shared status application performer, and status UI.
2. `StatusEffectSystem.ModifyOutgoingDamage` reads the caster's stacks and calculates the bonus without consuming them.
3. `DamageSystem` uses that outgoing amount for each target, then applies the target's existing incoming-damage modifier.
4. `StatusEffectSystem` subscribes to the hero and enemy turn boundaries and queues `DecayAttackUpGA` for living units with stacks.
5. The decay performer removes one stack. Both registrations and subscriptions have matching cleanup in `OnDisable`.
6. Use the existing hero icon as the shared prefab's Attack Up icon so enemies can display the same status.

## Acceptance examples

| Input | Expected result |
| --- | --- |
| 8 damage, no Attack Up | 8 damage |
| 8 damage, 1 or 3 Attack Up stacks | 10 damage per hit |
| 6 damage with Attack Up | 7 damage after rounding down |
| 8 damage with Attack Up against Vulnerable | 12 damage after both calculations |
| The preceding hit against 5 Block | 5 Block consumed and 7 HP lost |
| Several hits in one unit turn | Every hit receives the bonus; stacks remain until turn end |
| Hero ends its turn with 1 stack | Stack and icon disappear before enemy attacks |
| Enemy attacks with 1 stack | Attack receives the bonus; stack and icon disappear afterward |
| Another unit takes its turn | Its own decay does not consume this unit's stack |
| Disable/re-enable the existing system | One decay per unit turn |

## Validation

Before the user's code-only clarification, the modified scripts compiled with Unity 6000.3.24f1 and six automated Play Mode validation groups passed:

1. Application, reapplication, fixed bonus, rounding, icon, and count.
2. Multiple targets/hits and Vulnerable/Block ordering through the damage performer.
3. Bonus attributed to the caster.
4. Hero PRE and enemy POST decay, independent units, and zero-stack icon removal through the action flow.
5. Enable/disable registration symmetry and decay only for the acting unit.
6. Safe handling of null/dead queued targets.

The temporary harness exercised the actual status, damage, UI, and action-flow paths with deterministic turn scheduling and skipped presentation waits. It was not a manual full gameplay playthrough. Unity was returned to Edit Mode.

Also before that clarification, the agent assigned the existing Attack Up sprite on the shared combatant prefab and installed the Unity Editor bridge. These are historical actions, not the workflow to follow going forward.

**Current rule:** The agent creates or modifies code and enums only. All future Unity Editor setup, compilation, and tests belong to the user. The acceptance examples above are the user's verification checklist.
