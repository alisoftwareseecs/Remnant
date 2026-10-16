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
- InputActionReference fields must point to actions inside Assets/InputSystem_Actions.inputactions, never to scene-local objects. After creating or editing a prefab, verify its input references are still assigned.
- Code must compile without warnings. The human will confirm by checking the Unity Console.
- Batch mode (Unity.exe -batchmode) cannot run while the project is open in the Editor. Ask the human to close the Editor first.

## Game design
- Setting: a school. Few maps, reused heavily; the player backtracks through the same areas, which can behave differently depending on story progress.
- Perspective has two modes:
  - SideView: left/right only, walk + run, NO jump. Doors and stairs via interaction.
  - TopDown: 8-direction walk + run. Used for puzzles, key findings and chase sequences.
- Perspective shifts are FIXED story events in specific areas, triggered narratively by the Shadow. The player never controls them.
- Time has two timelines: Present and Future. Time shifts are also fixed story events.
- Default mapping: Present = TopDown. Future = SideView, except Future puzzle rooms, which are TopDown.
- Perspective and time are SEPARATE systems. Code must keep them independent; any combination (SideView/TopDown x Present/Future) must work. The default mapping above is content, set by story events, never hard-coded.
- The ending is decided by a key choice made in the Present (Act 4), recorded in the game state: one good ending, one bad ending.
- Themes: guilt, silence, loss. The story involves a student's suicide; handle it with care.
- The game opens with a content warning.
- Art style: mixed packs for now (see Cast). Keep visuals swappable.

### Story
- Protagonist (player): a male student, name TBD. Aiko: a classmate who is bullied and depressed.
- Act 1, Present Day 1 (TopDown): a normal school day. The player sees the bullies tormenting Aiko and does nothing.
- Act 2, Present Day 2: Aiko is missing. The protagonist's Shadow awakens and leads him to the Future.
- Act 3, Future: a dark, changed school. SideView corridors where an unknown monster chases the player; puzzle rooms switch to TopDown through Shadow-triggered shifts. Clues lead to discovering that Aiko has hanged herself (a brief still image: rope and feet in silhouette, no gore).
- Act 4, back to the Present: the player tries to stop the bullying and faces a key choice. The choice is made in the Present.
- Good ending: the monster was Aiko's ghost; she lives and the monster is gone.
- Bad ending: the Shadow abandons the player to the monster.

### Act 3 details (as built)
- Route: Present_Classroom (Day 2 desk, the Shadow wakes) -> Future_Corridor (SideView, monster) -> door "2-B" -> Future_Classroom2B (TopDown, the ruined classroom, no monster) -> back door (opens after the diary) -> Storage_Hallway (SideView, no monster) -> the storage room -> back to Present_Classroom (Act 4, flag `act4`).
- The storage room: the old storage room at the far end of the ground-floor hallway behind classroom 2-B. The protagonist walked past it every day. It is where Aiko was locked in when the bullies got bored (the only place no one ever looked), and where she hanged herself.
- Aiko's diary: six pages scattered around the Future 2-B classroom (flags `page_1` to `page_6`, "Pages: N/6" counter). They tell, in her own words, the hidden shoes, the forced smiles, the words scrubbed off her desk, "fine" as an answer at home, the boy who looked right at her and looked away (the protagonist), and the storage room. After the sixth page the Shadow says where to go (`diary_complete`) and the back door unlocks.
- Hangman on the chalkboard, drawn over three scenes: Day 1 only a head and four blank dashes; Day 2 head, body and arms; Future 2-B the full figure with the letters A I K O filled in ("The word was her name."). Each state is a chalk sprite shown by story flag.
- The cracked mirror (Future 2-B): first examine shows the protagonist's reflection and notes that his shadow is not in it. After all pages, a second examine has the Shadow say it was never part of him.
- The Shadow's true nature: it is the part of him that chose not to see. It is absent from mirrors, which foreshadows the bad ending where it abandons him. The Shadow must never be rendered in a reflection.
- The reveal (Storage_Hallway): only a tipped chair, a fallen shoe and her lower legs hanging from just above the knees (life size, one shoe on, one socked foot, toes down), with her cast shadow on the wall. Never her face or upper body, no gore, no rope or skirt on screen. An unlit black occluder drawn in front of the legs and the beam hides everything above the knees, so the flashlight can never reveal it. The beam has to touch her legs (same cone and line-of-sight test as the monster), then player movement and aim lock for about 4 s while the view cross-fades to a pixel-art close-up still (Art/Reveal/reveal_still.png, shown with point filtering) that stays up through the Shadow's lines. A slow fade then leads back to the Present with the title "Back." and the `act4` flag (the `day2` flag is cleared, so Aiko and the Day 1 bullies are present).
- Storage_Hallway has no monster, no running, and slower walking (set on its SceneSettings: Allow Running off, Walk Speed Multiplier 0.6).

