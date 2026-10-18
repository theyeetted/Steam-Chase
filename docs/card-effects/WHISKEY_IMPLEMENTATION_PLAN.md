# Whiskey — Shared Effect Implementation Plan

[Card map](../../Whiskey%20missing%20card%20effect%20logic.md) · [Shared effect design](SHARED_EFFECT_DESIGN.md)

## Delivery approach

Implement one usable slice at a time. Each slice must improve a card while adding a reusable capability for later cards. Proposed names in the design are not instructions to scaffold the entire framework first.

This plan is documentation only. The five source-file changes from the Got ‘em draft remain separate, unverified work. No new implementation, asset changes, compilation, or gameplay tests were performed for this plan.

## Recommended order

| Step | Small deliverable | Dependencies | Cards advanced | Completion boundary |
|---|---|---|---|---|
| **1** | Generalize Got ‘em into successful status application + owner filter + existing draw payload. Keep current authoring/UI as an adapter. | Existing action queue, status mutation, hero-indexed draw. | Got ‘em; reusable trigger/payload structure for I’ll get you back!. | One trigger path, no double draw, combat reset, owner-only filtering, stable enum. No general scheduler yet. |
| **2** | Separate condition subjects from recipients; add actual-Stun predicate, branch, and ordered payload. | Existing conditions/target modes. Add only the runtime context required here. | I'm Tuff, Socket to ’Em!, Pummel Down, Opportunist; groundwork for resonance/Wrench Sweep. | `Any` rewards once; branches are exclusive; old assets preserve defaults. Resolve timing/scope before claiming a card complete. |
| **3** | Add gain Energy, heal, bounded status removal, and owner-aware authored drawing, one operation at a time. | Existing status, health, Energy, draw systems; step 2 for conditional cards. | Roll With It, Cheap Opening, Bread, Bolt Stripper. | Explicit ownership/bounds, Bread targeting and Energy overflow decided; user performs asset setup. |
| **4** | Add resolved attack facts and hero history; use them for a boolean condition and scaled amount. | Agreed attack definition/window; add event context only where needed. | Throwing hand, Return to Sender, dramatic exit. | Blocked hits count, status/skipped damage does not, wrappers do not double count, history resets. Shared base + multiplier × count calculation. |
| **5** | Add named per-target results and small value/stack transforms, one consumer at a time. | Existing damage/status paths; value-source pattern from step 4. | Wrench Sweep, Stress Test!, Watch It Blow!; resonance if it needs resolved damage. | Results survive nested reactions; actual hit subjects, Stress cap/UI, and damage-to-Stun units are preserved. |
| **6** | Extend rules to attacks received, explicit lifetimes, and one-shot scheduled rewards; scope extra Stun to a counter grant. | Steps 1/4, relevant operations, phase-callback audit. | I’ll get you back!, Brace for It!, Full Tune-Up. | One Brace reward per activation, one expiry per phase, no duplicate retaliation/leaked Stun. Duration/window rules decided. |
| **7** | Record accepted Attack-card plays; extract a shared effect resolver; add replay and scheduled execution. | Existing `Card.Type`; history, scheduler, explicit replay policies. | following up, Gear overheat. | Works outside hand with fresh context; no duplicate spending/discard, ambiguous retargeting, or replay loops. |
| **8 — conditional** | Execute `RepeatCount` after inspecting affected card assets. | Shared resolver or a smaller shared effect-loop extraction. | Beat down, This gear is for you!, Overcrank Strike if their configuration needs it. | Two authored hits remain two; reactions/death between hits. No chance subsystem required. |

Step 1 is the next recommended coding task because it improves Got ‘em before another trigger is added. Steps 2–3 do not depend on a complete history/scheduler/replay system. Do not mark every card in a group complete because one shared helper exists.

## Confirmed rules and open decisions

