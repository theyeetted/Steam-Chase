# GameManager Guide

## Status and scope

The user authorized code implementation on 2026-10-19. The game-flow code is implemented in the existing agent worktree on `codex/game-flow`, based on `MergeforDemo` at `1ed85f7`.

`GameManager.cs`, its supporting systems, and the saved `Tutorial.unity` Canvas under `-----UI-----` were inspected. The latest scene was read from the user's main checkout because its UI changes are uncommitted there. No live Editor inspection or scene/Inspector changes were performed. The user subsequently requested applying the code, guide and verification files to `D:/universityProject/Game Logic/Steam Chase`; existing scene and asset edits must be preserved.

The saved hierarchy contains `HUD`, `GameOverUI`, `WinMenu`, `StartMenu`, `DeckChooseMenu`, `DifficultiesMenu`, and `PauseMenu`. GameManager resolves these names under `-----UI-----/Canvas` at runtime when its serialized references are empty. Existing references take precedence.

## Main flow

`StartMenu -> DifficultiesMenu -> DeckChooseMenu -> Combat`

For this guide, **MainMenu** and **StartMenu** refer to the same initial menu.

### StartMenu

| Button | Behavior |
| --- | --- |
| Start | Open `DifficultiesMenu`. |
| Exit | Exit the game |

### DifficultiesMenu

| Button | Behavior |
| --- | --- |
| Back | Return to `StartMenu` / MainMenu. |
| Normal | Select and load the Normal difficulty's enemy setup, then open `DeckChooseMenu`. |
| Elite | Select and load the Elite difficulty's enemy setup, then open `DeckChooseMenu`. |

Combat begins only after deck confirmation.

### DeckChooseMenu

| Button | Behavior |
| --- | --- |
| Back | Return to `DifficultiesMenu`. |
| Whiskey Deck 1, 2, or 3 | Load Whiskey's selected deck. |
| Ade Deck 1, 2, or 3 | Load Ade's selected deck. |
| Confirm | Load combat and start the fight using the selected difficulty and deck selection. |

The project uses two heroes with independent decks. Select one deck for Whiskey (hero 0) and one for Ade (hero 1). Button arguments are 1, 2, or 3; zero means unselected. Confirm remains disabled until both selected decks and the encounter are configured. Unconfigured deck choices are disabled.

## Combat and HUD

- Display the `HUD` during active combat.
- The HUD Pause button pauses combat and displays `PauseMenu`.
- Pressing Escape also pauses combat and displays `PauseMenu`.
- Pressing Escape while paused resumes combat. Both the new and legacy input backends are supported.
- The HUD EndTurn button ends the player's turn using the existing implementation. **Do not modify EndTurn behavior or its implementation.**

## PauseMenu

Display `PauseMenu` while the game is paused.

| Button | Behavior |
| --- | --- |
| Resume | Resume the current combat and return to the HUD. |
| Restart | Restart combat with the current difficulty and deck selection, without asking the player to choose decks again. |
| ChooseDeck / Choose Deck | Reset the fight and return to `DeckChooseMenu`, keeping the selected difficulty. Start the new fight after confirmation. |
| Mainmenu / MainMenu | Return to `StartMenu` / MainMenu. |
| Exit | Exit the game. |

## Defeat and victory

- When both heroes are defeated, stop combat and display `GameOverUI` / GameoverMenu. This preserves the existing combat rule that one surviving hero can continue fighting.
- When all enemies are eliminated, stop combat and display the victory menu.
- Both result menus must have the same button layout and functionality.

The victory menu is named `WinMenu` in the saved scene. Both result menus already contain Restart, Choose Deck, Mainmenu, and Exit buttons (including prefab-inherited buttons).

| Button | Behavior in either result menu |
| --- | --- |
| Restart | Restart combat with the current difficulty and deck selection. |
| Choose Deck | Reset the fight and return to `DeckChooseMenu`, keeping the selected difficulty. Start the new fight after confirmation. |
| Mainmenu / MainMenu | Return to `StartMenu` / MainMenu. |
| Exit | Exit the game. |

## Implementation behavior

- Inspect the current combat lifecycle, difficulty setup, deck loading, UI hierarchy, and button listeners before changing them.
- Manage menu visibility and the HUD consistently with the current flow state.
- Ensure pausing and combat results stop gameplay, and that resuming or starting a fresh fight restores normal execution.
- Restart must reset combat state while retaining the chosen difficulty and decks.
- Choosing decks again must reset combat and allow a new selection before the next fight starts.
- Returning to MainMenu must leave combat and display the initial menu.
- Verify all requested buttons exist and are wired, including Back, Confirm, and MainMenu buttons that are not clearly visible in the screenshots.
- Preserve the existing EndTurn implementation.

Restart reloads the current scene and reapplies the selected difficulty and both deck numbers. Choose Deck reloads into deck selection, retaining the difficulty and previous choices so the player can change either deck and confirm. MainMenu reloads into StartMenu and clears the selections. Reloading resets heroes, enemies, hands, energy, turns, perks, and pending effects through their normal scene initialization.

