using Remnant.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Remnant.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Remnant.Dialogue
{
    /// <summary>
    /// The one dialogue box of the game (Undertale style): a black box with a white pixel border at the bottom of the
    /// screen, pixel font, typewriter text. Every line carries a <see cref="DialogueMode"/>:
    /// Normal (confirm skips typing), Tense (slow, jittering letters, confirm never skips) and Auto (the hanging reveal:
    /// all input ignored, the box paces itself). Lines of the Shadow are crimson, everything else is white.
    /// Inline tag: &lt;shake&gt;word&lt;/shake&gt; jitters just those characters.
    /// All dialogue in the game goes through the DialogueBox prefab, which carries this component.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        private const string ShakeOpen = "<shake>";
        private const string ShakeClose = "</shake>";

        private struct Page
        {
            public string text;
            public List<Vector2Int> shake;
        }

        [Header("Input (Player/Interact from InputSystem_Actions)")]
        [SerializeField] private InputActionReference interactAction;

        [Header("References")]
        [SerializeField] private PlayerController player;
        [Tooltip("The box (border + background + text). Hidden when no dialogue is open.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private CanvasGroup boxGroup;
        [SerializeField] private RectTransform speakerBox;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private JitterText jitter;
        [SerializeField] private GameObject continueIndicator;

        [Header("Style")]
        [SerializeField] private Color defaultColor = Color.white;
        [Tooltip("Only the Shadow speaks in a colour.")]
        [SerializeField] private Color shadowColor = new Color(0.690f, 0.063f, 0.165f);
        [Tooltip("Start every page with '* ' like Undertale.")]
        [SerializeField] private bool starPrefix;
        [SerializeField, Min(1)] private int maxLines = 3;
        [SerializeField, Min(0f)] private float speakerLabelPadding = 36f;

        [Header("Punctuation pauses at Normal speed (seconds)")]
        [Tooltip("After a comma, semicolon, colon or dash.")]
        [SerializeField, Min(0f)] private float commaPauseSeconds = 0.14f;
        [Tooltip("After . ? or !")]
        [SerializeField, Min(0f)] private float sentencePauseSeconds = 0.28f;
        [Tooltip("After each dot inside an ellipsis (...) except the last.")]
        [SerializeField, Min(0f)] private float ellipsisDotPauseSeconds = 0.05f;
        [Tooltip("After the end of an ellipsis (...) or an ellipsis character.")]
        [SerializeField, Min(0f)] private float normalEllipsisSeconds = 0.45f;

        [Header("Normal")]
        [SerializeField, Min(1f)] private float normalCharsPerSecond = 35f;

        [Header("Tense")]
        [SerializeField, Min(1f)] private float tenseCharsPerSecond = 18f;
        [Tooltip("Multiplies the comma and sentence pauses.")]
        [SerializeField, Min(0.1f)] private float tensePauseScale = 1.33f;
        [Tooltip("The pause after an ellipsis (...) in Tense mode.")]
        [SerializeField, Min(0f)] private float tenseEllipsisSeconds = 0.4f;
        [SerializeField, Min(0f)] private float tenseJitter = 1.5f;

        [Header("Auto (hanging reveal)")]
        [SerializeField, Min(1f)] private float autoCharsPerSecond = 12f;
        [Tooltip("Multiplies the comma and sentence pauses.")]
        [SerializeField, Min(0.1f)] private float autoPauseScale = 3f;
        [Tooltip("The pause after an ellipsis (...) in Auto mode.")]
        [SerializeField, Min(0f)] private float autoEllipsisSeconds = 1.35f;
        [SerializeField, Min(0f)] private float autoJitter = 0.5f;
        [Tooltip("A finished page stays on screen for max(Hold Min, characters x Hold Per Character) seconds.")]
        [SerializeField, Min(0f)] private float autoHoldMinSeconds = 2.2f;
        [SerializeField, Min(0f)] private float autoHoldPerCharacter = 0.065f;
        [SerializeField, Min(0.1f)] private float autoTextFadeSeconds = 0.8f;
        [SerializeField, Min(0f)] private float autoSilenceSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float autoBoxFadeSeconds = 1f;

        [Header("Inline shake tag")]
        [SerializeField, Min(0f)] private float shakeJitter = 2f;

        [Header("Continue indicator")]
        [SerializeField, Min(0.05f)] private float indicatorBlinkSeconds = 0.4f;

        [Header("Typing sounds (clips are optional)")]
        [SerializeField] private AudioSource blipSource;
        [SerializeField] private AudioClip defaultBlip;
        [SerializeField] private AudioClip shadowBlip;
        [SerializeField, Min(1)] private int blipEveryCharacters = 2;
        [SerializeField] private Vector2 blipPitchRange = new Vector2(0.95f, 1.05f);

        private int startFrame = -1;
        private int endFrame = -1;
        private Action onComplete;
        private Coroutine routine;
        private bool indicatorActive;
        private bool lockedPlayer;
        private int currentLineIndex;
        private float currentLineStartedAt;
        private readonly Queue<KeyValuePair<DialogueData, Action>> pending = new Queue<KeyValuePair<DialogueData, Action>>();

        public bool IsOpen { get; private set; }

        /// <summary>
        /// For a dialogue that paces itself (Auto): roughly how many seconds it still needs, from the typing, pauses, holds and
        /// fades of the lines that are left. Other modes wait for the player, so only their typing time is counted.
        /// </summary>
        public float EstimatedRemainingSeconds
        {
            get
            {
                if (!IsOpen || CurrentDialogue == null) return 0f;

                DialogueLine[] lines = CurrentDialogue.Lines;
                float remaining = Mathf.Max(0f, EstimateLineSeconds(lines[currentLineIndex]) - (Time.unscaledTime - currentLineStartedAt));
                for (int i = currentLineIndex + 1; i < lines.Length; i++) remaining += EstimateLineSeconds(lines[i]);
                if (lines[lines.Length - 1].mode == DialogueMode.Auto) remaining += autoBoxFadeSeconds;
                return remaining;
            }
        }

        /// <summary>How long a dialogue (its typing, holds and fades) takes from its first character to its last fade, in seconds.</summary>
        public float EstimateDurationSeconds(DialogueData data)
        {
            if (data == null) return 0f;

            float total = 0f;
            foreach (DialogueLine line in data.Lines) total += EstimateLineSeconds(line);
            if (data.Lines.Length > 0 && data.Lines[0].mode == DialogueMode.Auto) total += autoBoxFadeSeconds;
            if (data.Lines.Length > 0 && data.Lines[data.Lines.Length - 1].mode == DialogueMode.Auto) total += autoBoxFadeSeconds;
            return total;
        }

        private float EstimateLineSeconds(DialogueLine line)
        {
            string plain = ParseMarkup(line.text ?? string.Empty, out List<Vector2Int> unused);
            float seconds = 0f;
            float perCharacter = 1f / CharsPerSecond(line.mode);
            // the first letter appears at once and nothing waits after the last one, so only the gaps between letters count
            for (int i = 0; i < plain.Length - 1; i++) seconds += perCharacter + PauseAfter(plain, i, line.mode);

            if (line.mode == DialogueMode.Auto)
            {
                seconds += Mathf.Max(autoHoldMinSeconds, plain.Length * autoHoldPerCharacter) + autoTextFadeSeconds + autoSilenceSeconds;
            }

            return seconds;
        }

        /// <summary>The dialogue that is open right now (null when none).</summary>
        public DialogueData CurrentDialogue { get; private set; }

        /// <summary>True while a dialogue is open, and on the frame it closed, so the closing press is not reused.</summary>
        public bool BlocksInteraction => IsOpen || Time.frameCount == endFrame;

        public event Action Started;
        public event Action Ended;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (continueIndicator != null) continueIndicator.SetActive(false);
            // The UI lives in a prefab, so the player is found at runtime when not assigned.
            if (player == null) player = FindFirstObjectByType<PlayerController>();
        }

        private void OnEnable()
        {
            if (interactAction != null) interactAction.action.Enable();
        }

        private void OnDisable()
        {
            // while everything is being destroyed (Play mode stop, scene unload, quit) the box just shuts; the completion
            // callbacks would touch objects that are already gone
            if (IsOpen) Close(!AppLifetime.IsTearingDown(gameObject));
        }

        /// <summary>Plays a dialogue. If another one is open, this one waits its turn (its callback still fires).</summary>
        public void Play(DialogueData data, Action completed = null)
        {
            if (IsOpen)
            {
                pending.Enqueue(new KeyValuePair<DialogueData, Action>(data, completed));
                return;
            }

            if (data == null || data.Lines == null || data.Lines.Length == 0)
            {
                completed?.Invoke();
                return;
            }

            onComplete = completed;
            CurrentDialogue = data;
            IsOpen = true;
            startFrame = Time.frameCount;

            lockedPlayer = data.LockPlayer && player != null;
            if (lockedPlayer) player.SetMovementLocked(true);
            if (panel != null) panel.SetActive(true);
            if (boxGroup != null) boxGroup.alpha = 1f;
            Canvas.ForceUpdateCanvases();

            Started?.Invoke();
            routine = StartCoroutine(Run(data));
        }

        private void Update()
        {
            if (continueIndicator == null) return;

            if (!indicatorActive)
            {
                if (continueIndicator.activeSelf) continueIndicator.SetActive(false);
                return;
            }

            bool on = Mathf.FloorToInt(Time.unscaledTime / indicatorBlinkSeconds) % 2 == 0;
            if (continueIndicator.activeSelf != on) continueIndicator.SetActive(on);
        }

        // ------------------------------------------------------------------ sequence

        private IEnumerator Run(DialogueData data)
        {
            DialogueLine[] lines = data.Lines;

            if (lines[0].mode == DialogueMode.Auto)
            {
                SetBoxAlpha(0f);
                yield return FadeBox(0f, 1f, autoBoxFadeSeconds);
            }

            for (int i = 0; i < lines.Length; i++)
            {
                currentLineIndex = i;
                currentLineStartedAt = Time.unscaledTime;
                yield return ShowLine(lines[i]);
            }

            if (lines[lines.Length - 1].mode == DialogueMode.Auto) yield return FadeBox(1f, 0f, autoBoxFadeSeconds);

            routine = null;
            Close();
        }

        private IEnumerator ShowLine(DialogueLine line)
        {
            bool shadow = line.speaker == Speaker.Shadow;
            Color color = shadow ? shadowColor : defaultColor;
            bool auto = line.mode == DialogueMode.Auto;

            ApplySpeaker(line.speaker, color);
            Color startColor = line.fadeColor ? Opaque(line.fadeFrom) : color;
            bodyText.color = startColor;
            if (line.fadeColor && line.fadeLabel && speakerLabel != null) speakerLabel.color = startColor;
            bodyText.richText = false;
            bodyText.text = string.Empty;

            List<Page> pages = BuildPages(line.text);
            for (int p = 0; p < pages.Count; p++)
            {
                Page page = pages[p];
                bodyText.color = startColor;
                bodyText.alpha = 1f;
                bodyText.text = page.text;
                bodyText.maxVisibleCharacters = 0;
                bodyText.ForceMeshUpdate();

                ConfigureJitter(line.mode, page.shake);

                yield return TypePage(page.text, line, shadow);

                if (auto)
                {
                    int count = bodyText.textInfo.characterCount;
                    yield return WaitUnscaled(Mathf.Max(autoHoldMinSeconds, count * autoHoldPerCharacter));
                    yield return FadeText(autoTextFadeSeconds);
                    bodyText.text = string.Empty;
                    jitter.SetRanges(null);
                    yield return WaitUnscaled(autoSilenceSeconds);
                }
                else
                {
                    // wait for the confirm key; the press that finished the typing is a frame older than this loop
                    indicatorActive = true;
                    yield return null;
                    while (!Pressed()) yield return null;
                    indicatorActive = false;
                }
            }
        }

        private IEnumerator TypePage(string text, DialogueLine line, bool shadow)
        {
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;
            float cps = CharsPerSecond(line.mode);
            AudioClip blip = line.mode == DialogueMode.Auto ? null : (shadow ? shadowBlip : defaultBlip);

            float acc = 0f;
            float nextDelay = 0f;
            int shown = 0;
            int blipCounter = 0;

            while (shown < total)
            {
                // Normal skips the typing, Tense and Auto never do
                if (line.mode == DialogueMode.Normal && Pressed()) break;

                acc += Time.unscaledDeltaTime;
                while (shown < total && acc >= nextDelay)
                {
                    acc -= nextDelay;
                    shown++;
                    bodyText.maxVisibleCharacters = shown;

                    char c = text[shown - 1];
                    if (blip != null && !char.IsWhiteSpace(c) && !char.IsPunctuation(c))
                    {
                        if (blipCounter % Mathf.Max(1, blipEveryCharacters) == 0) PlayBlip(blip);
                        blipCounter++;
                    }

                    nextDelay = 1f / cps + PauseAfter(text, shown - 1, line.mode);
                }

                if (line.fadeColor && total > 0)
                {
                    // smooth between characters too, so the colour never steps
                    float progress = shown >= total ? 1f : (shown + Mathf.Clamp01(acc / Mathf.Max(0.0001f, nextDelay))) / total;
                    Color faded = Color.Lerp(Opaque(line.fadeFrom), Opaque(line.fadeTo), progress);
                    bodyText.color = faded;
                    if (line.fadeLabel && speakerLabel != null) speakerLabel.color = faded;
                }

                yield return null;
            }

            bodyText.maxVisibleCharacters = int.MaxValue;
            if (line.fadeColor)
            {
                bodyText.color = Opaque(line.fadeTo);
                if (line.fadeLabel && speakerLabel != null) speakerLabel.color = Opaque(line.fadeTo);
            }
        }

        // ------------------------------------------------------------------ pages

        /// <summary>Strips the shake tag, adds the optional prefix and splits the text into pages of at most maxLines lines.</summary>
        private List<Page> BuildPages(string raw)
        {
            string plain = ParseMarkup(raw ?? string.Empty, out List<Vector2Int> shake);

            if (starPrefix)
            {
                plain = "* " + plain;
                for (int i = 0; i < shake.Count; i++) shake[i] = new Vector2Int(shake[i].x + 2, shake[i].y + 2);
            }

            List<Page> pages = new List<Page>();
            if (plain.Length == 0)
            {
                pages.Add(new Page { text = string.Empty, shake = new List<Vector2Int>() });
                return pages;
            }

            // tokens: a word with the spaces behind it; ends[] holds the index just after each token
            List<int> ends = new List<int>();
            int idx = 0;
            while (idx < plain.Length)
            {
                while (idx < plain.Length && !char.IsWhiteSpace(plain[idx])) idx++;
                while (idx < plain.Length && char.IsWhiteSpace(plain[idx])) idx++;
                ends.Add(idx);
            }

            int start = 0;
            int token = 0;
            while (token < ends.Count)
            {
                int good = start;
                int t = token;
                while (t < ends.Count)
                {
                    if (!Fits(plain.Substring(start, ends[t] - start).TrimEnd()))
                    {
                        break;
                    }

                    good = ends[t];
                    t++;
                }

                // a single token that never fits still goes on a page of its own
                if (good == start)
                {
                    good = ends[token];
                    t = token + 1;
                }

                pages.Add(MakePage(plain, start, good, shake));
                start = good;
                token = t;
            }

            return pages;
        }

        private bool Fits(string candidate)
        {
            bodyText.text = candidate;
            bodyText.maxVisibleCharacters = int.MaxValue;
            bodyText.ForceMeshUpdate();
            return bodyText.textInfo.lineCount <= maxLines;
        }

        private static Page MakePage(string plain, int start, int end, List<Vector2Int> shake)
        {
            string text = plain.Substring(start, end - start).TrimEnd();
            List<Vector2Int> local = new List<Vector2Int>();
            for (int i = 0; i < shake.Count; i++)
            {
                int a = Mathf.Max(shake[i].x, start);
                int b = Mathf.Min(shake[i].y, start + text.Length);
                if (b > a) local.Add(new Vector2Int(a - start, b - start));
            }

            return new Page { text = text, shake = local };
        }

        /// <summary>Removes the shake tags and returns the character ranges they covered.</summary>
        private static string ParseMarkup(string raw, out List<Vector2Int> shake)
        {
            shake = new List<Vector2Int>();
            StringBuilder plain = new StringBuilder(raw.Length);
            int open = -1;
            int i = 0;
            while (i < raw.Length)
            {
                if (string.CompareOrdinal(raw, i, ShakeOpen, 0, ShakeOpen.Length) == 0)
                {
                    open = plain.Length;
                    i += ShakeOpen.Length;
                }
                else if (string.CompareOrdinal(raw, i, ShakeClose, 0, ShakeClose.Length) == 0)
                {
                    if (open >= 0 && plain.Length > open) shake.Add(new Vector2Int(open, plain.Length));
                    open = -1;
                    i += ShakeClose.Length;
                }
                else
                {
                    plain.Append(raw[i]);
                    i++;
                }
            }

            return plain.ToString();
        }

        // ------------------------------------------------------------------ pacing

        private float CharsPerSecond(DialogueMode mode)
        {
            switch (mode)
            {
                case DialogueMode.Tense: return tenseCharsPerSecond;
                case DialogueMode.Auto: return autoCharsPerSecond;
                default: return normalCharsPerSecond;
            }
        }

        /// <summary>Pause after typing the character at index i: commas short, sentence ends longer, an ellipsis longest.</summary>
        private float PauseAfter(string text, int i, DialogueMode mode)
        {
            char c = text[i];
            float scale = mode == DialogueMode.Tense ? tensePauseScale : mode == DialogueMode.Auto ? autoPauseScale : 1f;

            if (c == '\u2026') return EllipsisSeconds(mode);

            if (c == '.')
            {
                bool nextIsDot = i + 1 < text.Length && text[i + 1] == '.';
                if (nextIsDot) return ellipsisDotPauseSeconds * scale;
                bool endOfEllipsis = i >= 2 && text[i - 1] == '.' && text[i - 2] == '.';
                if (endOfEllipsis) return EllipsisSeconds(mode);
                return sentencePauseSeconds * scale;
            }

            if (c == '?' || c == '!') return sentencePauseSeconds * scale;
            if (c == ',' || c == ';' || c == ':' || c == '\u2014') return commaPauseSeconds * scale;
            return 0f;
        }

        private float EllipsisSeconds(DialogueMode mode)
        {
            switch (mode)
            {
                case DialogueMode.Tense: return tenseEllipsisSeconds;
                case DialogueMode.Auto: return autoEllipsisSeconds;
                default: return normalEllipsisSeconds;
            }
        }

        private void ConfigureJitter(DialogueMode mode, List<Vector2Int> shake)
        {
            if (jitter == null) return;

            jitter.BaseAmplitude = mode == DialogueMode.Tense ? tenseJitter : mode == DialogueMode.Auto ? autoJitter : 0f;
            jitter.RangeAmplitude = shakeJitter;
            jitter.SetRanges(shake);
        }

        // ------------------------------------------------------------------ small helpers

        private void ApplySpeaker(Speaker speaker, Color color)
        {
            string label = LabelFor(speaker);
            bool show = !string.IsNullOrEmpty(label);

            if (speakerBox != null) speakerBox.gameObject.SetActive(show);
            if (!show || speakerLabel == null) return;

            speakerLabel.text = label;
            speakerLabel.color = color;
            speakerLabel.ForceMeshUpdate();
            if (speakerBox != null)
            {
                speakerBox.sizeDelta = new Vector2(speakerLabel.preferredWidth + speakerLabelPadding, speakerBox.sizeDelta.y);
            }
        }

        private static string LabelFor(Speaker speaker)
        {
            switch (speaker)
            {
                case Speaker.Shadow: return "SHADOW";
                case Speaker.Aiko: return "AIKO";
                case Speaker.Reina: return "REINA";
                case Speaker.Mika: return "MIKA";
                case Speaker.Saki: return "SAKI";
                default: return string.Empty;
            }
        }

        private static Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        private bool Pressed()
        {
            if (Time.frameCount == startFrame) return false;
            return interactAction != null && interactAction.action.WasPressedThisFrame();
        }

        private void PlayBlip(AudioClip clip)
        {
            if (blipSource == null || clip == null) return;
            blipSource.pitch = UnityEngine.Random.Range(blipPitchRange.x, blipPitchRange.y);
            blipSource.PlayOneShot(clip);
        }

        private void SetBoxAlpha(float alpha)
        {
            if (boxGroup != null) boxGroup.alpha = alpha;
        }

        private IEnumerator FadeBox(float from, float to, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                SetBoxAlpha(Mathf.Lerp(from, to, elapsed / seconds));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetBoxAlpha(to);
        }

        private IEnumerator FadeText(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                bodyText.alpha = Mathf.Lerp(1f, 0f, elapsed / seconds);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            bodyText.alpha = 0f;
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void Close(bool notify = true)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            IsOpen = false;
            CurrentDialogue = null;
            indicatorActive = false;
            endFrame = Time.frameCount;

            if (jitter != null)
            {
                jitter.BaseAmplitude = 0f;
                jitter.SetRanges(null);
            }

            if (continueIndicator != null) continueIndicator.SetActive(false);
            if (panel != null) panel.SetActive(false);
            if (boxGroup != null) boxGroup.alpha = 1f;
            if (lockedPlayer && player != null) player.SetMovementLocked(false);
            lockedPlayer = false;

            Action callback = onComplete;
            onComplete = null;

            if (!notify)
            {
                pending.Clear();
                return;
            }

            Ended?.Invoke();
            callback?.Invoke();

            // a dialogue that was asked for while this one was open goes next (unless the callback already started one)
            if (!IsOpen && pending.Count > 0)
            {
                KeyValuePair<DialogueData, Action> next = pending.Dequeue();
                Play(next.Key, next.Value);
            }
        }
    }
}
