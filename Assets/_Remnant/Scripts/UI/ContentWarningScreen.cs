using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Remnant.UI
{
    /// <summary>
    /// The first screen of the game: the content warning, written in chalk on a classroom chalkboard at night.
    /// Fade in from black, the lines are written one after another, "Press any key to continue" pulses at the bottom.
    /// After the first line is written any key finishes the writing instantly; once everything is written the next key
    /// wipes the board with an eraser, fades to black and loads the title screen.
    /// </summary>
    public class ContentWarningScreen : MonoBehaviour
    {
        [Serializable]
        private struct ChalkLine
        {
            public TMP_Text text;
            [Tooltip("Characters written per second.")]
            [Min(1f)] public float charsPerSecond;
            [Min(0f)] public float pauseAfterSeconds;
            [Tooltip("The chalky smudge left behind when the eraser sweeps over this line (Image, type Filled / Horizontal).")]
            public Image smear;
        }

        private enum Phase { FadingIn, Writing, Waiting, Wiping, Leaving }

        [Header("Board")]
        [Tooltip("Full-screen black image on top of everything.")]
        [SerializeField] private CanvasGroup blackOverlay;
        [SerializeField, Min(0.1f)] private float fadeInSeconds = 2f;
        [SerializeField, Min(0f)] private float pauseBeforeWritingSeconds = 1f;

        [Header("Writing")]
        [SerializeField] private ChalkLine[] lines;
        [Header("Writing sound")]
        [Tooltip("Looping chalk scratch clip. Each line starts from a random point in it and it is silent between lines.")]
        [SerializeField] private AudioSource writingSource;
        [SerializeField, Range(0f, 1f)] private float writingVolume = 0.35f;
        [SerializeField, Min(0.01f)] private float writingFadeSeconds = 0.06f;

        [Header("Prompt")]
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField, Range(0f, 1f)] private float promptMinAlpha = 0.35f;
        [SerializeField, Min(0.1f)] private float promptPulseSeconds = 2.4f;

        [Header("Wipe")]
        [SerializeField] private RectTransform eraser;
        [SerializeField] private RectTransform boardRect;
        [SerializeField, Min(0.1f)] private float sweepSeconds = 0.7f;
        [SerializeField, Min(0f)] private float eraserWobble = 28f;
        [SerializeField, Min(0.1f)] private float fadeOutSeconds = 1.2f;

        [Header("Wipe sound")]
        [Tooltip("The scraping clip. One stroke per line, starting where a stroke begins in the clip.")]
        [SerializeField] private AudioSource eraserSource;
        [SerializeField, Range(0f, 1f)] private float eraserVolume = 0.9f;
        [Tooltip("Times (seconds) in the clip where a scrape stroke starts; line i uses entry i.")]
        [SerializeField] private float[] eraserStartTimes = { 0.18f, 2.94f, 5.59f, 8.14f };
        [SerializeField] private Vector2 eraserPitchRange = new Vector2(0.93f, 1.07f);
        [SerializeField, Min(0.01f)] private float eraserFadeInSeconds = 0.04f;
        [SerializeField, Min(0.01f)] private float eraserFadeOutSeconds = 0.15f;

        [Header("After")]
        [SerializeField] private string nextScene = "Title";

        private Phase phase = Phase.FadingIn;
        private int linesFinished;
        private bool skipRequested;
        private float promptShownAt = -1f;
        private IDisposable listener;
        private Coroutine writingFade;

        private void Start()
        {
            if (blackOverlay != null) blackOverlay.alpha = 1f;
            if (promptGroup != null) promptGroup.alpha = 0f;
            if (eraser != null) eraser.gameObject.SetActive(false);

            foreach (ChalkLine line in lines)
            {
                if (line.text != null) line.text.maxVisibleCharacters = 0;
                if (line.smear != null) line.smear.fillAmount = 0f;
            }

            listener = InputSystem.onAnyButtonPress.Call(_ => OnAnyPress());
            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            listener?.Dispose();
        }

        private void Update()
        {
            if (promptGroup == null || promptShownAt < 0f || phase != Phase.Waiting) return;

            float t = Time.unscaledTime - promptShownAt;
            float fadeIn = Mathf.Clamp01(t / 0.8f);
            float pulse = Mathf.Lerp(promptMinAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / promptPulseSeconds));
            promptGroup.alpha = fadeIn * pulse;
        }

        private void OnAnyPress()
        {
            switch (phase)
            {
                case Phase.Writing:
                    // skippable once the first line has been written
                    if (linesFinished >= 1) skipRequested = true;
                    break;
                case Phase.Waiting:
                    StartCoroutine(WipeAndLeave());
                    break;
            }
        }

        private IEnumerator Run()
        {
            yield return Fade(blackOverlay, 1f, 0f, fadeInSeconds);
            yield return new WaitForSecondsRealtime(pauseBeforeWritingSeconds);

            phase = Phase.Writing;
            for (int i = 0; i < lines.Length && !skipRequested; i++)
            {
                yield return WriteLine(lines[i]);
                linesFinished = i + 1;

                float pause = lines[i].pauseAfterSeconds;
                while (pause > 0f && !skipRequested)
                {
                    pause -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // a skip (or the end) finishes every line instantly
            foreach (ChalkLine line in lines)
            {
                if (line.text != null) line.text.maxVisibleCharacters = int.MaxValue;
            }

            StopWriting(true);
            skipRequested = false;
            phase = Phase.Waiting;
            promptShownAt = Time.unscaledTime;
        }

        private IEnumerator WriteLine(ChalkLine line)
        {
            if (line.text == null) yield break;

            line.text.ForceMeshUpdate();
            int total = line.text.textInfo.characterCount;
            float baseDelay = 1f / Mathf.Max(1f, line.charsPerSecond);

            StartWritingSound(total, line.charsPerSecond);

            for (int i = 1; i <= total && !skipRequested; i++)
            {
                line.text.maxVisibleCharacters = i;

                char c = line.text.textInfo.characterInfo[i - 1].character;
                float delay = baseDelay * UnityEngine.Random.Range(0.6f, 1.5f);
                if (c == ',' || c == '.') delay += 0.22f;
                else if (c == ' ') delay *= 1.15f;

                while (delay > 0f && !skipRequested)
                {
                    delay -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            line.text.maxVisibleCharacters = int.MaxValue;
            StopWriting(skipRequested);
        }

        private void StartWritingSound(int characters, float charsPerSecond)
        {
            if (writingSource == null || writingSource.clip == null) return;

            // start somewhere different in the clip each line, far enough from its end that it does not wrap
            float expected = characters / Mathf.Max(1f, charsPerSecond) * 1.3f + 0.5f;
            float latestStart = Mathf.Max(0f, writingSource.clip.length - 0.5f - expected);

            if (writingFade != null) StopCoroutine(writingFade);
            writingSource.Stop();
            writingSource.loop = true;
            writingSource.volume = 0f;
            writingSource.time = UnityEngine.Random.Range(0f, latestStart);
            writingSource.Play();
            writingFade = StartCoroutine(FadeVolume(writingSource, writingVolume, writingFadeSeconds, false));
        }

        /// <summary>Stops the chalk sound: immediately when skipped, otherwise with a very short fade (no click).</summary>
        private void StopWriting(bool immediate)
        {
            if (writingSource == null) return;

            if (writingFade != null) StopCoroutine(writingFade);
            writingFade = null;

            if (immediate)
            {
                writingSource.Stop();
                writingSource.volume = 0f;
            }
            else
            {
                writingFade = StartCoroutine(FadeVolume(writingSource, 0f, writingFadeSeconds, true));
            }
        }

        private static IEnumerator FadeVolume(AudioSource source, float target, float seconds, bool stopAtEnd)
        {
            float from = source.volume;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                source.volume = Mathf.Lerp(from, target, elapsed / seconds);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            source.volume = target;
            if (stopAtEnd) source.Stop();
        }

        private IEnumerator WipeAndLeave()
        {
            phase = Phase.Wiping;
            if (promptGroup != null) StartCoroutine(Fade(promptGroup, promptGroup.alpha, 0f, 0.25f));

            if (eraser != null && boardRect != null)
            {
                Vector3[] corners = new Vector3[4];
                boardRect.GetWorldCorners(corners);
                float left = corners[0].x;
                float right = corners[2].x;
                float halfWidth = eraser.rect.width * eraser.lossyScale.x * 0.5f;
                float startX = left - halfWidth;
                float endX = right + halfWidth;

                eraser.gameObject.SetActive(true);

                for (int i = 0; i < lines.Length; i++)
                {
                    ChalkLine line = lines[i];
                    if (line.text == null) continue;

                    ChalkTextEffect effect = line.text.GetComponent<ChalkTextEffect>();
                    int dir = i % 2 == 0 ? 1 : -1;
                    float fromX = dir > 0 ? startX : endX;
                    float toX = dir > 0 ? endX : startX;
                    Vector3 bandCentre = line.text.transform.TransformPoint(line.text.textBounds.center);
                    float bandHeight = line.text.textBounds.size.y * line.text.transform.lossyScale.y;
                    float wobble = eraserWobble * eraser.lossyScale.y;

                    if (line.smear != null)
                    {
                        line.smear.fillOrigin = dir > 0 ? 0 : 1;
                        line.smear.fillAmount = 0f;
                    }

                    StartScrape(i);

                    float elapsed = 0f;
                    while (elapsed < sweepSeconds)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float p = Mathf.Clamp01(elapsed / sweepSeconds);
                        float eased = p * p * (3f - 2f * p);
                        float x = Mathf.Lerp(fromX, toX, eased);
                        float y = bandCentre.y + Mathf.Sin(p * Mathf.PI * 4f) * Mathf.Min(wobble, bandHeight * 0.5f);

                        eraser.position = new Vector3(x, y, eraser.position.z);
                        eraser.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * Mathf.PI * 4f) * 6f);

                        if (eraserSource != null)
                        {
                            float fadeIn = Mathf.Clamp01(elapsed / eraserFadeInSeconds);
                            float fadeOut = Mathf.Clamp01((sweepSeconds - elapsed) / eraserFadeOutSeconds);
                            eraserSource.volume = eraserVolume * fadeIn * fadeOut;
                        }

                        float front = x + dir * halfWidth * 0.4f;
                        if (effect != null) effect.SetEraser(front, dir);
                        if (line.smear != null) line.smear.fillAmount = eased;
                        yield return null;
                    }

                    if (effect != null) effect.SetEraser(dir > 0 ? endX + halfWidth : startX - halfWidth, dir);
                    if (eraserSource != null) eraserSource.Stop();
                }

                eraser.gameObject.SetActive(false);
            }

            phase = Phase.Leaving;
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Fade(blackOverlay, 0f, 1f, fadeOutSeconds);
            SceneManager.LoadScene(nextScene);
        }

        private void StartScrape(int lineIndex)
        {
            if (eraserSource == null || eraserSource.clip == null) return;

            float start = eraserStartTimes != null && eraserStartTimes.Length > 0
                ? eraserStartTimes[lineIndex % eraserStartTimes.Length]
                : 0f;
            eraserSource.Stop();
            eraserSource.loop = false;
            eraserSource.volume = 0f;
            eraserSource.pitch = UnityEngine.Random.Range(eraserPitchRange.x, eraserPitchRange.y);
            eraserSource.time = Mathf.Clamp(start, 0f, eraserSource.clip.length - 0.1f);
            eraserSource.Play();
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
        {
            if (group == null) yield break;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                group.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            group.alpha = to;
        }
    }
}
