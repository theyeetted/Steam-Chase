# Steam Chase — Developer Implementation Guide

Learn where to change the code, how cards execute, and how to add unit mechanics without bypassing the combat action system.

**Source snapshot:** 28 September 2026, commit `e28a533`, Unity `6000.3.24f1`. This guide describes the current source and saved assets. Examples labelled **proposed** are implementation instructions, not features already present in the game. This documentation change does not implement those examples.

## Contents

- [Start here](#start-here)
- [Card architecture](#card-architecture)
- [Implementing a stun bar for units](#implementing-a-stun-bar-for-units)
- [Implementing new effects for cards](#implementing-new-effects-for-cards)
- [Implementing new status effects](#implementing-new-status-effects)
- [Testing and common pitfalls](#testing-and-common-pitfalls)

## Start here

Open [Tutorial.unity](Assets/Scenes/Tutorial.unity) in Unity. It contains the connected combat systems, player, enemy slots, hand, targeting, and UI. The saved build configuration currently enables `SampleScene.unity`, so do not assume launching a build runs Tutorial.

Read these files first:

| File | What to learn |
| --- | --- |
| [MatchSetupSystem.cs](Assets/Script/System/MatchSetupSystem.cs) | Starts the match using a character, enemy definitions, and a perk. In Tutorial, its GameObject is named `MatchSystem`. |
| [CardData.cs](Assets/Script/Data/CardData.cs) and [Card.cs](Assets/Script/Models/Card.cs) | Saved card definitions versus individual runtime cards. |
| [CardSystem.cs](Assets/Script/System/CardSystem.cs) | Draw pile, hand, discard pile, card payment, and effect scheduling. |
| [ActionSystem.cs](Assets/Script/System/ActionSystem.cs) and [GameAction.cs](Assets/Script/Combat/GameAction.cs) | The shared execution pipeline used by cards, enemies, and reactions. |
| [CombatantView.cs](Assets/Script/Combat/View/CombatantView.cs) | Shared unit health, status stacks, and UI updates. Despite its name, it also owns runtime gameplay state. |

### Vocabulary

| Term | Meaning here |
| --- | --- |
| Unit | A `CombatantView`: either the player (`PlayerView`) or an enemy (`EnemyView`). |
| Data | A saved `ScriptableObject`, such as a `CardData` or `EnemyData` asset. |
| Effect | An authored instruction that creates a `GameAction`, such as “deal 6 damage.” |
| Target mode | Code that chooses the units affected by an automatic effect. |
| GA | **Game Action**: a request containing the information needed to execute something. |
| Performer | The coroutine registered to execute a particular action type. |
| Reaction | A child action queued while another action is running. |
| Status effect | State that stays on a unit, represented today by an enum value and an integer stack count. |

## Card architecture

### 1. What a saved card contains

This tree shows **composition**, not class inheritance. Effects and target modes are embedded managed references inside the card asset; they are not separate components to attach to a GameObject.

```text
CardData asset                         Assets/Data/CardInfos/*.asset
├── Asset name                         Used as the card title
├── description, Image, Cost            Display and energy cost
├── MannualTargetEffect                Optional single Effect
│   └── Effect                         Player selects one enemy
│       ├── DealDamageEffect
│       ├── AddStatusEffect
│       └── YourNewEffect
└── OtherTargetEffects                 List of AutoTargetEffect entries
    └── AutoTargetEffect               Each entry has BOTH parts below
        ├── TargetMode                 WHO receives this effect?
        │   ├── HeroTM                 The player
        │   ├── AllEnemies             Every enemy (AllEnemiesTM.cs)
        │   ├── RandomEnemyTM          One randomly selected enemy
        │   └── NoTM                   No unit targets; currently returns null
        └── Effect                     WHAT happens?
            ├── DealDamageEffect       Creates DealDamageGA
            ├── DrawCardsEffect        Creates DrawCardsGA; ignores targets
            ├── AddStatusEffect        Creates AddStatusEffectGA
            └── YourNewEffect          Creates a new or existing GameAction
```

`MannualTargetEffect` is the current spelling in the source. `Card.OtherEffects` exposes `CardData.OtherTargetEffects`. Use these names when following the code.

A card can have both a manual effect and automatic effects. When a manual effect is present, the player must select an enemy to play the card; its automatic effects then follow.

### 2. How data becomes a card in the hand

```text
MatchSetupSystem
└── CharData.Deck                       List of CardData asset references
    └── CardSystem.Setup(deck)
        └── new Card(cardData)          One runtime object per deck entry
            ├── Cost                   Copied into the runtime card
            └── Definition reference   Title, image, description, effects

DrawCardsGA → CardSystem
└── CardViewCreator                    Instantiates CardView.prefab
    └── CardView.Setup(card)            Displays the runtime card
        └── HandView                   Positions it along the hand spline
```

Runtime cards share the effect definitions stored in their `CardData`. Keep changing values such as “times used,” targets, or damage for this particular play in runtime state or the action, not inside the shared `Effect` object.

### 3. What happens when a card is played

```text
CardView                               Mouse input + energy check
├── ManualTargetSystem                 Select enemy, if required
└── ActionSystem.Perform(PlayCardGA)
    └── CardSystem.PlayCardPerformer
        ├── Remove card from hand and animate discard
        └── Queue reactions in this order
            ├── SpendEnergyGA          EnergySystem
            ├── PerformEffectGA        Manual effect, if present
            └── PerformEffectGA(s)     Automatic effects, in list order
                └── EffectSystem
                    └── Effect.GetGameAction(targets, caster)
                        └── Concrete GameAction
                            └── Registered system performer
                                └── Update unit/card/energy state and UI
```

For every action, [ActionSystem](Assets/Script/System/ActionSystem.cs) runs:

```text
PRE subscribers → PRE reactions
→ performer → reactions queued by the performer
→ POST subscribers → POST reactions
```

Each child action completes its own flow before the next child action. Automatic target lists are selected while `PlayCardPerformer` builds its reactions, before those effects execute. A later effect can therefore hold a target killed by an earlier effect; new performers should check for null, destroyed, or dead targets.

**Two rules to remember:** use `Perform(...)` to start an action from idle input; use `AddReaction(...)` inside an active performer or reaction callback. `Perform` drops requests while the system is busy. `AddReaction` is not a general-purpose idle queue.

The registry uses the exact action type and has one performer per type. A second registration replaces the first. Register in `OnEnable`, unregister in `OnDisable`, and add the component to the scene.

### Existing cards to inspect

| Asset | Actual configuration |
| --- | --- |
| [Swing](Assets/Data/CardInfos/Swing.asset) | Manual `DealDamageEffect`: 6 damage. |
| [Wide Swing](Assets/Data/CardInfos/Wide%20Swing.asset) | `AllEnemies` + `DealDamageEffect`: 10 damage. |
| [Draw](Assets/Data/CardInfos/Draw.asset) | `NoTM` + `DrawCardsEffect`: draw 2. |
| [Block](Assets/Data/CardInfos/Block.asset) | `HeroTM` + `AddStatusEffect`: 5 BLOCK. Its description incorrectly says “Apply 1 Attack UP.” |
| [StunGun](Assets/Data/CardInfos/StunGun.asset) | `AllEnemies` + `AddStatusEffect`: 3 SHOCK. It does not implement a stun bar or skip an action. |

## Implementing a stun bar for units

**Current state:** there is no stun meter or stun-based action skipping. The following is a **proposed implementation**, using the existing unit and action architecture.

### Files to work with

| Existing file | Change or responsibility |
| --- | --- |
| [CharData.cs](Assets/Script/Data/CharData.cs), [EnemyData.cs](Assets/Script/Data/EnemyData.cs) | Add an authored maximum stun value. |
| [CombatantView.cs](Assets/Script/Combat/View/CombatantView.cs) | Own the current meter, stunned state, accumulation, consumption, and reset. |
| [PlayerView.cs](Assets/Script/Combat/View/PlayerView.cs), [EnemyView.cs](Assets/Script/Combat/View/EnemyView.cs) | Pass the authored maximum into shared setup. |
| [EnemySystem.cs](Assets/Script/System/EnemySystem.cs) | Decide whether an enemy may attack when its queued opportunity executes. |
| [HeroSystem.cs](Assets/Script/System/HeroSystem.cs), [Interactions.cs](Assets/Script/System/Interactions.cs) | Define when player stun starts/ends and block card input when appropriate. |
| [CardSystem.cs](Assets/Script/System/CardSystem.cs) | Reject a prohibited card play before discarding it or spending energy. |
| [CombatantViewBase.prefab](Assets/Prefabs/CombatantViewBase.prefab) | Add the shared meter visual to the unit's world-space Canvas. |
| [Player variant](Assets/Prefabs/CombatantViewBase%20Variant.prefab), [enemy variant](Assets/Prefabs/EnemyVIew%20Variant.prefab) | Verify the bar and component references on both unit types. |
| [Tutorial.unity](Assets/Scenes/Tutorial.unity) | Add the new system component and verify scene overrides. |

Create these files as part of the feature; **they do not exist yet**:

```text
Assets/Script/
├── UI/StunBarUI.cs                     Draws current / maximum
├── Effects/AddStunEffect.cs            Authored amount → AddStunGA
├── Combat/GameAction/AddStunGA.cs      Amount, targets, optional caster
├── Combat/GameAction/TryAttackHeroGA.cs Checks an enemy's attack opportunity
└── System/StunSystem.cs                Performs AddStunGA
```

### Step 1 — Define the rule

For a first version, use this example rule: start at zero, accumulate stun until the maximum, hold a full bar while stunned, then skip one eligible action and reset to zero. Discard overflow, ignore additional buildup while stunned, and apply no passive decay. Use a separate stun amount on cards; ordinary damage, Block, and Shock do not change this meter unless explicitly designed to do so.

For an enemy, an eligible action is its next attack opportunity. For the player, define a stunned opportunity as one player phase in which cards cannot be played, while **End Turn remains available**. These are proposed design choices, not existing game rules.

### Step 2 — Separate configuration from runtime state

Add a positive `MaxStun` setting to both data classes. Set it explicitly on [Whiskey.asset](Assets/Data/CharData/Whiskey.asset), [TestEnemy.asset](Assets/Data/EnemyData/TestEnemy.asset), and [TestEnemyStronger.asset](Assets/Data/EnemyData/TestEnemyStronger.asset).

In `CombatantView`, add `MaxStun`, `CurrentStun`, and `IsStunned`, plus methods such as `AddStun(int amount)` and `ConsumeStun()`. Extend `SetupBase` and both derived `Setup` callers to initialize the maximum and reset runtime stun state. Clamp the maximum to at least 1 and reject non-positive additions.

Suggested logic, expressed as pseudocode:

```text
AddStun(amount):
    ignore dead units, non-positive amounts, and units already stunned
    increase CurrentStun by at most (MaxStun - CurrentStun)
    if CurrentStun reaches MaxStun: set IsStunned
    refresh bar

ConsumeStun():
    if not stunned: return false
    clear IsStunned and CurrentStun
    refresh bar
    return true                         The caller skips this opportunity
```

### Step 3 — Display the meter

Make `StunBarUI` accept current and maximum values. A non-interactive uGUI Slider or a filled Image fits the existing Canvas setup. For a filled Image, display `Clamp01(current / (float)maximum)` after validating the maximum. Refresh on setup, gain, and consumption; keep combat decisions out of the UI component.

Add the visual to the shared prefab and wire the reference in both variants. The player currently obtains its status UI reference through a Tutorial scene override, so also inspect the player instance in that scene; inheritance alone does not guarantee every reference is assigned.

### Step 4 — Route card stun through an action

Follow the [new card effect pattern](#implementing-new-effects-for-cards):

```text
CardData → AddStunEffect → AddStunGA → StunSystem → CombatantView.AddStun
```

The action carries the amount and targets. The performer checks each target is alive, calls `AddStun`, and handles any animation. Register/detach `AddStunGA` in the system's enable/disable methods. Attach `StunSystem` to an enabled object in Tutorial. Author a test card and add it to `Whiskey.Deck`.

### Step 5 — Make a full bar affect actions

Enemy turns currently queue Shock and then `AttackHeroGA` for every enemy before those actions execute. A check made only while constructing that queue can become stale.

Replace the directly queued attack with the proposed `TryAttackHeroGA`. Register its performer in `EnemySystem`, alongside the existing performers, and detach it on disable:

```text
EnemyTurnGA queues, for each enemy:
├── ApplyShockGA, if needed
└── TryAttackHeroGA                     Runs AFTER that tick completes
    ├── Dead / removed?                Stop
    ├── ConsumeStun() returns true?    Stop; this opportunity was skipped
    └── Otherwise                     AddReaction(new AttackHeroGA(enemy))
```

Do not simply return early inside `AttackHeroPerformer` for stun: its PRE/POST subscribers still run. [OnEnemyAttackCondition](Assets/Script/PerkConditions/OnEnemyAttackCondition.cs) listens to that action, so a skipped attack could incorrectly trigger a counterattack perk. Queue `AttackHeroGA` only for an accepted attack. For this design, stun applied after acceptance affects the next opportunity. Still guard the actual attack performer against an attacker destroyed by earlier reactions.

For player stun, add a proposed `BeginPlayerTurnGA` step after the player's Shock tick in `HeroSystem`'s enemy-turn POST reactions. At that step, consume pending stun into a `PlayerTurnBlocked` flag for the new phase, then draw the hand. Check that flag in `Interactions.PlayerCanInteract` and before `CardSystem` commits a play. On the next player-phase start, recompute the flag; keep End Turn usable and retain the normal energy refill. This separates a pending stun from the phase currently being skipped.

If you later make HP damage generate stun, inspect [DamageSystem](Assets/Script/System/DamageSystem.cs) and [ShockSystem](Assets/Script/System/ShockSystem.cs). `DealDamageGA.Amount` is requested damage, not HP lost after Block; `CombatantView.Damage` currently returns no applied-damage result.

### Stun acceptance checks

- Player and enemies start at zero with the correct maximum; setup resets both state and visuals.
- Partial, exact-threshold, and overflow gains behave as designed, independently for each target.
- A stunned enemy skips exactly one opportunity; the skip does not trigger attack perks.
- A stunned player cannot play cards and can still end the turn and eventually recover.
- Dead or destroyed units cannot attack or receive queued bar updates.
- Both prefab variants display the bar correctly with no missing Inspector references.

## Implementing new effects for cards

An `Effect` translates configuration into an action. It should not directly change health, instantiate UI, or start another top-level action.

### Files to work with

| File or folder | Responsibility |
| --- | --- |
| [Effect.cs](Assets/Script/Models/Effect.cs) | Contract: `GetGameAction(List<CombatantView> targets, CombatantView caster)`. |
| [Effects/](Assets/Script/Effects) | Concrete effect definitions and serialized parameters. |
| [GameAction/](Assets/Script/Combat/GameAction) | Execution requests such as `DealDamageGA`. |
| [EffectSystem.cs](Assets/Script/System/EffectSystem.cs) | Turns `PerformEffectGA` into the concrete action returned by the effect. |
| [AutoTargetEffect.cs](Assets/Script/Models/AutoTargetEffect.cs), [TargetMode.cs](Assets/Script/Models/TargetMode.cs), [TargetMode/](Assets/Script/TargetMode) | Automatic effect/target pairing and target selection. |
| [CardData.cs](Assets/Script/Data/CardData.cs), [CardInfos/](Assets/Data/CardInfos) | Card authoring and existing examples. |
| [CardView.cs](Assets/Script/Combat/View/CardView.cs), [ManualTargetSystem.cs](Assets/Script/System/ManualTargetSystem.cs) | Input and manual enemy selection. |
| [Whiskey.asset](Assets/Data/CharData/Whiskey.asset) | Current test deck; a new card must be referenced here to be drawn. |

**Choose the smallest change:** a new damage value needs only a card asset; a new status application can reuse `AddStatusEffect`. Add a new effect class when the conversion to an action changes. Add a new action and performer when execution itself is new.

### Worked example — Heal a unit

**Proposed example:** restore a positive amount of HP, capped at maximum health, without reviving dead units. The four code blocks below belong in the indicated files; these files/methods are not currently implemented.

**1. Create `Assets/Script/Effects/HealEffect.cs`.**

```csharp
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HealEffect : Effect
{
    [SerializeField, Min(1)] private int amount = 5;

    public override GameAction GetGameAction(
        List<CombatantView> targets, CombatantView caster)
    {
        return new HealGA(amount, targets);
    }
}
```

**2. Create `Assets/Script/Combat/GameAction/HealGA.cs`.** Copy the target list so this action owns its snapshot.

```csharp
using System.Collections.Generic;

public class HealGA : GameAction
{
    public int Amount { get; }
    public List<CombatantView> Targets { get; }

    public HealGA(int amount, List<CombatantView> targets)
    {
        Amount = amount;
        Targets = targets == null
            ? new List<CombatantView>()
            : new List<CombatantView>(targets);
    }
}
```

**3. Add this method inside `CombatantView`.** Its health properties have private setters, so the unit should own this update and refresh its own text.

```csharp
public void Heal(int amount)
{
    if (amount <= 0 || CurrentHealth <= 0) return;
    int missingHealth = Mathf.Max(0, MaxHealth - CurrentHealth);
    CurrentHealth += Mathf.Min(amount, missingHealth);
    UpdateHealthText();
}
```

**4. Create `Assets/Script/System/HealSystem.cs`.**

```csharp
using System.Collections;
using UnityEngine;

public class HealSystem : MonoBehaviour
{
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<HealGA>(HealPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<HealGA>();
    }

    private IEnumerator HealPerformer(HealGA action)
    {
        foreach (var target in action.Targets)
        {
            if (target == null || target.CurrentHealth <= 0) continue;
            target.Heal(action.Amount);
        }
        yield break;
    }
}
```

### Wire the effect in Unity

1. Allow the scripts to compile. Add one enabled `HealSystem` component to Tutorial.
2. Create a card through **Create → Data → CardData** in `Assets/Data/CardInfos`.
3. Set its asset name, description, image, and cost. Descriptions are display text; they do not implement behavior.
4. Leave `MannualTargetEffect` empty for a self-heal. Add an `AutoTargetEffect` entry to `OtherTargetEffects`, select `HeroTM`, and select `HealEffect` with the desired amount. The `[SerializeReference, SR]` fields provide the concrete type picker.
5. Add that asset to the `Deck` list on `Whiskey.asset`, which Tutorial's `MatchSetupSystem` references.
6. Enter Play Mode, take damage, then play the card. Verify health increases once, caps at maximum, updates visually, and spends the expected energy. Check the card leaves the hand and the console remains clear.

### Targeting and reuse details

- Manual targeting currently returns only `EnemyView`. Use `HeroTM` for player-targeted effects; selecting allies manually requires changes to `ManualTargetSystem`, `PlayCardGA`, and the input path.
- The class in `AllEnemiesTM.cs` is named `AllEnemies`. `SingleEnemyTM.cs` currently contains an empty `SingleEnemy : MonoBehaviour`; it is not a usable `TargetMode`.
- `NoTM` returns `null`; it works for `DrawCardsEffect` because that effect ignores targets. Do not use it for a target-dependent heal or damage effect. `RandomEnemyTM` also needs an empty-enemy guard before use when the board can be empty.
- Initialize `OtherTargetEffects` to an empty list for a manual-only card. Do not leave null wrapper, effect, or target-mode entries; the current card pipeline dereferences them directly.
- `EffectSystem` currently supplies the player as caster. If effects are reused for enemy casting, extend `PerformEffectGA` and its callers to carry the actual source.
- [Perk.cs](Assets/Script/Models/Perk.cs) also calls `Effect.GetGameAction`, so effects can be reused by perks. Add `IHaveCaster` to a new action only when its consumers need source information; the minimal healing example does not retain it.
- Preserve managed-reference class names/namespaces and asset `.meta` files. Renaming serialized effect types or moving their assemblies needs a migration plan; check existing card and perk assets afterward.

## Implementing new status effects

A card effect happens when the card resolves. A status persists on a unit and needs a rule that reads or consumes it. Adding an enum value and icon alone does not give it gameplay behavior.

### Files to work with

| File | Responsibility |
| --- | --- |
| [StatusEffectType.cs](Assets/Script/Enums/StatusEffectType.cs) | Stable serialized identifiers for statuses. |
| [AddStatusEffect.cs](Assets/Script/Effects/AddStatusEffect.cs) | Existing card effect for applying any status type. |
| [AddStatusEffectGA.cs](Assets/Script/Combat/GameAction/AddStatusEffectGA.cs), [StatusEffectSystem.cs](Assets/Script/System/StatusEffectSystem.cs) | Carry and execute status application. |
| [CombatantView.cs](Assets/Script/Combat/View/CombatantView.cs) | Stores stack counts; provides `AddStatusEffect`, `RemoveStatusEffect`, and `GetStatusEffectStacks`. |
| [StatusEffectsUI.cs](Assets/Script/UI/StatusEffectsUI.cs) | Maps a type to a sprite and creates/removes widgets. |
| [StatusEffectUI.cs](Assets/Script/UI/StatusEffectUI.cs), [statusEffectUI.prefab](Assets/Prefabs/statusEffectUI.prefab) | Displays one icon and stack count. |
| [ApplyShockGA.cs](Assets/Script/Combat/GameAction/ApplyShockGA.cs), [ShockSystem.cs](Assets/Script/System/ShockSystem.cs) | Existing tick-action example; see its death-handling limitation below. |
| [EnemySystem.cs](Assets/Script/System/EnemySystem.cs), [HeroSystem.cs](Assets/Script/System/HeroSystem.cs) | Choose when existing Shock ticks occur. |

### Existing statuses

| Status | Current behavior |
| --- | --- |
| `VULNERABLE` (`0`) | Stored and displayed; no damage modifier is implemented. |
| `SHOCK` (`1`) | Deals damage equal to the stacks captured when its tick is queued, then removes one stack. Enemy ticks are queued before attacks; the player tick is queued after the enemy turn. |
| `BLOCK` (`2`) | Absorbs incoming damage in `CombatantView.Damage` and spends the absorbed stacks. No automatic turn expiry. |
| `ATTACKUP` (`3`) | Stored and displayed; no attack modifier is implemented. |

Applications add stacks together. Removing enough stacks deletes the status and its icon. There is no general duration system, source tracking, or automatic status reset in `SetupBase`.

```text
Card's AddStatusEffect
└── AddStatusEffectGA
    └── StatusEffectSystem
        └── CombatantView.AddStatusEffect
            ├── Runtime dictionary: type → stack count
            ├── StatusEffectsUI → StatusEffectUI prefab
            └── Gameplay consumer
                ├── Damage-time rule, such as BLOCK
                ├── Action subscriber, such as the proposed WEAK below
                └── Timed action, such as ApplyShockGA → ShockSystem
```

### Worked example — WEAK

**Proposed rule:** each stack represents one weakened outgoing damage action. While the caster has WEAK, reduce that action's damage by 2, to a minimum of zero, then consume one stack. A multi-target action spends one stack and reduces damage to all its targets. There is no turn expiry in this example.

**1. Append the enum value.** Preserve the current numeric IDs because existing assets store these numbers. If another feature has already added a value, choose the next unused value.

```csharp
public enum StatusEffectType
{
    VULNERABLE = 0,
    SHOCK = 1,
    BLOCK = 2,
    ATTACKUP = 3,
    WEAK = 4
}
```

**2. Add the icon mapping.** In `StatusEffectsUI`, add a serialized `Sprite weakSprite` field and the switch arm `StatusEffectType.WEAK => weakSprite`. Assign that sprite on the shared unit prefab and verify both variants and Tutorial's player instance. Continue using the existing `statusEffectUI.prefab`.

**3. Create `Assets/Script/System/WeakSystem.cs`.** This behavior modifies an existing action, so it uses a PRE subscription instead of replacing `DamageSystem`'s performer.

```csharp
using UnityEngine;

public class WeakSystem : MonoBehaviour
{
    private void OnEnable()
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(
            BeforeDamage, ReactionTiming.PRE);
    }

    private void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<DealDamageGA>(
            BeforeDamage, ReactionTiming.PRE);
    }

    private void BeforeDamage(DealDamageGA action)
    {
        var caster = action.Caster;
        if (caster == null || caster.CurrentHealth <= 0) return;
        if (caster.GetStatusEffectStacks(StatusEffectType.WEAK) <= 0) return;

        action.Amount = Mathf.Max(0, action.Amount - 2);
        caster.RemoveStatusEffect(StatusEffectType.WEAK, 1);
    }
}
```

This example spends a stack for every `DealDamageGA` emitted by a living weakened caster, even if its amount is zero or its targets are no longer valid. If the intended rule is “consume only on a successful hit,” add an explicit damage-result mechanism before implementing that variation. Multiple damage modifiers also need a deliberate ordering rule.

**4. Configure and try it.** Add one `WeakSystem` to Tutorial. Make a card using the existing `AddStatusEffect`, select `WEAK`, and set a positive stack count. Use a manual effect for one enemy or `AllEnemies` for all enemies. Add the card to `Whiskey.Deck`.

Give an enemy with attack power 5 two stacks: its next two ordinary damage actions should deal 3 before Block, leaving 1 and then 0 stacks; the next should deal 5. Verify the icon disappears at zero. Also test player-sourced damage, multi-target cards, and system disable/re-enable.

### Adding a status that ticks each turn

For poison, regeneration, or another timed mechanic, add a dedicated action and performer when needed, then queue it from the appropriate turn hook. The current order is:

```text
EnemyTurnGA
├── PRE: HeroSystem queues discard-all-cards
├── Performer: EnemySystem queues, per enemy
│   ├── Shock tick, if present
│   └── AttackHeroGA → DealDamageGA
└── POST
    ├── HeroSystem queues player Shock tick, then draw 5
    └── EnergySystem queues energy refill
```

The relative order of the HeroSystem and EnergySystem POST callbacks depends on subscription order. Put operations with strict ordering requirements in one explicit sequence.

For a damaging status, use or extend the normal damage action path and decide how Block, outgoing modifiers, and kill handling should apply. Current `ShockSystem` calls `CombatantView.Damage` directly: Block absorbs it, but `DealDamageGA` subscribers and `DamageSystem`'s death handling are bypassed. A lethally shocked enemy can remain on the board and execute its queued attack. Do not copy that behavior as a complete damage-over-time implementation.

The current application action and stack dictionary do not remember who applied a status. If the mechanic needs original-caster modifiers, kill credit, or separate durations per application, extend the stored status data and application action explicitly.

## Testing and common pitfalls

Use Tutorial for manual checks. This guide was verified against source and serialized asset references; its proposed examples have not been installed, compiled, or run in Play Mode. [Tests/CombatManagerChecks.cs](Tests/CombatManagerChecks.cs) targets an older `CombatManager`/`Battle` API and does not validate the current action system.

### Before considering a feature complete

- Confirm scripts compile and all new systems are enabled in the gameplay scene.
- Check card payment, targeting, effect order, hand removal, and discard recycling.
- Check zero/full values, stacking, multiple targets, dead targets, and both unit types.
- Confirm status/bar UI updates on application, consumption, expiry, and reset.
- Disable/re-enable a system and restart Play Mode to check registration cleanup.
- Commit scripts, assets, prefab/scene wiring, and Unity-generated `.meta` files together.

### Existing issues that can affect your tests

| Observation | Why it matters |
| --- | --- |
| `PlayCardPerformer` adds the played card to `discardPile`, then `DiscardCard` adds it again. | A played card can be duplicated during recycling; distinguish this from a new effect firing twice. |
| `MatchSetupSystem` starts draw and immediately calls `Perform` for energy refill. | The refill request is dropped while drawing is busy. Energy starts at 3 independently; future startup actions need explicit sequencing. |
| `SetupBase` does not clear statuses, and `CardSystem.Setup` appends to existing piles. | Calling setup again is not currently a complete fresh-match reset. |
| Shock bypasses normal death handling; queued attacks do not check for dead attackers. | New damage/status mechanics must account for lethal effects and queued targets. |
| `DamageSystem` leaves player death as a comment. | There is no complete game-over flow to rely on yet. |
| BLOCK/SHOCK work, but VULNERABLE/ATTACKUP lack gameplay rules. | An icon or enum entry is not evidence that a mechanic is implemented. |

Keep gameplay edits in `Assets/Script`, author content in `Assets/Data`, and wire visuals in prefabs/scenes. Generated folders such as `Library`, `obj`, and `Logs` are not implementation locations. The older [project file guide](PROJECT_FILE_GUIDE.md) is a historical snapshot; use the paths and behavior in this guide for these extension tasks.
