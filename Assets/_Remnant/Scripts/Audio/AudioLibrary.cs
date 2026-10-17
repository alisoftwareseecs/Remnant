using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>The cues that code (not just scene components) needs to play by name. Lives on the AudioSystem prefab.</summary>
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

        public AudioCue Pleasant => pleasant;
        public AudioCue Corridor => corridor;
        public AudioCue Diary => diary;
        public AudioCue Storage => storage;
        public AudioCue Transition => transition;
        public AudioCue ShadowAppear => shadowAppear;
        public AudioCue GameOver => gameOver;
    }
}