| Topic | Status / required decision | Affected work |
|---|---|---|
| Got ‘em duration | **User confirmed:** rest of combat. | Preserve in step 1. |
| Got ‘em source | **User confirmed:** Whiskey's own applications only, not Ade's. | Owner filter in step 1. |
| Got ‘em frequency/replays | **Draft defaults:** one draw per enemy application, independent of stacks; replays add strength. Not separately confirmed. | Preserve during refactor unless revised; expose stacking as a rule policy. |
| I’ll get you back! duration | Choose a finite window or rest of combat before authoring its rule. | Step 6. |
| “This turn” across two heroes | Define player phase versus full round and received-attack history reset. Brace needs a window including intended attacks and an explicit next-turn payout. | Steps 4/6. |
| Received-attack semantics | Decide one enemy action versus each landed strike, and whether attacks before activation qualify for Brace. Keep blocked hits as attacks; exclude status damage. | History and reactive rewards. |
| dramatic exit count | Define window and qualifying normal/Extra/Follow-Up/counter attacks; how multi-target/multi-hit actions count. | Step 4. |
| resonance condition/amount | Selected enemy or any enemy? Base 12 or earlier resolved damage? Does the primary enemy also receive the area hit? | Steps 2/5. |
| Roll With It scope | Clarify “any target enemy”: selected target or any enemy on board. | Steps 2/3. |
| Conditional timing | Confirm pre-hit versus post-hit state for Socket, Opportunist, Cheap Opening, and similar bonuses. Define Wrench Sweep's hit-status snapshot. | Steps 2/5. |
| Bread recipient | Automatic other hero or manual ally choice; may Whiskey heal himself? No lowest-health selector is required by the text. | Step 3; manual choice also requires targeting changes. |
| Energy grants | Refill is currently 3. Decide whether card grants can exceed it; do not silently clamp away a promised grant. | Step 3. |
| Watch It Blow! conversion | Damage before Block or actual HP lost? Does 40% produce flat gauge points? Define rounding. | Step 5. |
| Multiple counter grants | Preserve combined damage or emit distinct strikes? Which strike payloads belong to each grant and how do they stack? | Step 6. |
| Gear overheat selection | Which Attack card, from which zone, chosen how, with which target? | Step 7. |
| Replay/scheduled execution | Original target or new target; pay Energy or not; move/consume a physical card or replay its definition; record a play or just execution? Define dead-target/no-target behavior and recursion limits. | following up, Gear overheat. |
| following up history scope | First Attack card by Whiskey or either hero; behavior if none exists. | Step 7. |

An unresolved decision blocks only dependent behavior, not independent reusable operations. These are gameplay choices, not facts supplied by the videos or current framework.

## Verification handoff

These checks are for the user after corresponding code and setup exist. They have **not** been run by the agent. Damage examples are base values unless stated otherwise; ordinary modifiers and Block still apply.

| Capability | Example | Expected result |
|---|---|---|
| Trigger filtering | Activate Got ‘em once; Whiskey applies 2 Vulnerable to one enemy, then Ade applies Vulnerable. | Whiskey draws 1 from his application, 0 from Ade's. No draw on decay. |
| Trigger scope/stacking | Apply Vulnerable to three living enemies; activate Got ‘em a second time and apply to one. | Draft defaults: 3 draws, then 2. No duplicate legacy hook. |
| Trigger lifecycle | Disable/re-enable owning system, then restart combat. | No duplicate callback; no old rules/jobs remain after restart. |
| Aggregate condition | Two enemies have Vulnerable; play I'm Tuff. | Exactly 3 Attack Up, not 6. |
| Branch selection | Pummel Down on non-Stunned/Stunned targets. | One base-5/base-16 hit respectively, never both. |
| Exclusive reward | Roll With It's condition is false/true. | 8 Block or 2 Energy respectively, never both. |
| Ordered payload | Cheap Opening's Stun condition passes. | Draw 1 and gain 1 Energy for the same owner from one decision. |
| Numeric bounds | Bolt Stripper finds 6 remaining Block; Bread heals an ally missing 4 HP. | Remove 6 without going negative; heal 4 without exceeding maximum. |
| History | Whiskey receives a fully blocked hit, then a status tick. | Received-attack count increases for the hit only. |
| Scaled values | Return to Sender sees 3 received attacks; dramatic exit sees 4 qualifying performed attacks. | Base damage 28 and 26. |
| Hit results | Wrench Sweep hits two Aggravated enemies and kills one. | Single 6 Block reward using agreed snapshot; death does not erase the hit. |
| Stack transform | Stress Test! starts from 0, 40, and 60 Stress. | Ends at 0, 80, 99; draw 2; correct status display. |
| Result isolation | Watch It Blow! detonates Stress while other reactions also deal damage. | Stun derives only from its detonation under chosen conversion; Stress remains. |
| Scheduled reward | Several qualifying attacks occur in one Brace activation's window. | Exactly 1 draw at agreed next-turn boundary; none without a qualifying attack. |
| Counter scope | Full Tune-Up counter and unrelated ordinary attack occur. | Only eligible counter gets extra 15% maximum-gauge fill; none after expiry. |
| Card execution | Replay first accepted Attack card or execute Gear's queued card. | Agreed owner/target/resource policy; no duplicate zone movement or recursive replay. |
| Separate repeats | Hit 1 of a two-hit effect kills the enemy. | Hit 2 skips that dead target; no extra history/status/counter event for the skipped hit. |

## Working rules

- Use the existing agent worktree at `C:/Users/PHUN/.codex/worktrees/f8a6/Steam Chase`; preserve active-checkout changes and existing work.
- Extend interfaces compatibly; preserve enum numbers, managed-reference types, and fixed-value defaults.
- Keep amounts/card settings in data. Add a class for a distinct reusable mechanic, not a card title or different number.
- Use the same mutation/action paths for immediate, conditional, triggered, delayed, and replayed effects.
- Follow [AGENTS.md](../../AGENTS.md) for future code work: the user performs Editor changes, compilation, and tests. Hand off exact card configuration, required components/references, and expected checks.
- Report code, asset setup, and verification separately. The Got ‘em draft is not yet a verified card, and these groups are not implemented merely because they are specified.
