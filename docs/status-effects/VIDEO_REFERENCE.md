# Status-effect video reference

## Source boundary

Only these three videos are the tutorial sources for this guide:

| Video | Topic |
| --- | --- |
| [Episode 22](https://youtu.be/2NLD2GdfIZU) | Status Effect UI |
| [Episode 23](https://youtu.be/heFmmQlDTFQ) | Status Effect System and Armor |
| [Episode 24](https://youtu.be/mLxcXp9JVRM) | Burn |

Evidence comes from the videos' English auto-generated captions and selected code frames. Captions can mis-transcribe identifiers; use the verified symbols below as tutorial names. No linked downloads, repositories, other episodes, comments, or project implementation were used as technical sources. This is an extension guide, not a copy of the tutorial's source code.

## Episode 22: presentation

- [0:18-1:36](https://www.youtube.com/watch?v=2NLD2GdfIZU&t=18s): `StatusEffectType` identifies effects, initially `ARMOR` and `BURN`. `StatusEffectUI.Set(Sprite sprite, int stackCount)` displays one icon and count. `StatusEffectsUI` manages a combatant's collection of those views.
- [1:54-4:01](https://www.youtube.com/watch?v=2NLD2GdfIZU&t=114s): `StatusEffectsUI` maps each type to one UI instance. `UpdateStatusEffectUI(type, stackCount)` removes and destroys an existing entry at zero; otherwise it creates a missing entry and sets its icon/count. `GetSpriteByType` supplies the sprite.
- [4:12-7:12](https://www.youtube.com/watch?v=2NLD2GdfIZU&t=252s): wire the prefab, image, TMP text, and sprites. A world-space canvas and horizontal layout group display the icons; child force expansion is disabled. The demonstrated 0.01 canvas scale assumes 100 pixels per unit, and 60-by-60 icons are example sizing, not mandatory values for new effects.

## Episode 23: shared application and Armor

- [0:15-1:42](https://www.youtube.com/watch?v=heFmmQlDTFQ&t=15s): `CombatantView` owns a type-to-stack-count dictionary. `AddStatusEffect` accumulates stacks. `RemoveStatusEffect` subtracts when present and removes entries at zero or below. `GetStatusEffectStacks` returns zero for an absent type. Mutations refresh the UI with the resulting count.
- [1:47-3:49](https://www.youtube.com/watch?v=heFmmQlDTFQ&t=107s): `AddStatusEffectGA` carries type, count, and combatant targets. `StatusEffectSystem` performs it by calling each target's shared add method. `AddStatusEffectEffect` converts configured card-effect data into that action.
- [3:56-5:46](https://www.youtube.com/watch?v=heFmmQlDTFQ&t=236s): `HeroTM : TargetMode` returns `HeroSystem.Instance.HeroView`, enabling the Armor Up card to give the hero five stacks. Scene and combatant UI references must be assigned.
- [5:55-7:13](https://www.youtube.com/watch?v=heFmmQlDTFQ&t=355s): Armor absorbs incoming damage first and loses the absorbed amount. Only positive remaining damage reaches the existing health-damage logic. This video does not establish automatic turn-based Armor expiration.

## Episode 24: Burn execution and timing

- [0:15-0:48](https://www.youtube.com/watch?v=mLxcXp9JVRM&t=15s): `ApplyBurnGA` carries damage and one target for an execution. Applying stacks still uses `AddStatusEffectGA`.
- [0:56-2:20](https://www.youtube.com/watch?v=mLxcXp9JVRM&t=56s): `BurnSystem` attaches/detaches its performer. Execution creates the target VFX, calls the target's damage method, removes one `BURN` stack, then waits one second for presentation.
- [2:24-2:50](https://www.youtube.com/watch?v=mLxcXp9JVRM&t=144s): `EnemySystem.EnemyTurnPerformer` checks each enemy's Burn count. Positive stacks supply `ApplyBurnGA`'s damage; its reaction is queued before that enemy's attack reaction.
- [2:51-3:46](https://www.youtube.com/watch?v=mLxcXp9JVRM&t=171s): enemy-turn reactions move from `CardSystem` to `HeroSystem`. `EnemyTurnPreReaction` discards cards in `ReactionTiming.PRE`. `EnemyTurnPostReaction`, subscribed in `POST`, queues hero Burn before drawing cards. Matching subscriptions/unsubscriptions appear in `OnEnable`/`OnDisable`.
- [3:48-4:58](https://www.youtube.com/watch?v=mLxcXp9JVRM&t=228s): Firestorm applies three Burn stacks to all enemies. Configure its card/deck and the scene's Burn system/VFX. Play Mode demonstrates damage, VFX, and one-stack decay.

## Limits and deductions

Armor absorbing Burn follows from Burn calling the ordinary damage method together with Episode 23's Armor handling; it is an implementation deduction. The videos do not establish a universal status duration, stack cap, immunity, cleansing, persistence, or behavior after a queued target dies. Specify relevant new behavior explicitly rather than attributing it to these sources.
