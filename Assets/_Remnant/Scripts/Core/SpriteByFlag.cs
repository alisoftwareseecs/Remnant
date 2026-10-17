using System;
using UnityEngine;

namespace Remnant.Core
{
    /// <summary>
    /// Swaps a SpriteRenderer's sprite by story flag, so one object can show several versions of itself (Aiko's desk
    /// is defaced on Day 1, half scrubbed on Day 2, cleaned in the epilogue). The first entry whose flag is set wins;
    /// with none set the renderer keeps the sprite it had at the start.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteByFlag : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            [Tooltip("One flag, or several separated by commas (any of them counts as set).")]
            public string flag;
            public Sprite sprite;
        }

        [SerializeField] private Entry[] entries = new Entry[0];

        private SpriteRenderer spriteRenderer;
        private Sprite defaultSprite;
        private GameState state;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            defaultSprite = spriteRenderer.sprite;
        }

        private void Start()
        {
            state = GameState.Instance;
            if (state != null) state.FlagChanged += OnFlagChanged;
            Apply();
        }

        private void OnDestroy()
        {
            if (state != null) state.FlagChanged -= OnFlagChanged;
        }

        private void OnFlagChanged(string flag, bool value)
        {
            Apply();
        }

        private void Apply()
        {
            Sprite chosen = defaultSprite;
            if (state != null)
            {
                foreach (Entry entry in entries)
                {
                    if (entry.sprite != null && state.HasAnyFlag(entry.flag))
                    {
                        chosen = entry.sprite;
                        break;
                    }
                }
            }

            spriteRenderer.sprite = chosen;
        }
    }
}
