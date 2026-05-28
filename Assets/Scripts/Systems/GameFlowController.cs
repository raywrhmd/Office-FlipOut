using OfficeFlipOut.UI;
using OfficeFlipOut.Data;
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
        public static bool IsClipboardOpen { get; private set; }

        public static event Action<bool> MainMenuOpenChanged;
        public static event Action<bool> PauseChanged;
        public static event Action<bool> WorldFrozenChanged;
        public static event Action<bool> WinChanged;

        /// <summary>
        /// True whenever real-world gameplay simulation should be halted: includes
        /// the main menu, win overlay, an explicit pause, or an open clipboard.
        /// Drives Time.timeScale via <see cref="ApplyWorldTimeScale"/>.
        /// </summary>
        public static bool IsWorldFrozen => IsMainMenuOpen || IsWin || IsPaused || IsClipboardOpen;

        public static bool ShouldBlockGameplayInput => IsPaused || IsWin || IsCinematicInputLocked || IsMainMenuOpen || IsClipboardOpen;

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

            bool wasFrozen = IsWorldFrozen;
            IsPaused = paused;
            ApplyWorldTimeScale(wasFrozen);
            PauseChanged?.Invoke(IsPaused);
        }

        public static void SetWin(bool win)
        {
            if (IsWin == win)
            {
                return;
            }

            bool wasFrozen = IsWorldFrozen;
            IsWin = win;
            if (IsWin)
            {
                IsMainMenuOpen = false;
                IsPaused = false;
                IsClipboardOpen = false;
            }
            ApplyWorldTimeScale(wasFrozen);
            WinChanged?.Invoke(IsWin);
        }

        public static void SetMainMenuOpen(bool open)
        {
            if (IsMainMenuOpen == open)
            {
                return;
            }

            bool wasFrozen = IsWorldFrozen;
            IsMainMenuOpen = open;
            if (IsMainMenuOpen)
            {
                IsPaused = false;
                IsWin = false;
                IsClipboardOpen = false;
            }
            ApplyWorldTimeScale(wasFrozen);

            MainMenuOpenChanged?.Invoke(IsMainMenuOpen);
        }

        /// <summary>
        /// Mark the clipboard intel screen as open/closed. The clipboard is a
        /// tactical pause: the world halts while the player reads dossiers and
        /// progress so they're not anxious about NPCs eating the floor mid-read.
        /// </summary>
        public static void SetClipboardOpen(bool open)
        {
            if (IsWin || IsMainMenuOpen)
            {
                open = false;
            }
            if (IsClipboardOpen == open)
            {
                return;
            }

            bool wasFrozen = IsWorldFrozen;
            IsClipboardOpen = open;
            ApplyWorldTimeScale(wasFrozen);
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
            IsClipboardOpen = false;
            Time.timeScale = 1f;
        }

        private static void ApplyWorldTimeScale(bool wasFrozen)
        {
            bool frozen = IsWorldFrozen;
            Time.timeScale = frozen ? 0f : 1f;
            if (frozen != wasFrozen)
            {
                WorldFrozenChanged?.Invoke(frozen);
            }
        }
    }

    [DefaultExecutionOrder(-220)]
    public class GameFlowController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (ProjectBootstrap.FindFirst<GameFlowController>() != null)
            {
                return;
            }

            GameObject go = new GameObject("GameFlowController");
            go.AddComponent<GameFlowController>();
        }

        [Header("Win Condition")]
        [SerializeField] private bool autoWinWhenAllCoworkersFlipped = true;
        [SerializeField] private bool autoWinWhenFinalBossFlipsOut = true;
        [SerializeField] private string finalBossNpcSignalId = NpcIds.Boss;

        private ProgressTracker progressTracker;
        private Rage_Meter finalBossMeter;

        private void Awake()
        {
            progressTracker = ProjectBootstrap.FindFirst<ProgressTracker>();
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

    }
}