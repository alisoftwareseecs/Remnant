using System;
using UnityEngine;

namespace Remnant.Dialogue
{
    public enum Speaker { Protagonist, Shadow, Narration }

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
