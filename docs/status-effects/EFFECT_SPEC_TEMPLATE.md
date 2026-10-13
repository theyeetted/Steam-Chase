# New status-effect specification

Use this before implementation alongside [the workflow](ADDING_STATUS_EFFECTS.md). Fill it from the user's request and permitted project context. Mark unresolved gameplay choices explicitly; do not invent them from the effect's name.

Implementation scope: the agent changes code and enum files only. Unity Editor setup and all testing belong to the user. Do not automate that setup or run compilation/tests.

## Behavior

| Decision | Specification |
| --- | --- |
| Name and `StatusEffectType` member | |
| Player-facing description | |
| Meaning of one stack | |
| Who receives the effect | |
| How it is applied: card, action, or other source | |
| Initial stack amount | |
| Reapplication: add, replace, refresh, or another rule | |
| Maximum stacks, if any | |
| Trigger: immediate, damage handling, or named action phase | |
| Which combatants are processed at that trigger | |
| Gameplay result and action(s) it produces | |
| Damage interaction with Armor, if applicable | |
| Consumption or decay amount | |
| Ordering of gameplay result and stack removal | |
| Expiration/reset conditions | |
| Icon and stack-count presentation | |

The videos demonstrate additive application, Armor consumption during damage, and Burn behavior. They do not establish universal rules for caps, duration, cleansing, immunity, or every future status. Specify those only when relevant.

## Integration checklist

- Enum member and sprite mapping:
- Shared `AddStatusEffectEffect` / `AddStatusEffectGA` application path:
- Trigger, subscriber, and reaction phase, if needed:
- Dedicated gameplay action and performer, if needed:
- Matching attach/detach and subscribe/unsubscribe lifecycle:
- Target mode and card/data configuration, if applicable:
- Scene component and prefab references for the user to assign, if needed:
- Existing components reused:
- Any deliberate departure from the videos, and why:

## Acceptance examples

Record concrete inputs and expected outcomes for the applicable cases:

1. Apply to a combatant with no existing stacks.
2. Apply again to the same combatant.
3. Apply to another combatant and confirm independent state.
4. Trigger the effect at the specified time and confirm its action order.
5. Consume fewer than, exactly, and more than the remaining stacks.
6. Reach zero and confirm that the icon disappears.
7. Interact with Armor or another relevant effect.
8. Disable/re-enable the system, if it registers callbacks, and confirm one execution per trigger.

## Completion record

- Code and enum files changed:
- Editor setup for the user:
- Tests and expected results for the user:
- Agent code review performed:
- User-reported verification results, if supplied:
- Tests not run by the agent: user owns all testing.
- Remaining behavior decisions or limitations:
