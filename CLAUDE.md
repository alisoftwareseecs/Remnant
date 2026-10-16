# Remnant - project brief for Claude Code

2D psychological horror game. Built module by module; implement only the module you are asked for.

## Environment
- Unity 6000.3.2f1 (Unity 6.3 LTS), Universal 2D template (URP 2D renderer)
- Windows, PowerShell, Git + Git LFS
- Project root: D:\Unity\Remnant
- Input: Unity Input System package (the template's InputSystem_Actions asset). Do not use the old Input Manager.

## Folder conventions
All game content lives under Assets/_Remnant/:
- Scripts/ (subfolders per module, e.g. Scripts/Player/, Scripts/Interaction/)
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

## Game design (fill in as decided)
- Setting: school
- Core themes: guilt, silence, something or someone missing
- Mechanics: time shifting between present and future; a shadow mechanic
- Perspective: TBD
