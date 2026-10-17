using System;
using UnityEngine;

namespace Remnant.Dialogue
{
    // New speakers are appended: dialogue assets store the numeric value.
    public enum Speaker { Protagonist, Shadow, Narration, Aiko, Bully1, Bully2, Bully3 }

    /// <summary>
    /// How a line is delivered. Normal: brisk typing, confirm skips the typing. Tense: slow typing with jittering
    /// letters, confirm never skips. Auto: the hanging reveal only; no input at all, the box paces itself.
    /// New modes are appended: dialogue assets store the numeric value.
    /// </summary>
    public enum DialogueMode { Normal, Tense, Auto }

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
    }

    [CreateAssetMenu(fileName = "Dialogue", menuName = "Remnant/Dialogue")]
    public class DialogueData : ScriptableObject
    {
        [SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();

        public DialogueLine[] Lines => lines;

        /// <summary>Used by editor/setup tooling to fill the asset.</summary>
        public void SetLines(DialogueLine[] newLines)
        {
            lines = newLines;
        }
    }
}
