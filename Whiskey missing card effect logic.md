# Whiskey — Shared Card Effect Implementation Map

Build cards by composing reusable conditions, value calculations, operations, and timing rules. Card names and numbers belong in card data; shared gameplay code should not contain a branch for each card.

This document reorganizes the **21 Whiskey cards explicitly identified in the original missing-feature list**, plus **3 conditional repeat-attack dependencies**. A card can belong to several groups. Group membership describes the pieces it needs, not a separate implementation for that card.

## Documents

- **This file:** shared groups and every card's logic breakdown.
- [Shared effect design](docs/card-effects/SHARED_EFFECT_DESIGN.md): responsibilities, execution contracts, existing code to reuse, and the Got ‘em migration.
- [Implementation plan](docs/card-effects/WHISKEY_IMPLEMENTATION_PLAN.md): small implementation steps, dependencies, open decisions, and verification cases.

## Evidence and current status

Sources: [missing-feature list](<D:/universityProject/Game Logic/missing feature list.md>), [GDD](<D:/universityProject/Game Logic/Asset for game/Steam chasing GDD.md>), and relevant source inspected on **17 October 2026**, based on `MergeforDemo` commit `46e2725` plus the local Got ‘em code draft.

- The original list is the scope baseline, not proof that every listed feature is still absent.
- **Got ‘em has a local code draft; Editor setup and gameplay verification are pending.** The user confirmed **rest of combat** and **Whiskey's applications only**. The draft also uses one draw per enemy application, not per Vulnerable stack, and additive replays. Those last two rules are implementation defaults, not separately confirmed GDD rules.
- **Attack-card classification already exists:** `CardData.Type` and `Card.Type` use `CardType.Attack/Skill`. Reuse it; the original claim that classification is missing is outdated.
- `DrawCardsGA` supports a hero index, but `DrawCardsEffect` still defaults to index 0. Reusable effects must resolve the owner explicitly. Got ‘em's draft already routes its own draws to its caster.
- Conditions, status application, damage, Block, percentage Stun fill, counters, and configurable Stress detonation already have implementation paths. Extend these paths instead of rebuilding them.
- The design below is a **proposal**, not an implemented framework. This documentation update does not change code, card assets, or the Editor.

## 1. Shared implementation groups

| Group | Reusable responsibility | Similarity across cards | Example consumers |
|---|---|---|---|
| **G1 — Conditions, scopes, and branches** | Inspect a subject/set, evaluate once or per target, select a branch, then resolve recipients independently. | “If the enemy…”, “if any enemy…”, and “instead” are variants of one conditional composition. | resonance, I'm Tuff, Socket to ’Em!, Wrench Sweep, Throwing hand, Bolt Stripper, the four Stun-conditional cards |
| **G2 — Values and action results** | Produce an amount from constants, history, current stacks, or a named previous action result. | Fixed damage, scaled damage, doubled stacks, and damage-derived Stun differ in their input calculation. | dramatic exit, Return to Sender, Stress Test!, Watch It Blow! |
| **G3 — State-changing operations** | Apply damage/status, remove stacks, heal, gain Energy, draw for an owner, fill Stun, or trigger status damage. | Small operations are reused directly and as conditional, triggered, or delayed payloads. | Bread, Bolt Stripper, Stress Test!, Cheap Opening, Full Tune-Up, plus most other cards |
| **G4 — Combat history** | Record resolved attacks and accepted card plays once, with source, target, kind, and time window. | “Was attacked”, “number of attacks”, and “first Attack card” are queries over shared history. | Throwing hand, Brace for It!, Return to Sender, dramatic exit, following up |
| **G5 — Triggered and scheduled effects** | Register a rule with an owner and lifetime; run a payload on a combat event or at an explicit future phase. | “Whenever”, “at turn end”, and “next turn” reuse event delivery, ownership, lifetime, and scheduling. | Got ‘em, I’ll get you back!, Brace for It!, Full Tune-Up, Gear overheat |
| **G6 — Card execution and repetition** | Resolve a card's effects from runtime context; replay a remembered card or repeat an effect without duplicating execution code. | Delayed card execution and replay share a resolver; repeated hits reuse its effect loop. | Gear overheat, following up; conditionally Beat down, This gear is for you!, Overcrank Strike |

**Composition rule:** timing → condition → value → operation → result. Target scope and effect ownership are explicit inputs throughout. Implement only the combinations required by the cards below.

## 2. Card logic breakdown

