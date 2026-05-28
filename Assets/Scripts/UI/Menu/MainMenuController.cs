using OfficeFlipOut.Systems;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OfficeFlipOut.UI.Menu
{
    /// <summary>
    /// Owns the Main Menu UIDocument. Decoupled from the clipboard so the menu
    /// can stack independently and isn't dependent on clipboard state.
    /// </summary>
    [DefaultExecutionOrder(-240)]
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private const int RuledLineCount = 20;

        // GameObject creation is handled by UIRootBootstrap.

        [Header("Document")]
        [SerializeField] private VisualTreeAsset documentOverride;

        [Header("Branding")]
        [Tooltip("Title art texture. Defaults to nothing - falls back to text title.")]
        [SerializeField] private Texture2D mainMenuTitleArt;
        [SerializeField] private string subtitleOverride;

        [Header("Bootstrap")]
        [Tooltip("If true, opens the main menu on Awake (matches the legacy GameFlowController behavior).")]
        [SerializeField] private bool openOnAwake = true;

        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement shell;
        private VisualElement board;
        private VisualElement dimmer;
        private VisualElement titleImage;
        private Label titleFallback;
        private Label subtitleLabel;
        private Button startButton;
        private Button quitButton;
        private bool initialized;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            GameRuntimeState.MainMenuOpenChanged += HandleMainMenuOpenChanged;
            TryInitialize();
            if (openOnAwake)
            {
                GameRuntimeState.SetMainMenuOpen(true);
            }
        }

        private void OnDisable()
        {
            GameRuntimeState.MainMenuOpenChanged -= HandleMainMenuOpenChanged;
        }

        private void Update()
        {
            if (!initialized) { TryInitialize(); if (!initialized) return; }
            if (!GameRuntimeState.IsMainMenuOpen) return;

            if (IsSubmitPressedThisFrame())
            {
                StartGame();
            }
            else if (IsEscapePressedThisFrame())
            {
                QuitGame();
            }
        }

        private static bool IsSubmitPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
            return false;
#endif
        }

        private static bool IsEscapePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
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
                VisualTreeAsset asset = Resources.Load<VisualTreeAsset>("UI/Menu/MainMenu");
                if (asset != null)
                {
                    uiDocument.visualTreeAsset = asset;
                }
            }

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            shell = root.Q<VisualElement>("MainMenuShell");
            board = root.Q<VisualElement>("MainMenuBoard");
            dimmer = root.Q<VisualElement>("MainMenuDimmer");
            titleImage = root.Q<VisualElement>("MainMenuTitleImage");
            titleFallback = root.Q<Label>("MainMenuTitleFallback");
            subtitleLabel = root.Q<Label>("MainMenuSubtitle");
            startButton = root.Q<Button>("MainMenuStart");
            quitButton = root.Q<Button>("MainMenuQuit");

            if (shell == null) return;

            GenerateRuledLines();
            ApplyTitleArt();
            if (!string.IsNullOrWhiteSpace(subtitleOverride) && subtitleLabel != null)
            {
                subtitleLabel.text = subtitleOverride;
            }

            if (startButton != null) startButton.clicked += StartGame;
            if (quitButton != null) quitButton.clicked += QuitGame;

            initialized = true;
            ApplyVisibility();
        }

        private void GenerateRuledLines()
        {
            VisualElement container = root.Q<VisualElement>("MainMenuRuledLines");
            if (container == null) return;

            for (int i = 0; i < RuledLineCount; i++)
            {
                float yPercent = 100f * (1f - ((float)(i + 1) / (RuledLineCount + 1)));
                VisualElement line = new VisualElement();
                line.AddToClassList("ruled-line");
                line.style.top = new StyleLength(new Length(yPercent, LengthUnit.Percent));
                line.pickingMode = PickingMode.Ignore;
                container.Add(line);
            }
        }

        private void ApplyTitleArt()
        {
            if (titleImage == null) return;

            if (mainMenuTitleArt != null)
            {
                titleImage.style.backgroundImage = new StyleBackground(mainMenuTitleArt);
                titleImage.RemoveFromClassList("hidden");
                if (titleFallback != null) titleFallback.AddToClassList("hidden-title-fallback");
            }
            else
            {
                titleImage.style.backgroundImage = StyleKeyword.None;
                titleImage.AddToClassList("hidden");
                if (titleFallback != null) titleFallback.RemoveFromClassList("hidden-title-fallback");
            }
        }

        private void HandleMainMenuOpenChanged(bool _) => ApplyVisibility();

        private void ApplyVisibility()
        {
            if (!initialized || shell == null) return;

            bool open = GameRuntimeState.IsMainMenuOpen;
            if (open)
            {
                shell.RemoveFromClassList("hidden");
                if (dimmer != null) dimmer.RemoveFromClassList("anim-hidden");
                if (board != null) board.RemoveFromClassList("anim-closed");
                if (startButton != null) startButton.Focus();
            }
            else
            {
                shell.AddToClassList("hidden");
                if (dimmer != null) dimmer.AddToClassList("anim-hidden");
                if (board != null) board.AddToClassList("anim-closed");
            }
        }

        private void StartGame() => GameRuntimeState.SetMainMenuOpen(false);

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
