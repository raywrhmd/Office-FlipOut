using OfficeFlipOut.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OfficeFlipOut.Systems
{
    public static class GameRuntimeState
    {
        public static bool IsPaused { get; private set; }
        public static bool IsCinematicInputLocked { get; private set; }

        public static bool ShouldBlockGameplayInput => IsPaused || IsCinematicInputLocked;

        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused)
            {
                return;
            }

            IsPaused = paused;
            Time.timeScale = IsPaused ? 0f : 1f;
        }

        public static void ResetSession()
        {
            IsPaused = false;
            IsCinematicInputLocked = false;
            Time.timeScale = 1f;
        }

        public static void SetCinematicInputLocked(bool locked)
        {
            IsCinematicInputLocked = locked;
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

        [Header("Overlay")]
        [SerializeField] private GUISkin guiSkin;

        private Rect overlayRect = new Rect(0f, 0f, 580f, 320f);
        private GUIStyle pauseTitleStyle;
        private GUIStyle pauseBodyStyle;

        private void Awake()
        {
            GameRuntimeState.ResetSession();
        }

        private void OnDisable()
        {
            GameRuntimeState.ResetSession();
        }

        private void Update()
        {
            if (GameRuntimeState.IsCinematicInputLocked)
            {
                return;
            }

            if (Input.GetKeyDown(pauseKey) && !ClipboardUIState.IsOpen)
            {
                GameRuntimeState.SetPaused(!GameRuntimeState.IsPaused);
            }
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
            if (!GameRuntimeState.IsPaused)
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

            const string title = "Paused";
            const string subtitle = "Take a breather and pick your next move.";

            GUILayout.Label(title, pauseTitleStyle);
            GUILayout.Space(10f);
            GUILayout.Label(subtitle, pauseBodyStyle);
            GUILayout.Space(22f);

            if (GUILayout.Button("Resume", GUILayout.Height(42f)))
            {
                GameRuntimeState.SetPaused(false);
            }
            GUILayout.Space(10f);

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