Pause freezes the game clock and suspends nested action enumerators, including routines that yield `null`. Resume continues the pending action. Pausing also cancels card dragging/targeting. Gameplay view roots remain active while paused so their coroutines are preserved; the HUD is hidden and PauseMenu is shown.

Runtime button binding replaces legacy GameManager listeners and `MatchSetupSystem.RestartFight` listeners only on flow buttons. This corrects the saved Resume-to-RestartFight hookup without changing scene serialization. EndTurn's code and Inspector wiring are untouched. The old unguarded W/L instant-win/lose shortcuts were removed from GameManager.

## Required Unity setup

These are manual setup steps, not changes already made by the agent. `AGENTS.md` reserves scene/component changes, Inspector assignments and Editor checks for explicit user permission.

1. Apply the code changes to the current project before configuring its existing `Tutorial` scene. The agent worktree contains the committed scene, while the user's main checkout has newer unsaved-to-Git UI changes; do not overwrite that scene with the worktree copy.
2. On `MatchSetupSystem`, assign `Whiskey Decks` and `Ade Decks` with three `CharData` assets each, in Deck 1, 2, 3 order. Each asset must have positive Health and a nonempty Deck without missing cards. An unassigned Deck 1 slot falls back to `First Char Data` / `Second Char Data`; other unassigned slots are unavailable.
3. Assign `Normal Enemy Datas` and `Elite Enemy Datas`. Empty Normal falls back to the existing `Enemy Datas` list. Elite requires its own nonempty list; the implementation does not invent enemy balance or substitute Normal enemies.
4. Keep `GameManager`, `MatchSetupSystem`, combat systems and EventSystem active. Assign menu references manually if their names or locations differ from `-----UI-----/Canvas`. Optional `In Game Objects` should contain only gameplay view roots, never managers, menus or EventSystem.
5. Add the missing `DifficultiesMenu/Back`, `DeckChooseMenu/Back`, and `DeckChooseMenu/Confirm` buttons. They were absent from the inspected saved hierarchy. `BackButton` and `ConfirmButton` are also accepted names. `DifficultiesMenu/Pause_0` is not treated as Back because it has no Button component.
6. Keep the existing result-menu buttons in the same layout and ensure the `WinMenu` label communicates victory. Both result menus use the same callbacks.
7. Save the scene and include it in the build scene list so scene reload works in a player build.

### Automatic button paths and manual callback alternatives

Paths are relative to `-----UI-----/Canvas`. If you use these paths, GameManager adds the callbacks at runtime; extra Inspector listeners are unnecessary.

| Path | GameManager callback |
| --- | --- |
| `StartMenu/StartButton` | `StartGame()` |
| `StartMenu/ExitButton` | `ExitGame()` |
| `DifficultiesMenu/Back` | `BackToMainMenu()` |
| `DifficultiesMenu/Normal` | `SelectNormalDifficulty()` |
| `DifficultiesMenu/Elite` | `SelectEliteDifficulty()` |
| `DeckChooseMenu/Back` | `BackToDifficultiesMenu()` |
| `DeckChooseMenu/Whiskey/Deck1` through `Deck3` | `SelectWhiskeyDeck(1)` through `(3)` |
| `DeckChooseMenu/Ade/Deck1` through `Deck3` | `SelectAdeDeck(1)` through `(3)` |
| `DeckChooseMenu/Confirm` | `ConfirmDecks()` |
| `HUD/Pause` | `PauseGame()` |
| `PauseMenu/Resume` | `ResumeGame()` |
| `PauseMenu`, `WinMenu`, `GameOverUI`: `Restart` | `RestartGame()` |
| Same three menus: `Choose Deck` | `ChooseDeck()` |
| Same three menus: `Mainmenu` | `MainMenu()` |
| Same three menus: `Exit` | `ExitGame()` |

## Verification

Run `Tests/VerifyGameFlowCompilation.ps1 -ReferenceProject '<existing Unity project path>'` outside the Editor after that reference project has generated Unity compilation references. It compiles this worktree's gameplay source for the Editor/new-input and player/legacy-input branches, then checks the production pause enumerator without starting Unity.

Completed: both source configurations compiled successfully; eight pause/resume/cancellation checks passed; source diff confirms `EndTurnButtonUI.cs` and `TurnSystem.cs` are unchanged. This is not a Unity player build or Play Mode test.

Manual Play Mode checks still required after setup:

- Start and all Back buttons navigate correctly; no combat starts before Confirm.
- Try each difficulty and all three deck choices for both heroes. Missing assignments must not start a fight.
- Pause with both the button and Escape, during a card drag, opening draws, and an enemy attack. Health, cards, energy, and queued actions remain frozen; Resume continues the same fight.
- Restart from pause, victory and defeat restores a fresh fight with the same difficulty and decks.
- Choose Deck from all three menus retains difficulty, permits new selections, and waits for Confirm.
- MainMenu returns to a fresh initial flow. Exit stops Play Mode in the Editor and quits a build.
- One defeated hero does not end the fight; both defeated show GameOverUI. Defeating every enemy shows WinMenu. Results stop combat.
- EndTurn continues to behave as before.
