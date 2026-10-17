# Credits

Every pack used in the project.

| Pack | Author | URL | License / terms | Used for | Local note |
|---|---|---|---|---|---|
| Top Down Pixel Art Horror (school tiles: A4, A5, Inside_C/D/E, Outside) | kamisama887 (credit name: AnisAous) | https://kamisama887.itch.io/lorenz-fries-school-horror | Free for personal use on any engine. Commercial use requires a donation of 4.50 USD or more. Modifications and adaptations allowed; selling or separately re-uploading the pack is not allowed. Credit not required but appreciated. | Environment/School | Licenses/kamisama887_License.txt |
| Horror Pixel Art Assets | lenidev | https://lenidev.itch.io/horror-pixel-art-assets | Described as free to use; there is no formal license. **Confirm with the author before any commercial release.** | Environment/FutureSide, Characters/SideView | Licenses/lenidev_NOTE.txt |
| RPG Maker VX ACE: FREE 300 Modern Charas (MV pack) | adanoliva | https://adanoliva.itch.io/rpg-maker-free-300-modern-charas | Free for commercial and non-commercial use. | Characters/TopDown (Aiko, protagonist, bullies 1-3); the protagonist is also the side-view sprite (Characters/SideView/protagonist_side.png, the same frames) | Licenses/MV_300_Modern_Charas_NOTE.txt |
| Gloria Hallelujah (font) | Kimberly Geswein (kimberlygeswein.com) | https://fonts.google.com/specimen/Gloria+Hallelujah | SIL Open Font License 1.1 (free for any use, including commercial; may not be sold on its own). | Fonts/ (content-warning chalkboard text) | Fonts/GloriaHallelujah_OFL.txt |
| Pixel Operator Mono (font) | Jayvee Enaguas (HarvettFox96) | https://www.dafont.com/pixel-operator.font | CC0 1.0 (public domain dedication; no credit required). | Fonts/ (dialogue box and Shadow captions) | Assets/Fonts/PixelOperatorMono.ttf, Assets/Fonts/LICENSE.txt |
| Writing on Black Board 3 (sound effect, Pixabay ID 86723) | freesound_community (Pixabay; original artist ta8keiteasy on Freesound) | https://pixabay.com/sound-effects/film-special-effects-writing-on-black-board-3-86723/ | Pixabay Content License (free use, no attribution required). | Audio/SFX/chalk_writing.mp3 (content-warning screen) | Downloaded file name: freesound_community-writing-on-black-board-3-86723.mp3 |
| Scraping (sound effect, Pixabay ID 68271) | freesound_community (Pixabay; original artist takeabeat on Freesound) | https://pixabay.com/sound-effects/film-special-effects-scraping-68271/ | Pixabay Content License (free use, no attribution required). | Audio/SFX/eraser_scraping.mp3 (content-warning screen) | Downloaded file name: freesound_community-scraping-68271.mp3 |
| Track (pleasant) — Music/music_pleasant | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Present_Classroom, credits | |
| Track (corridor) — Music/music_corridor | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Future_Corridor, Act4_Encounter | |
| Track (diary) — Music/music_diary | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Future_Classroom2B | |
| Track (storage) — Music/music_storage | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Storage_Hallway | |
| Track (transition) — Music/music_transition | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | the Shadow's red/black flashing transitions | |
| Sound effect (shadow appears) — SFX/sfx_shadow_appear | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | the Shadow separating on Day 2 | |
| Sound effect (game over) — SFX/sfx_game_over | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | the game over screen | |
| Sound effect (dialogue blip source) — SFX/sfx_dialogue_source (one blip is cut out of it into sfx_blip.wav; sfx_blip_shadow.wav is made from that blip) | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Typing blips of every speaker and the Shadow | |
| Sound effect (door open) — SFX/sfx_door_open | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Every door that opens or changes scene | |
| Sound effect (door locked) — SFX/sfx_door_locked | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Every locked door | |
| Sound effect (menu select) — SFX/sfx_menu_select | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Title and pause menu buttons | |
| Sound effect (paper turn) — SFX/sfx_paper_turn | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Every diary page opening | |
| Sound effect (heartbeat) — SFX/sfx_heartbeat | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Day 1 freeze, the blood on her desk, the ghost encounter | |
| Sound effect (ghost breathing) — SFX/sfx_ghost_breathing | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | The monster and Aiko's ghost | |
| Ambience (rain on windows) — Ambience/amb_rain_light | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Day 1 and Back to Day 1 | |
| Ambience (heavy rain) — Ambience/amb_rain_heavy | Artist (Pixabay) | link to fill in | Pixabay Content License (confirm) | Day 2 | |

## Music (placeholders to fill in)

The tracks come from Pixabay; the rows above are placeholders ("Track — Artist (Pixabay)") until the real names and links are filled in. The Credits scene has the same placeholder lines in its Music section: update both. Loudness of every track was measured with `Tools/audio/measure.py` and corrected by the gain on its AudioCue asset (Data/Audio). Note: Shadow Appearing.wav carries C2PA (content credentials) metadata, so check how it was produced before release.

## Generated in this project

Drawn with Pillow by `Tools/gen_slice_c_art.py` (hard pixels, 48 px per world unit, same scale as the school art): the chalk hangman states, the diary page and the cracked mirror (Environment/School/chalk_hangman_*.png, diary_page.png, mirror_cracked.png). The reveal art (lower legs, cast shadow, fallen shoe, close-up still and darkness occluder in Art/Reveal) is drawn by Tools/reveal_art/*.py. Also generated: Characters/SideView/monster_eyes.png (the white eye pixels of the lenidev monster frames, drawn unlit), Environment/School/sign_2B.png and Placeholders/white.png. Extra named sprites were cut from the kamisama887 Inside_C/D/E sheets (lockers, windows, doors, and so on) by adding sprite rectangles to the existing sheets.

The content-warning chalkboard (Art/Warning/chalkboard_warning.png), the felt eraser, the chalk grain and the eraser smear are drawn with Pillow by `Tools/warning_art/gen_board.py` and `gen_props.py` (320x180 pixel art, shown with point filtering).
