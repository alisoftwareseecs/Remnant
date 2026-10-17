using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Story;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Remnant.UI
{
    /// <summary>
    /// The pause menu (Esc, gamepad Start): Resume and Quit to Title. It lives in Resources/PauseMenu, is created once
    /// when the game starts and survives scene changes. The prefab also carries the project's EventSystem, so the title
    /// menu works with the same UI input. Not available in the warning, title and credits scenes, during scene transitions,
    /// game over, the storage-room reveal, or while a dialogue or diary page is open.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private const string ResourcePath = "PauseMenu";

        private static PauseMenu instance;

        [Header("Input (Player/Pause from InputSystem_Actions)")]
        [SerializeField] private InputActionReference pauseAction;

        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        public static bool IsPaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            IsPaused = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SpawnAtStart()
        {
            EnsureExists();
        }

        /// <summary>
        /// Creates the persistent pause menu (and with it the project's EventSystem) if it is not there yet.
        /// Menus that need UI input call this so they work even when the scene was not started through the normal flow.
        /// </summary>
        public static void EnsureExists()
        {
            if (instance != null) return;

            GameObject prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning("PauseMenu: Resources/" + ResourcePath + " is missing.");
                return;
            }

            Instantiate(prefab);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (panel != null) panel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(() => { MenuSound(); Resume(); });
            if (quitButton != null) quitButton.onClick.AddListener(() => { MenuSound(); QuitToTitle(); });
        }

        private void OnEnable()
        {
            if (pauseAction != null) pauseAction.action.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                if (IsPaused) Time.timeScale = 1f;
                IsPaused = false;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPaused) Close();
        }

        private void Update()
        {
            if (pauseAction == null || !pauseAction.action.WasPressedThisFrame()) return;

            if (IsPaused) Resume();
            else if (CanPause()) Open();
        }

        private static bool CanPause()
        {
            if (!SaveSystem.IsGameplayScene(SceneManager.GetActiveScene().name)) return false;

            SceneTransition transition = SceneTransition.Instance;
            if (transition != null && transition.IsTransitioning) return false;
            if (GameOverController.IsActive || StorageRevealSequence.IsRunning) return false;

            DialogueRunner dialogue = FindFirstObjectByType<DialogueRunner>();
            if (dialogue != null && dialogue.BlocksInteraction) return false;

            DiaryPanel diary = FindFirstObjectByType<DiaryPanel>();
            if (diary != null && diary.IsOpen) return false;

            return true;
        }

        private void Open()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
            if (resumeButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        private void Close()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
        }

        public void Resume()
        {
            Close();
        }

        private static void MenuSound()
        {
            Remnant.Audio.SfxPlayer.Play(Remnant.Audio.MusicManager.Cues?.MenuSelect);
        }

        public void QuitToTitle()
        {
            Close();
            SceneManager.LoadScene(SaveSystem.TitleScene);
        }
    }
}
