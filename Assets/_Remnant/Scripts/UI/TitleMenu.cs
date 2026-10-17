using System.Collections;
using Remnant.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Remnant.UI
{
    /// <summary>
    /// The title screen menu: New Game (clears the flags and the save, starts Day 1), Continue (only with a save),
    /// Quit. Mouse, keyboard and gamepad all work through the project's UI input module.
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private string firstScene = "Present_Classroom";

        private IEnumerator Start()
        {
            // Back on the title screen the game is not paused and nothing is half-finished.
            Time.timeScale = 1f;

            // The EventSystem comes with the persistent pause menu; make sure it exists.
            PauseMenu.EnsureExists();

            bool hasSave = SaveSystem.HasSave;
            if (continueButton != null) continueButton.gameObject.SetActive(hasSave);

            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
            if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);

#if UNITY_WEBGL
            if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif

            // The event system lives in the persistent pause menu object, which may appear a frame later.
            yield return null;
            Select(hasSave && continueButton != null ? continueButton : newGameButton);
        }

        private static void Select(Button button)
        {
            if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private void NewGame()
        {
            GameState state = GameState.Instance;
            if (state != null) state.ResetAll();
            SaveSystem.Clear();

            SceneTransition transition = SceneTransition.Instance;
            if (transition != null) transition.GoTo(firstScene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(firstScene);
        }

        private void ContinueGame()
        {
            if (!SaveSystem.Continue()) NewGame();
        }

        private void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
