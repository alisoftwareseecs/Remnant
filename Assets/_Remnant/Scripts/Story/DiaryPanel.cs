using System;
using Remnant.Interaction;
using Remnant.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Remnant.Story
{
    /// <summary>
    /// Shows one diary page on a paper-style panel. Locks the player and interaction while open
    /// and closes on the Interact press.
    /// </summary>
    public class DiaryPanel : MonoBehaviour
    {
        [Header("Input (Player/Interact from InputSystem_Actions)")]
        [SerializeField] private InputActionReference interactAction;

        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text hintText;

        private PlayerController player;
        private int openFrame = -1;
        private Action onClosed;

        public bool IsOpen { get; private set; }

        /// <summary>Raised after a page was closed.</summary>
        public event Action Closed;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (interactAction != null) interactAction.action.Enable();
        }

        private void OnDisable()
        {
            if (IsOpen) Close();
        }

        public void Show(string title, string text, Action closed = null)
        {
            if (IsOpen) return;

            if (player == null) player = FindFirstObjectByType<PlayerController>();

            titleText.text = title;
            bodyText.text = text;
            onClosed = closed;
            IsOpen = true;
            openFrame = Time.frameCount;

            InteractionLock.Acquire();
            if (player != null) player.SetMovementLocked(true);
            panelRoot.SetActive(true);
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openFrame) return;
            if (interactAction != null && interactAction.action.WasPressedThisFrame()) Close();
        }

        private void Close()
        {
            IsOpen = false;
            panelRoot.SetActive(false);
            InteractionLock.Release();
            if (player != null) player.SetMovementLocked(false);

            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
            Closed?.Invoke();
        }
    }
}
