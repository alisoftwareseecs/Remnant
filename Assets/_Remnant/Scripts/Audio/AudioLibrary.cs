using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// The cues that code (not just scene components) needs to play by name. Lives on the AudioSystem prefab.
    /// Slots whose cue has no clip yet are silent and null-safe; drop the file into the cue asset later.
    /// </summary>
    [CreateAssetMenu(menuName = "Remnant/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Music")]
        [SerializeField] private AudioCue pleasant;
        [SerializeField] private AudioCue corridor;
        [SerializeField] private AudioCue diary;
        [SerializeField] private AudioCue storage;
        [SerializeField] private AudioCue transition;

        [Header("Sound effects")]
        [SerializeField] private AudioCue shadowAppear;
        [SerializeField] private AudioCue gameOver;
        [SerializeField] private AudioCue doorOpen;
        [SerializeField] private AudioCue doorLocked;
        [SerializeField] private AudioCue menuSelect;
        [SerializeField] private AudioCue paperTurn;

        [Header("Loops")]
        [SerializeField] private AudioCue heartbeat;
        [SerializeField] private AudioCue ghostBreathing;
        [SerializeField] private AudioCue rainLight;
        [SerializeField] private AudioCue rainHeavy;
        [SerializeField] private AudioCue pleasantMemory;

        [Header("Empty slots (silent until a file is dropped into the cue)")]
        [SerializeField] private AudioCue footstepClassroom;
        [SerializeField] private AudioCue footstepCorridor;
        [SerializeField] private AudioCue lightLower;
        [SerializeField] private AudioCue lightsFlicker;
        [SerializeField] private AudioCue lightsDie;
        [SerializeField] private AudioCue lightsReturn;
        [SerializeField] private AudioCue girlsLaugh;
        [SerializeField] private AudioCue ghostFlinch;

        public AudioCue Pleasant => pleasant;
        public AudioCue Corridor => corridor;
        public AudioCue Diary => diary;
        public AudioCue Storage => storage;
        public AudioCue Transition => transition;
        public AudioCue ShadowAppear => shadowAppear;
        public AudioCue GameOver => gameOver;
        public AudioCue DoorOpen => doorOpen;
        public AudioCue DoorLocked => doorLocked;
        public AudioCue MenuSelect => menuSelect;
        public AudioCue PaperTurn => paperTurn;
        public AudioCue Heartbeat => heartbeat;
        public AudioCue GhostBreathing => ghostBreathing;
        public AudioCue RainLight => rainLight;
        public AudioCue RainHeavy => rainHeavy;
        public AudioCue PleasantMemory => pleasantMemory;
        public AudioCue FootstepClassroom => footstepClassroom;
        public AudioCue FootstepCorridor => footstepCorridor;
        public AudioCue LightLower => lightLower;
        public AudioCue LightsFlicker => lightsFlicker;
        public AudioCue LightsDie => lightsDie;
        public AudioCue LightsReturn => lightsReturn;
        public AudioCue GirlsLaugh => girlsLaugh;
        public AudioCue GhostFlinch => ghostFlinch;
    }
}