These recipes describe behavior, not existing C# APIs. `Sequence` means ordered actions; `Branch` evaluates once and executes only the selected branch. `Owner` means the hero whose card or rule is executing. A status stack condition and actual `IsStunned` are different checks.

Existing effects are included where necessary to show how the missing part fits. Preserve each card's cost, rarity, and `Disintegrate` setting from the GDD.

### Deck 1 — Attack: 6 cards

| Card | Composition / logic breakdown | Shared groups | Missing work or migration |
|---|---|---|---|
| **Got ‘em** | Register a combat-long rule: successful Vulnerable application, source = Owner, recipient = enemy → draw 1 for Owner. | G3, G5 | Replace the draft's dedicated draw hook with configurable event + filter + draw payload when G5 is implemented. Preserve confirmed ownership and duration. |
| **resonance** | Deal 12 to the selected enemy → if the specified enemy scope has Vulnerable, deal the same damage to all enemies. | G1, G2, G3 | Separate condition subjects from recipients. Clarify whether “same damage” means base 12 or resolved damage, and whether the condition checks the selected enemy or any enemy. |
| **I'm Tuff** | If any enemy has Vulnerable → apply 3 Attack Up to Owner once. | G1, G3 | Aggregate with `Any`; apply the reward once regardless of how many enemies match. |
| **Gear overheat** | Choose an Attack card → schedule its execution at player-turn end. | G5, G6 | Shared scheduling + card execution. Card source, selection, target, and resource policies remain unspecified. |
| **dramatic exit** | Read qualifying attacks performed by both heroes → calculate `6 + 5 × count` → deal damage. | G2, G3, G4 | Shared attack counter and value calculation. Counting window and qualifying attack kinds remain unspecified. |
| **following up** | Query the first Attack card played this turn → execute it through the shared resolver. | G4, G6 | Record accepted Attack-card plays; replay with explicit owner, targeting, payment, and history policies. Owner-only versus either hero's history is unspecified. |

### Deck 2 — Counter: 9 cards

| Card | Composition / logic breakdown | Shared groups | Missing work or migration |
|---|---|---|---|
| **Socket to ’Em!** | Deal 8 to the selected enemy → if that enemy is Aggravated, apply 4 Block to Owner. | G1, G3 | Inspect an enemy but award Block to Owner. Define whether the condition uses a pre-hit snapshot or live post-hit state. |
| **Throwing hand** | Query Owner's attacks received this turn → branch: count > 0 gives one hit of 12; otherwise one hit of 7. | G1, G3, G4 | Hero attack history + mutually exclusive damage selection. Do not deal 7 followed by 12. |
| **Wrench Sweep** | Deal 6 to all enemies → inspect enemies actually hit → if any qualifying hit enemy is Aggravated, apply 6 Block to Owner once. | G1, G2, G3 | Hit results + aggregate condition + independent recipient. Do not award 6 Block per matching enemy. |
| **Brace for It!** | Apply 12 Block to Owner → observe/query attacks received within the specified turn window → if at least one qualifies, draw 1 for Owner at the next player-turn start. | G1, G3, G4, G5 | History + one pending reward per activation + phase scheduler. Multiple qualifying hits must not produce multiple draws from one activation. |
| **I’ll get you back!** | Register a rule: Owner receives a qualifying attack → apply 1 Attack Up to Owner. | G3, G5 | Same trigger/payload mechanism as Got ‘em, with different event/filter/payload. Duration remains unspecified. |
| **Full Tune-Up** | Apply 14 Block to Owner and Aggravated to all enemies → for the specified turn, each qualifying incoming attack produces a counter strike of 10 against its attacker with an additional 15% of that target's maximum Stun gauge. | G3, G5 | Keep the counter path; add a scoped post-strike Stun payload to the relevant grant. Do not give unrelated attacks/counters an extra 15%. |
| **Bolt Stripper** | Deal 8 → if the target is Aggravated, remove `min(10, current Block)` and apply 1 Vulnerable to that enemy. | G1, G3 | Shared bounded status removal. Damage precedes removal of remaining Block. |
| **Bread** | Resolve the ally recipient → heal 10, clamped to missing HP. | G3 | Shared healing. Manual choice and self-target eligibility need a decision; existing manual targeting accepts enemies only. |
| **Return to Sender** | Read Owner's attacks received this combat → calculate `10 + 6 × count` → deal damage. | G2, G3, G4 | Reuse the history used by Throwing hand/Brace for It!, with a combat window and numeric output. |

