using OfficeFlipOut.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OfficeFlipOut.Systems
{
    public static class GameRuntimeState
    {
        public static bool IsPaused { get; private set; }
        public static bool IsWin { get; private set; }

        public static bool ShouldBlockGameplayInput => IsPaused || IsWin;

        public static void SetPaused(bool paused)
        {
            if (IsWin)
            {
                paused = false;
            }

            if (IsPaused == paused)
            {
                return;
            }

            IsPaused = paused;
            Time.timeScale = IsPaused ? 0f : 1f;
        }

        public static void SetWin(bool win)
        {
            if (IsWin == win)
            {
                return;
            }

            IsWin = win;
            if (IsWin)
            {
                IsPaused = false;
                Time.timeScale = 0f;
            }
            else if (!IsPaused)
            {
                Time.timeScale = 1f;
            }
        }

        public static void ResetSession()
        {
            IsPaused = false;
            IsWin = false;
            Time.timeScale = 1f;
        }
    }

    [DefaultExecutionOrder(-220)]
    public class GameFlowController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirst<GameFlowController>() != null)
            {
                return;
            }

            GameObject go = new GameObject("GameFlowController");
            go.AddComponent<GameFlowController>();
        }

        [Header("Input")]
        [SerializeField] private KeyCode pauseKey = KeyCode.P;

        [Header("Win Condition")]
        [SerializeField] private bool autoWinWhenAllCoworkersFlipped = true;

        [Header("Overlay")]
        [SerializeField] private GUISkin guiSkin;

        private ProgressTracker progressTracker;
        private Rect overlayRect = new Rect(0f, 0f, 580f, 320f);
        private GUIStyle pauseTitleStyle;
        private GUIStyle pauseBodyStyle;

        private void Awake()
        {
            progressTracker = FindFirst<ProgressTracker>();
            if (progressTracker == null)
            {
                progressTracker = new GameObject("ProgressTracker").AddComponent<ProgressTracker>();
            }

            GameRuntimeState.ResetSession();
        }

        private void OnDisable()
        {
            GameRuntimeState.ResetSession();
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey) && !GameRuntimeState.IsWin && !ClipboardUIState.IsOpen)
            {
                GameRuntimeState.SetPaused(!GameRuntimeState.IsPaused);
            }

            if (autoWinWhenAllCoworkersFlipped && !GameRuntimeState.IsWin && AreAllCoworkersFlipped())
            {
                TriggerWin();
            }
        }

        private bool AreAllCoworkersFlipped()
        {
            if (progressTracker == null)
            {
                return false;
            }

            var snapshots = progressTracker.GetSnapshots();
            if (snapshots == null || snapshots.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                if (!snapshots[i].isFlippedOut)
                {
                    return false;
                }
            }

            return true;
        }

        public void TriggerWin()
        {
            ClipboardUIState.SetOpen(false);
            GameRuntimeState.SetWin(true);
        }

        private void RestartCurrentScene()
        {
            ClipboardUIState.SetOpen(false);
            GameRuntimeState.ResetSession();
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnGUI()
        {
            if (!GameRuntimeState.IsPaused && !GameRuntimeState.IsWin)
            {
                return;
            }

            if (guiSkin != null)
            {
                GUI.skin = guiSkin;
            }

            EnsurePauseOverlayStyles();

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            overlayRect.x = (Screen.width - overlayRect.width) * 0.5f;
            overlayRect.y = (Screen.height - overlayRect.height) * 0.5f;

            GUILayout.BeginArea(overlayRect, GUI.skin.window);
            GUILayout.Space(14f);

            string title = GameRuntimeState.IsWin ? "You Made Everyone Flip Out" : "Paused";
            string subtitle = GameRuntimeState.IsWin
                ? "The whole office lost it on your watch. Congratulations on your job security."
                : "Take a breather and pick your next move.";

            GUILayout.Label(title, pauseTitleStyle);
            GUILayout.Space(10f);
            GUILayout.Label(subtitle, pauseBodyStyle);
            GUILayout.Space(22f);

            if (!GameRuntimeState.IsWin)
            {
                if (GUILayout.Button("Resume", GUILayout.Height(42f)))
                {
                    GameRuntimeState.SetPaused(false);
                }
                GUILayout.Space(10f);
            }

            if (GUILayout.Button("Restart Level", GUILayout.Height(42f)))
            {
                RestartCurrentScene();
            }
            GUILayout.Space(10f);

            if (GUILayout.Button("Quit Game", GUILayout.Height(42f)))
            {
                QuitGame();
            }

            GUILayout.EndArea();
        }

        private void EnsurePauseOverlayStyles()
        {
            if (pauseTitleStyle != null && pauseBodyStyle != null)
            {
                return;
            }

            pauseTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                richText = false
            };

            pauseBodyStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                richText = false
            };
        }

        private static T FindFirst<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
#else
            return FindObjectOfType<T>();
#endif
        }
    }
}