# Credits

Every pack used in the project.

| Pack | Author | URL | License / terms | Used for | Local note |
|---|---|---|---|---|---|
| Top Down Pixel Art Horror (school tiles: A4, A5, Inside_C/D/E, Outside) | kamisama887 (credit name: AnisAous) | https://kamisama887.itch.io/lorenz-fries-school-horror | Free for personal use on any engine. Commercial use requires a donation of 4.50 USD or more. Modifications and adaptations allowed; selling or separately re-uploading the pack is not allowed. Credit not required but appreciated. | Environment/School | Licenses/kamisama887_License.txt |
| Horror Pixel Art Assets | lenidev | https://lenidev.itch.io/horror-pixel-art-assets | Described as free to use; there is no formal license. **Confirm with the author before any commercial release.** | Environment/FutureSide, Characters/SideView | Licenses/lenidev_NOTE.txt |
| RPG Maker VX ACE: FREE 300 Modern Charas (MV pack) | adanoliva | https://adanoliva.itch.io/rpg-maker-free-300-modern-charas | Free for commercial and non-commercial use. | Characters/TopDown (Aiko, protagonist, bullies 1-3) | Licenses/MV_300_Modern_Charas_NOTE.txt |
| Gloria Hallelujah (font) | Kimberly Geswein (kimberlygeswein.com) | https://fonts.google.com/specimen/Gloria+Hallelujah | SIL Open Font License 1.1 (free for any use, including commercial; may not be sold on its own). | Fonts/ (content-warning chalkboard text) | Fonts/GloriaHallelujah_OFL.txt |
| Writing on Black Board 3 (sound effect, Pixabay ID 86723) | freesound_community (Pixabay; original artist ta8keiteasy on Freesound) | https://pixabay.com/sound-effects/film-special-effects-writing-on-black-board-3-86723/ | Pixabay Content License (free use, no attribution required). | Audio/SFX/chalk_writing.mp3 (content-warning screen) | Downloaded file name: freesound_community-writing-on-black-board-3-86723.mp3 |
| Scraping (sound effect, Pixabay ID 68271) | freesound_community (Pixabay; original artist takeabeat on Freesound) | https://pixabay.com/sound-effects/film-special-effects-scraping-68271/ | Pixabay Content License (free use, no attribution required). | Audio/SFX/eraser_scraping.mp3 (content-warning screen) | Downloaded file name: freesound_community-scraping-68271.mp3 |

## Generated in this project

Drawn with Pillow by `Tools/gen_slice_c_art.py` (hard pixels, 48 px per world unit, same scale as the school art): the chalk hangman states, the diary page and the cracked mirror (Environment/School/chalk_hangman_*.png, diary_page.png, mirror_cracked.png). The reveal art (lower legs, cast shadow, fallen shoe, close-up still and darkness occluder in Art/Reveal) is drawn by Tools/reveal_art/*.py. Also generated: Characters/SideView/monster_eyes.png (the white eye pixels of the lenidev monster frames, drawn unlit), Environment/School/sign_2B.png and Placeholders/white.png. Extra named sprites were cut from the kamisama887 Inside_C/D/E sheets (lockers, windows, doors, and so on) by adding sprite rectangles to the existing sheets.

The content-warning chalkboard (Art/Warning/chalkboard_warning.png), the felt eraser, the chalk grain and the eraser smear are drawn with Pillow by `Tools/warning_art/gen_board.py` and `gen_props.py` (320x180 pixel art, shown with point filtering).
