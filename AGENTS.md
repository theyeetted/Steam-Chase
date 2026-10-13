# Project instructions

## Unity Editor restrictions

These restrictions apply to all work in this project, not only status effects. Without the user's explicit permission for the action, do not:

1. Create or modify GameObjects, components, scenes, or prefabs.
2. Assign Inspector values or references.
3. Run C# scripts inside the Unity Editor, including scripts intended only for inspection.
4. Enter or exit Play Mode, or run checks in the Editor.

Do not perform these actions indirectly through the bridge, CLI, automation, or serialized scene/prefab edits. Bridge availability and general requests to implement a feature are not permission for these actions. Read-only inspection that does not execute C# or trigger checks is allowed. Editing source code and enum definitions remains allowed within the requested task. The status-effect boundary below additionally reserves all compilation and testing for the user.

## Status-effect instructions

For every request to add, change, or fix a status effect in this project:

**User implementation boundary:** Create or modify code and enum files only. The user exclusively makes Unity Editor changes and runs tests. Do not operate the Editor, enter Play Mode, run compilation or tests, change scenes/prefabs/card assets/Inspector references, or install Editor tooling for status-effect work unless the user explicitly changes this instruction. Describe required Editor setup and verification steps for the user to perform. Read-only inspection of relevant project files and review of the code diff are allowed.

1. Read [the implementation workflow](docs/status-effects/ADDING_STATUS_EFFECTS.md) before planning or editing.
2. Consult [the video reference](docs/status-effects/VIDEO_REFERENCE.md) for the demonstrated architecture and timing rules.
3. Use [the effect specification template](docs/status-effects/EFFECT_SPEC_TEMPLATE.md) to make the requested behavior explicit. It is a planning checklist; a separate filled-in file is optional.
4. Preserve the shared stack, action, and UI paths described in the workflow. Add effect-specific behavior at the appropriate trigger or damage integration point.
5. Review the code changes and hand off gameplay, presentation, and test checks to the user. Clearly report that those checks were not run by the agent.

These documents are based only on the three videos linked in the reference. No project implementation was read to author them. Tutorial symbols are reference names, not a claim that matching project files already exist. During a later implementation task, map them to the actual project only within that request's inspection permissions.

Do not introduce guidance from other tutorials, linked repositories, or articles unless the user expands the source scope. Distinguish behavior demonstrated in a video from a new gameplay decision or an implementation safeguard. Follow the user's current instructions when they override these defaults.
