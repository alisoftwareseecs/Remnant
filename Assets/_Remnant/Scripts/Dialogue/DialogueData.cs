using System;
using UnityEngine;

namespace Remnant.Dialogue
{
    // New speakers are appended: dialogue assets store the numeric value.
    public enum Speaker { Protagonist, Shadow, Narration, Aiko, Bully1, Bully2, Bully3 }

    [Serializable]
    public struct DialogueLine
    {
        public Speaker speaker;
        [TextArea(1, 4)] public string text;
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
