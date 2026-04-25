using OfficeFlipOut.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

using System;

namespace OfficeFlipOut.Systems
{
    public static class GameRuntimeState
    {
        public static bool IsPaused { get; private set; }
        public static bool IsCinematicInputLocked { get; private set; }
        public static bool IsWin { get; private set; }
        public static bool IsMainMenuOpen { get; private set; }

        public static event Action<bool> MainMenuOpenChanged;

        public static bool ShouldBlockGameplayInput => IsPaused || IsWin || IsCinematicInputLocked || IsMainMenuOpen;

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
                IsMainMenuOpen = false;
                IsPaused = false;
                Time.timeScale = 0f;
            }
            else if (!IsPaused)
            {
                Time.timeScale = 1f;
            }
        }

        public static void SetMainMenuOpen(bool open)
        {
            if (IsMainMenuOpen == open)
            {
                return;
            }

            IsMainMenuOpen = open;
            if (IsMainMenuOpen)
            {
                IsPaused = false;
                IsWin = false;
                Time.timeScale = 0f;
            }
            else if (!IsPaused && !IsWin)
            {
                Time.timeScale = 1f;
            }

            MainMenuOpenChanged?.Invoke(IsMainMenuOpen);
        }

        public static void SetCinematicInputLocked(bool locked)
        {
            IsCinematicInputLocked = locked;
        }

        public static void ResetSession()
        {
            IsPaused = false;
            IsWin = false;
            IsCinematicInputLocked = false;
            IsMainMenuOpen = false;
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

        [Header("Win Condition")]
        [SerializeField] private bool autoWinWhenAllCoworkersFlipped = true;
        [SerializeField] private bool autoWinWhenFinalBossFlipsOut = true;
        [SerializeField] private string finalBossNpcSignalId = "boss_1";

        [Header("Overlay")]
        [SerializeField] private GUISkin guiSkin;

        private ProgressTracker progressTracker;
        private Rage_Meter finalBossMeter;
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

            TryBindFinalBossMeter();
            GameRuntimeState.ResetSession();
            GameRuntimeState.SetMainMenuOpen(true);
        }

        private void OnDisable()
        {
            if (finalBossMeter != null)
            {
                finalBossMeter.FlippedOut -= HandleFinalBossFlippedOut;
            }

            GameRuntimeState.ResetSession();
        }

        private void Update()
        {
            if (GameRuntimeState.IsCinematicInputLocked)
            {
                return;
            }

            if (GameRuntimeState.IsMainMenuOpen)
            {
                return;
            }

            if (autoWinWhenFinalBossFlipsOut && !GameRuntimeState.IsWin)
            {
                if (finalBossMeter == null)
                {
                    TryBindFinalBossMeter();
                }
                else if (finalBossMeter.IsFlippedOut)
                {
                    TriggerWin();
                    return;
                }
            }

            if (!autoWinWhenFinalBossFlipsOut &&
                autoWinWhenAllCoworkersFlipped &&
                !GameRuntimeState.IsWin &&
                AreAllCoworkersFlipped())
            {
                TriggerWin();
            }
        }

        private void TryBindFinalBossMeter()
        {
            if (string.IsNullOrWhiteSpace(finalBossNpcSignalId))
            {
                return;
            }

            Rage_Meter[] meters = FindObjectsByType<Rage_Meter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < meters.Length; i++)
            {
                Rage_Meter meter = meters[i];
                if (meter == null || meter.NpcSignalId != finalBossNpcSignalId)
                {
                    continue;
                }

                if (finalBossMeter == meter)
                {
                    return;
                }

                if (finalBossMeter != null)
                {
                    finalBossMeter.FlippedOut -= HandleFinalBossFlippedOut;
                }

                finalBossMeter = meter;
                finalBossMeter.FlippedOut += HandleFinalBossFlippedOut;
                return;
            }
        }

        private void HandleFinalBossFlippedOut(Rage_Meter meter)
        {
            if (!autoWinWhenFinalBossFlipsOut || meter == null)
            {
                return;
            }

            if (GameRuntimeState.IsWin || GameRuntimeState.IsMainMenuOpen)
            {
                return;
            }

            TriggerWin();
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
            if (!GameRuntimeState.IsWin)
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

            overlayRect.width = 580f;
            overlayRect.height = 320f;
            overlayRect.x = (Screen.width - overlayRect.width) * 0.5f;
            overlayRect.y = (Screen.height - overlayRect.height) * 0.5f;

            GUILayout.BeginArea(overlayRect, GUI.skin.window);
            GUILayout.Space(14f);

            string title = "You Win";
            string subtitle = "The boss flipped out. Office chaos complete.";

            GUILayout.Label(title, pauseTitleStyle);
            GUILayout.Space(10f);
            GUILayout.Label(subtitle, pauseBodyStyle);
            GUILayout.Space(22f);

            if (GUILayout.Button("Restart Game", GUILayout.Height(42f)))
            {
                RestartCurrentScene();
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

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
#else
            return FindObjectOfType<T>();
#endif
        }
    }
}