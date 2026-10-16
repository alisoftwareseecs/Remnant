# Remnant - project brief for Claude Code

2D psychological horror game. Built module by module; implement only the module you are asked for.

## Environment
- Unity 6000.3.2f1 (Unity 6.3 LTS), Universal 2D template (URP 2D renderer)
- Windows, PowerShell, Git + Git LFS
- Project root: D:\Unity\Remnant
- Input: Unity Input System package (the template's InputSystem_Actions asset). Do not use the old Input Manager.

## Folder conventions
All game content lives under Assets/_Remnant/:
- Scripts/ (subfolders per module, e.g. Scripts/Player/, Scripts/Story/)
- Scenes/, Prefabs/, Art/, Audio/, Data/ (ScriptableObjects)

C# namespace: Remnant (sub-namespaces per module, e.g. Remnant.Player).

## Rules
- Never edit or delete Library/, Temp/, Logs/, UserSettings/.
- Never create, edit or delete .meta files by hand. Unity generates them.
- Do not change ProjectSettings/ or Packages/manifest.json unless the task says so; if a package is needed, say which and why first.
- Prefer small, single-purpose MonoBehaviours and ScriptableObjects for data.
- Use [SerializeField] private fields, not public fields, for Inspector references.
- After each change, list exactly what the human must do in the Editor (create objects, assign references, layers/tags, scene setup), because you cannot click in the Editor.
- Code must compile without warnings. The human will confirm by checking the Unity Console.
- Batch mode (Unity.exe -batchmode) cannot run while the project is open in the Editor. Ask the human to close the Editor first.

## Game design
- Setting: a school. Few maps, reused heavily; the player backtracks through the same areas, which can behave differently depending on story progress.
- Perspective has two modes:
  - SideView: left/right only, walk + run, NO jump. Doors and stairs via interaction.
  - TopDown: 8-direction walk + run. Used for puzzles, key findings and chase sequences.
- Perspective shifts are FIXED story events in specific areas, triggered narratively by the Shadow (a companion entity that speaks to the protagonist). The player never controls them.
- Time has two timelines: Present and Future. Time shifts are also fixed story events.
- Perspective and time are SEPARATE systems. Code must keep them independent; any combination (SideView/TopDown x Present/Future) must work.
- Choices made in the Future are recorded and decide the ending: one good ending, one bad ending.
- Themes: guilt, silence, loss. The story involves a student's suicide; handle it with care.
- Art style: undecided. Keep visuals swappable; no pixel-perfect assumptions yet.

## Architecture
- One central game state holds: current perspective, current timeline, story flags, recorded choices. Systems read it and react to its change events instead of polling.
- The player controller supports both perspective modes and switches when the game state changes.

## Workflow
- Unity MCP (MCP for Unity v10.3.0) is connected on http://127.0.0.1:8081. Port 8080 is taken by another program (AgentService), so never switch back to 8080.
- Build and wire scenes directly with the Unity MCP tools: create GameObjects, add components, assign references, save the scene. Do not ask the human to do this by hand.
- Only if MCP is unavailable, fall back to an Editor setup script in Assets/_Remnant/Editor/ with a "Remnant/Setup/Module N - <name>" menu item (safe to run twice: reuse existing objects by name instead of duplicating them).
- After writing code, wait for Unity to finish compiling, then read the Console through MCP and fix all errors and warnings yourself. Use %LOCALAPPDATA%\Unity\Editor\Editor.log only as a fallback.
- Runtime code never goes in an Editor folder; Editor code always does.
- The human's job is to press Play, test what you describe, and report how it feels.
- Before every commit, show git status. When the human confirms a module works, commit it as "Module N: <summary>".
