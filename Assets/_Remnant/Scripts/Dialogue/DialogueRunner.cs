using System;
using Remnant.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Remnant.Dialogue
{
    public class DialogueRunner : MonoBehaviour
    {
        [Header("Input (Player/Interact from InputSystem_Actions)")]
        [SerializeField] private InputActionReference interactAction;

        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject continueIndicator;

        [Header("Typewriter")]
        [SerializeField, Min(1f)] private float charactersPerSecond = 45f;

        [Header("Speaker styles")]
        [SerializeField] private Color protagonistColor = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] private Color shadowColor = new Color(0.62f, 0.30f, 0.30f);
        [SerializeField] private Color narrationColor = new Color(0.70f, 0.72f, 0.75f);

        private DialogueData current;
        private int lineIndex;
        private float revealed;
        private int totalCharacters;
        private bool typing;
        private int startFrame = -1;
        private int endFrame = -1;
        private Action onComplete;

        public bool IsOpen { get; private set; }

        /// <summary>True while a dialogue is open, and on the frame it closed, so the closing press is not reused.</summary>
        public bool BlocksInteraction => IsOpen || Time.frameCount == endFrame;

        public event Action Started;
        public event Action Ended;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            // The UI lives in a prefab, so the player is found at runtime when not assigned.
            if (player == null) player = FindFirstObjectByType<PlayerController>();
        }

        private void OnEnable()
        {
            if (interactAction != null) interactAction.action.Enable();
        }

        private void OnDisable()
        {
            if (IsOpen) Close();
        }

        public void Play(DialogueData data, Action completed = null)
        {
            if (IsOpen) return;

            if (data == null || data.Lines == null || data.Lines.Length == 0)
            {
                completed?.Invoke();
                return;
            }

            current = data;
            onComplete = completed;
            lineIndex = 0;
            IsOpen = true;
            startFrame = Time.frameCount;

            if (player != null) player.SetMovementLocked(true);
            if (panel != null) panel.SetActive(true);

            Started?.Invoke();
            ShowLine(current.Lines[0]);
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == startFrame) return;

            bool pressed = interactAction != null && interactAction.action.WasPressedThisFrame();

            if (typing)
            {
                revealed += charactersPerSecond * Time.unscaledDeltaTime;
                ApplyReveal();
                if (pressed) FinishTyping();
            }
            else if (pressed)
            {
                Advance();
            }
        }

        private void ShowLine(DialogueLine line)
        {
            bool shadow = line.speaker == Speaker.Shadow;
            bool narration = line.speaker == Speaker.Narration;

            Color color = shadow ? shadowColor : narration ? narrationColor : protagonistColor;
            FontStyles style = shadow || narration ? FontStyles.Italic : FontStyles.Normal;

            if (speakerLabel != null)
            {
                speakerLabel.text = shadow ? "SHADOW" : string.Empty;
                speakerLabel.color = color;
                speakerLabel.fontStyle = FontStyles.Normal;
            }

            bodyText.text = line.text;
            bodyText.color = color;
            bodyText.fontStyle = style;
            bodyText.ForceMeshUpdate();
            totalCharacters = bodyText.textInfo.characterCount;

            revealed = 0f;
            typing = true;
            if (continueIndicator != null) continueIndicator.SetActive(false);
            ApplyReveal();
        }

        private void ApplyReveal()
        {
            int visible = Mathf.Min(totalCharacters, Mathf.FloorToInt(revealed));
            bodyText.maxVisibleCharacters = visible;
            if (visible >= totalCharacters && typing) FinishTyping();
        }

        private void FinishTyping()
        {
            typing = false;
            bodyText.maxVisibleCharacters = int.MaxValue;
            if (continueIndicator != null) continueIndicator.SetActive(true);
        }

        private void Advance()
        {
            lineIndex++;
            if (lineIndex >= current.Lines.Length)
            {
                Close();
                return;
            }

            ShowLine(current.Lines[lineIndex]);
        }

        private void Close()
        {
            IsOpen = false;
            typing = false;
            endFrame = Time.frameCount;
            current = null;

            if (panel != null) panel.SetActive(false);
            if (player != null) player.SetMovementLocked(false);

            Action callback = onComplete;
            onComplete = null;

            Ended?.Invoke();
            callback?.Invoke();
        }
    }
}