### Deck 3 — Stun: 6 cards

| Card | Composition / logic breakdown | Shared groups | Missing work or migration |
|---|---|---|---|
| **Roll With It** | Branch on actual Stun: true → gain 2 Energy for Owner; false → apply 8 Block to Owner. | G1, G3 | `IsStunned` + gain Energy. Rewards are exclusive. “Any target enemy” needs a scope decision. |
| **Opportunist** | Deal 5 → if the target satisfies the actual-Stun condition, draw 2 for Owner. | G1, G3 | Reuse `IsStunned` and owner-aware drawing. Specify whether the check uses Stun before or after this hit. |
| **Cheap Opening** | Deal 3 → if the actual-Stun condition passes, execute both draw 1 and gain 1 Energy for Owner. | G1, G3 | One condition gates an ordered payload of two operations. Specify pre/post-hit timing. |
| **Pummel Down** | Branch on the target's actual Stun before the attack: deal one hit of 16 if true, otherwise one hit of 5. | G1, G3 | Reuse branch selection and damage; do not resolve both attacks. |
| **Watch It Blow!** | Apply 20 Stress → trigger all Stress with no consumption → capture that detonation's damage result → derive Stun from 40% of that result. | G2, G3 | Reuse configurable Stress detonation. Add a named result and explicit conversion to Stun units; do not substitute flat 40% maximum-gauge fill. |
| **Stress Test!** | Read current Stress → calculate `min(current × 2, 99)` → apply the required stack delta → draw 2 for Owner. | G2, G3 | Reuse a bounded stack transform; preserve status caps and UI updates. |

### Conditional repeat-attack dependencies: 3 cards

These were not explicitly identified as broken cards. `AutoTargetEffect.RepeatCount` exists, but `CardSystem` currently does not execute it; separate authored damage effects can already produce multiple hits.

| Card | Reusable composition | Groups / qualification |
|---|---|---|
| **Beat down** | Deal 8; if the enemy has Vulnerable, execute one additional hit of 8. | G1, G3, G6; inspect its asset before claiming it needs a repeat fix. |
| **This gear is for you!** | Deal 40; if the enemy has Vulnerable, execute one additional hit of 40. | G1, G3, G6; same dependency check. |
| **Overcrank Strike** | Two separate hits of 6, then the card's conditional Stress payload. | G1, G3, G6; only the repeat dependency is in this scope. Preserve the 10-Stress payload and clarify the “has 5 Stress” threshold separately if necessary. |

Repeating a hit is not multiplying damage: Block, Mark, counter reactions, death, and attack history resolve between hits. No Whiskey card in this scope requires a chance-based effect.

## 3. Similarities that should share code

| Shared pattern | Cards / variants | Implementation boundary |
|---|---|---|
| Enemy condition → owner reward | Socket to ’Em!, I'm Tuff, Wrench Sweep | One condition-scope evaluator plus independent recipient resolution. |
| Actual Stun → alternative or bonus | Roll With It, Opportunist, Cheap Opening, Pummel Down | One `IsStunned` predicate; branch or sequence determines the outcome. |
| Attack count → boolean or number | Throwing hand, Brace for It!, Return to Sender, dramatic exit | One history service; conditions compare its count, value sources scale it. |
| Event → effect payload | Got ‘em, I’ll get you back!, Full Tune-Up | One runtime rule mechanism with filters and existing operations. Counters retain their attack-kind behavior. |
| Future phase → effect payload | Brace for It!, Gear overheat | One scheduler; payload is draw or card execution. |
| Calculated amount → existing operation | dramatic exit, Return to Sender, Stress Test!, Watch It Blow! | Small typed value sources; no new damage/heal/status implementation for every formula. |
| Reuse a card's definition | following up, Gear overheat | One card-effect resolver; payment, targeting, zone movement, and replay provenance are explicit. |

## 4. Next implementation target

Start with **the reusable trigger/payload slice needed by Got ‘em**, preserving its confirmed behavior, rather than adding another dedicated trigger for I’ll get you back!. Then build the condition/recipient and primitive-operation slices in the [implementation plan](docs/card-effects/WHISKEY_IMPLEMENTATION_PLAN.md). Each step should complete a usable card behavior; do not build the whole proposed framework first.

Unresolved gameplay choices are recorded in that plan. They are not silently decided by this grouping. Whiskey's unique card **Back street’s greeting** remains outside the missing-feature scope.
