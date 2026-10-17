using System;
using UnityEngine;

namespace Remnant.Dialogue
{
    // New speakers are appended: dialogue assets store the numeric value.
    public enum Speaker { Protagonist, Shadow, Narration, Aiko, Reina, Mika, Saki }

    /// <summary>
    /// How a line is delivered. Normal: brisk typing, confirm skips the typing. Tense: slow typing with jittering
    /// letters, confirm never skips. Auto: the hanging reveal only; no input at all, the box paces itself.
    /// New modes are appended: dialogue assets store the numeric value.
    /// </summary>
    public enum DialogueMode { Normal, Tense, Auto }

    /// <summary>Per-line override of the typing blips. Default = the speaker's setting (and the parenthesis rule).</summary>
    public enum BlipOverride { Default, On, Off }

    [Serializable]
    public struct DialogueLine
    {
        public Speaker speaker;
        [TextArea(1, 4)] public string text;
        public DialogueMode mode;

        [Tooltip("Fade the text colour while it types: from the 'from' colour at the first character to the 'to' colour when the last character appears. Overrides the speaker colour for this line.")]
        public bool fadeColor;
        public Color fadeFrom;
        public Color fadeTo;
        [Tooltip("Also fade the speaker label with the text (only used when Fade Color is on).")]
        public bool fadeLabel;

        [Tooltip("Default: the speaker's blip setting decides (and a protagonist line in parentheses is a thought: no blips). On / Off force it for this line. Auto lines never blip.")]
        public BlipOverride blips;
        [Tooltip("Extra gain in dB for this line's blips (0 = normal, -3 = half the power). Used to keep the Shadow's slow lines heavy instead of chatty.")]
        public float blipGainDb;
    }

    [CreateAssetMenu(fileName = "Dialogue", menuName = "Remnant/Dialogue")]
    public class DialogueData : ScriptableObject
    {
        [SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();

        [Tooltip("Lock the player's movement while this dialogue is open. Off for lines that play while the player keeps walking (the Shadow in the storage hallway).")]
        [SerializeField] private bool lockPlayer = true;

        public DialogueLine[] Lines => lines;
        public bool LockPlayer => lockPlayer;

        /// <summary>Used by editor/setup tooling to fill the asset.</summary>
        public void SetLines(DialogueLine[] newLines)
        {
            lines = newLines;
        }
    }
}
