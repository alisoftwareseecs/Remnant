using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Remnant.Dialogue
{
    /// <summary>
    /// Per-character vertex jitter for a TMP text: every visible character gets a small random offset that is re-rolled
    /// every <see cref="interval"/> seconds (the characters move, the text box does not). The whole text can jitter
    /// (<see cref="BaseAmplitude"/>), and single ranges of characters can jitter harder (<see cref="RangeAmplitude"/>,
    /// used for the inline shake tag). Offsets are whole canvas pixels, so the pixel font stays crisp.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class JitterText : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float interval = 0.06f;

        private TMP_Text text;
        private readonly List<Vector2Int> ranges = new List<Vector2Int>();

        /// <summary>Jitter (canvas pixels) applied to every character; 0 = none.</summary>
        public float BaseAmplitude { get; set; }

        /// <summary>Jitter applied to the characters inside the ranges.</summary>
        public float RangeAmplitude { get; set; } = 2f;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        /// <summary>Sets the character ranges [start, end) that shake.</summary>
        public void SetRanges(List<Vector2Int> newRanges)
        {
            ranges.Clear();
            if (newRanges != null) ranges.AddRange(newRanges);
        }

        private void LateUpdate()
        {
            if (BaseAmplitude <= 0f && ranges.Count == 0) return;

            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            int visibleLimit = Mathf.Min(info.characterCount, text.maxVisibleCharacters);
            int tick = Mathf.FloorToInt(Time.unscaledTime / interval);

            for (int i = 0; i < visibleLimit; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (!ch.isVisible) continue;

                float amplitude = BaseAmplitude;
                for (int r = 0; r < ranges.Count; r++)
                {
                    if (i >= ranges[r].x && i < ranges[r].y) amplitude = Mathf.Max(amplitude, RangeAmplitude);
                }

                if (amplitude <= 0f) continue;

                float ox = Mathf.Round((Hash(i, tick, 1) - 0.5f) * 2f * amplitude);
                float oy = Mathf.Round((Hash(i, tick, 2) - 0.5f) * 2f * amplitude);
                Vector3[] verts = info.meshInfo[ch.materialReferenceIndex].vertices;
                int v = ch.vertexIndex;
                Vector3 offset = new Vector3(ox, oy, 0f);
                for (int k = 0; k < 4; k++) verts[v + k] += offset;
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private static float Hash(int index, int tick, int salt)
        {
            float x = Mathf.Sin(index * 12.9898f + tick * 78.233f + salt * 37.719f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }
}
