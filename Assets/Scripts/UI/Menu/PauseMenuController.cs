using OfficeFlipOut.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OfficeFlipOut.UI.Menu
{
    /// <summary>
    /// Pause menu in its own UIDocument. Esc opens it whenever gameplay is
    /// active (no clipboard, no main menu, no win overlay), giving the player
    /// a proper pause / settings / restart / quit affordance instead of the
    /// clipboard's old confirm popup.
    /// </summary>
    [DefaultExecutionOrder(-235)]
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Document")]
        [SerializeField] private VisualTreeAsset documentOverride;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

        // GameObject creation is handled by UIRootBootstrap.

        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement pauseRoot;
        private VisualElement settingsHost;
        private Button resumeBtn;
        private Button settingsBtn;
        private Button restartBtn;
        private Button toMenuBtn;
        private Button quitBtn;
        private SettingsPanelView settingsView;
        private bool initialized;
        private bool settingsVisible;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            GameRuntimeState.PauseChanged += HandlePauseChanged;
            TryInitialize();
        }

        private void OnDisable()
        {
            GameRuntimeState.PauseChanged -= HandlePauseChanged;
        }

        private void Update()
        {
            if (!initialized) { TryInitialize(); if (!initialized) return; }

            // Esc opens / closes the pause menu, but only when gameplay is
            // actually live (clipboard, main menu, win overlay all suppress it).
            if (IsKeyPressedThisFrame(toggleKey))
            {
                if (GameRuntimeState.IsMainMenuOpen) return;
                if (GameRuntimeState.IsWin) return;
                if (ClipboardUIState.IsOpen) return;
                GameRuntimeState.SetPaused(!GameRuntimeState.IsPaused);
            }
        }

        private static bool IsKeyPressedThisFrame(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.Escape:
                    return Keyboard.current.escapeKey.wasPressedThisFrame;
                default:
                    return false;
            }
#else
            return false;
#endif
        }

        private void TryInitialize()
        {
            if (initialized || uiDocument == null) return;

            if (documentOverride != null)
            {
                uiDocument.visualTreeAsset = documentOverride;
            }
            else if (uiDocument.visualTreeAsset == null)
            {
                VisualTreeAsset asset = Resources.Load<VisualTreeAsset>("UI/Menu/PauseMenu");
                if (asset != null)
                {
                    uiDocument.visualTreeAsset = asset;
                }
            }

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            pauseRoot = root.Q<VisualElement>("PauseMenuRoot");
            settingsHost = root.Q<VisualElement>("PauseSettingsHost");
            resumeBtn = root.Q<Button>("PauseResume");
            settingsBtn = root.Q<Button>("PauseSettings");
            restartBtn = root.Q<Button>("PauseRestart");
            toMenuBtn = root.Q<Button>("PauseToMenu");
            quitBtn = root.Q<Button>("PauseQuit");

            if (pauseRoot == null) return;

            if (resumeBtn != null) resumeBtn.clicked += Resume;
            if (settingsBtn != null) settingsBtn.clicked += ToggleSettings;
            if (restartBtn != null) restartBtn.clicked += RestartRun;
            if (toMenuBtn != null) toMenuBtn.clicked += QuitToMenu;
            if (quitBtn != null) quitBtn.clicked += QuitGame;

            settingsView = new SettingsPanelView();
            if (settingsHost != null)
            {
                settingsHost.Add(settingsView.Root);
                settingsView.SetVisible(false);
            }

            initialized = true;
            ApplyVisibility();
        }

        private void HandlePauseChanged(bool _) => ApplyVisibility();

        private void ApplyVisibility()
        {
            if (!initialized || pauseRoot == null) return;
            bool open = GameRuntimeState.IsPaused;
            pauseRoot.EnableInClassList("pause-root--hidden", !open);
            pauseRoot.pickingMode = open ? PickingMode.Position : PickingMode.Ignore;
            if (open && resumeBtn != null) resumeBtn.Focus();
            if (!open) SetSettingsVisible(false);
        }

        private void Resume() => GameRuntimeState.SetPaused(false);

        private void ToggleSettings() => SetSettingsVisible(!settingsVisible);

        private void SetSettingsVisible(bool visible)
        {
            settingsVisible = visible;
            settingsView?.SetVisible(visible);
            if (settingsBtn != null)
            {
                settingsBtn.text = visible ? "Hide Settings" : "Settings";
            }
        }

        private void RestartRun()
        {
            GameRuntimeState.SetPaused(false);
            GameRuntimeState.ResetSession();
            ClipboardUIState.SetOpen(false);
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

        private void QuitToMenu()
        {
            GameRuntimeState.SetPaused(false);
            ClipboardUIState.SetOpen(false);
            GameRuntimeState.SetMainMenuOpen(true);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
