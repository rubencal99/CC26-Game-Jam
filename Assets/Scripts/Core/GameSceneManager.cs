using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CC26
{
    // One instance per scene (via prefab). Buttons call the public methods through UnityEvents.
    public class GameSceneManager : MonoBehaviour
    {
        [Tooltip("Toggles pause when performed.")]
        [SerializeField] private InputActionReference pauseAction;

        [Tooltip("Root of the pause screen UI. Shown only while paused.")]
        [SerializeField] private GameObject pauseScreen;

        [Tooltip("Disable in scenes that shouldn't pause, e.g. the main menu.")]
        [SerializeField] private bool canPause = true;

        [Tooltip("Root of the level complete UI. Shown when an Exit is reached.")]
        [SerializeField] private GameObject levelCompleteScreen;

        public bool IsPaused { get; private set; }
        public bool IsLevelComplete { get; private set; }

        private void Awake()
        {
            SetPaused(false);
            if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);
        }

        private void OnEnable()
        {
            if (pauseAction == null) return;
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            if (pauseAction == null) return;
            pauseAction.action.performed -= OnPausePerformed;
        }

        public void LoadScene(string sceneName)
        {
            // timeScale is global and survives scene loads.
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Pause()
        {
            if (canPause) SetPaused(true);
        }

        public void Resume() => SetPaused(false);

        public void TogglePause()
        {
            // Esc must not resume past the complete screen
            if (IsLevelComplete) return;
            if (IsPaused) Resume();
            else Pause();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Freezes the level like pause. Leave via the screen's buttons (LoadScene, ReloadCurrentScene, Quit).
        public void CompleteLevel()
        {
            if (IsLevelComplete) return;
            SetPaused(false);
            IsLevelComplete = true;
            Time.timeScale = 0f;
            if (levelCompleteScreen != null) levelCompleteScreen.SetActive(true);
        }

        private void OnPausePerformed(InputAction.CallbackContext _) => TogglePause();

        private void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (pauseScreen != null) pauseScreen.SetActive(paused);
        }
    }
}