### Mechanics
- Flashlight (Future, both perspectives): a URP 2D spot light aimed with the mouse or right stick (360 degrees in TopDown; in SideView the protagonist faces the side the beam points to). The Future is nearly dark, so the beam is the main light. Holding the beam on the monster for a short time banishes it (it fades out). It returns after a random delay, preferably from off-screen or behind the player.
- If the monster touches the player: game over, restart from the last checkpoint.
- The Shadow: the protagonist's own shadow, built in code from his current sprite (black silhouette, no eyes, unlit, slight distortion, delayed and sometimes out-of-sync movement). The monster keeps its white eyes and gets a faint cold tint, so the two never look alike.

### Cast (final; sources under Assets/_Remnant/Art/Characters/)
MV sheets (TopDown, 2.0x): 768x768, 4x2 characters, each 3 columns x 4 rows of 64x96 frames (rows: down, left, right, up). Sprite names: `<role>_<direction>_<0-2>`, e.g. `aiko_down_0`, `bully3_right_0`.

| Role | Source file | Position in sheet | Sprite prefix |
|---|---|---|---|
| Aiko | TopDown/Charas89-96b.png | character #4 (column 3, row 0; x 576-767, y 0-383) | `aiko_*` |
| Protagonist (TopDown) | TopDown/Charas105-112b.png | character #5 (column 0, row 1; x 0-191, y 384-767) | `protagonist_*` |
| Bully 1 | TopDown/Charas249-256b.png | character #3 (column 2, row 0; x 384-575, y 0-383) | `bully1_*` |
| Bully 2 | TopDown/Charas81-88b.png | character #2 (column 1, row 0; x 192-383, y 0-383) | `bully2_*` |
| Bully 3 | TopDown/Charas193-200b.png | character #6 (column 1, row 1; x 192-383, y 384-767) | `bully3_*` |
| Protagonist (SideView) | SideView/daniel_chettiar.png (lenidev) | 16x16 frames; row 0 = 4 frames (idle), row 1 = 8 frames (walk) | `protagonist_side_idle_0-3`, `protagonist_side_walk_0-7` |
| Monster (SideView) | SideView/monster_sighting-sheet.png (lenidev) | 32x32 frames, 12 in one row | `monster_0-11` |

## Architecture
- One central game state holds: current perspective, current timeline, story flags, recorded choices. Systems read it and react to its change events instead of polling.
- The player controller supports both perspective modes and switches when the game state changes.
- Each scene declares its perspective and time period with SceneSettings. Mid-scene shifts happen only through story events triggered by the Shadow. No player key ever changes perspective.

## Workflow
- Unity MCP (MCP for Unity v10.3.0) is connected on http://127.0.0.1:8081. Port 8080 is taken by another program (AgentService), so never switch back to 8080.
- Build and wire scenes directly with the Unity MCP tools: create GameObjects, add components, assign references, save the scene. Do not ask the human to do this by hand.
- Only if MCP is unavailable, fall back to an Editor setup script in Assets/_Remnant/Editor/ with a "Remnant/Setup/Module N - <name>" menu item (safe to run twice: reuse existing objects by name instead of duplicating them).
- Before any Editor change, check through MCP that Unity is not in Play mode. If it is, stop and ask the human to exit Play mode first.
- After writing code, wait for Unity to finish compiling, then read the Console through MCP and fix all errors and warnings yourself. Use %LOCALAPPDATA%\Unity\Editor\Editor.log only as a fallback.
- Runtime code never goes in an Editor folder; Editor code always does.
- The human's job is to press Play, test what you describe, and report how it feels.
- Before every commit, show git status. When the human confirms a module works, commit it as "Module N: <summary>".
