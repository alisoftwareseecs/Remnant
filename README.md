# REMNANT

A 2D psychological horror game about guilt, silence and being unseen. You are a student who watched a classmate, Aiko, get bullied and did nothing. When she disappears, your own shadow wakes up and leads you into a dark, changed version of your school. Something is hunting you in the corridors, a classroom holds her diary, and at the end of the hallway is what happens when everyone looks away. Then you get to go back and decide whether you look this time.

Made with Unity by **Squad Zero**. One ending. Windows.

## Content warning

Remnant deals with **bullying, child abuse, parental death, depression and suicide.** Abuse and death are only ever mentioned in words, and the suicide is implied, not shown graphically. The game still deals with heavy themes and may be upsetting.

If you are struggling, you are not alone. Please reach out to someone you trust, or find a helpline near you at **[findahelpline.com](https://findahelpline.com)**.

## How to play

1. Go to the [Releases page](../../releases) and download the latest release zip (`Remnant-v1.0-Windows.zip`).
2. Extract the whole zip.
3. Run `Remnant.exe`.

## Controls

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD / arrow keys | Left stick |
| Run | Shift | Left stick press |
| Interact / continue dialogue | E | North button (Y / Triangle) |
| Aim the flashlight | Mouse | Right stick |
| Lower the light (late in the game) | F | East button |
| Pause menu | Esc | Start |

## Opening the project

- Unity **6000.3.2f1** (Unity 6.3 LTS), Universal 2D (URP) template.
- **Git LFS is required** for the art and audio. Run `git lfs install` **before** cloning:

```
git lfs install
git clone https://github.com/alisoftwareseecs/Remnant.git
```

Then open the folder with Unity Hub and load `Assets/_Remnant/Scenes/ContentWarning.unity` (the first scene of the game). The project uses the Input System package only. Game content lives in `Assets/_Remnant/`; see `CLAUDE.md` for the design and architecture notes.

Note: "Download ZIP" on GitHub includes the real LFS files only because the repository has *Include Git LFS objects in archives* enabled.

## Credits

**Game by Squad Zero.** Made with Unity.

Art, font and sound packs used (full list in [`Assets/_Remnant/Art/CREDITS.md`](Assets/_Remnant/Art/CREDITS.md)):

| Pack | Author | URL | License / terms |
|---|---|---|---|
| Top Down Pixel Art Horror (school tiles) | kamisama887 (credit name: AnisAous) | https://kamisama887.itch.io/lorenz-fries-school-horror | Free for personal use on any engine. Commercial use requires a donation of 4.50 USD or more. Modifications and adaptations allowed; selling or separately re-uploading the pack is not allowed. Credit not required but appreciated. |
| Horror Pixel Art Assets | lenidev | https://lenidev.itch.io/horror-pixel-art-assets | Described as free to use; there is no formal license. |
| RPG Maker VX ACE: FREE 300 Modern Charas (MV pack) | adanoliva | https://adanoliva.itch.io/rpg-maker-free-300-modern-charas | Free for commercial and non-commercial use. |
| Gloria Hallelujah (font) | Kimberly Geswein | https://fonts.google.com/specimen/Gloria+Hallelujah | SIL Open Font License 1.1. |
| Pixel Operator Mono (font) | Jayvee Enaguas (HarvettFox96) | https://www.dafont.com/pixel-operator.font | CC0 1.0. |
| Writing on Black Board 3 (sound effect, ID 86723) | freesound_community (Pixabay) | https://pixabay.com/sound-effects/film-special-effects-writing-on-black-board-3-86723/ | Pixabay Content License. |
| Scraping (sound effect, ID 68271) | freesound_community (Pixabay) | https://pixabay.com/sound-effects/film-special-effects-scraping-68271/ | Pixabay Content License. |

Also generated in this project: the chalk hangman states, diary page, cracked mirror, the reveal art and the content-warning chalkboard, drawn with Python/Pillow scripts in `Tools/`.

The third-party art keeps its own license; it is not covered by any license of this repository's code.
