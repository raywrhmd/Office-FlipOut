using UnityEngine;
using OfficeFlipOut.Systems;

namespace OfficeFlipOut.Systems
{
    /// <summary>
    /// Manages audio state based on game runtime state (paused, menu open, etc).
    /// Pauses and resumes ambient sounds and background music when appropriate.
    /// </summary>
    [DefaultExecutionOrder(-98)]
    public class AudioStateManager : MonoBehaviour
    {
        private static AudioStateManager instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (ProjectBootstrap.FindFirst<AudioStateManager>() != null)
            {
                return;
            }

            GameObject go = new GameObject("AudioStateManager");
            go.AddComponent<AudioStateManager>();
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                SubscribeToGameStateEvents();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ApplyCurrentRuntimeState();
        }

        private void SubscribeToGameStateEvents()
        {
            GameRuntimeState.PauseChanged += HandlePauseStateChanged;
            GameRuntimeState.MainMenuOpenChanged += HandleMainMenuStateChanged;
        }

        private void OnDestroy()
        {
            GameRuntimeState.PauseChanged -= HandlePauseStateChanged;
            GameRuntimeState.MainMenuOpenChanged -= HandleMainMenuStateChanged;
        }

        private void HandlePauseStateChanged(bool isPaused)
        {
            if (GameRuntimeState.IsMainMenuOpen)
            {
                return;
            }

            if (isPaused)
            {
                AmbientSoundManager.PauseAllAmbient();
                AudioManager.PauseBackgroundMusic();
            }
            else
            {
                AmbientSoundManager.ResumeAllAmbient();
                AudioManager.ResumeBackgroundMusic();
            }
        }

        private void HandleMainMenuStateChanged(bool isMenuOpen)
        {
            if (isMenuOpen)
            {
                AmbientSoundManager.StopAllAmbient();
                AudioManager.StopBackgroundMusic();
            }
            else
            {
                AmbientSoundManager.PlayAllAmbient();
                if (AudioManager.IsTemporaryMusicActive())
                {
                    AudioManager.ResumeBackgroundMusic();
                }
                else
                {
                    AudioManager.PlayBackgroundMusic();
                }
            }
        }

        private void ApplyCurrentRuntimeState()
        {
            if (GameRuntimeState.IsMainMenuOpen)
            {
                HandleMainMenuStateChanged(true);
                return;
            }

            if (GameRuntimeState.IsPaused)
            {
                HandlePauseStateChanged(true);
                return;
            }

            AmbientSoundManager.PlayAllAmbient();
            if (AudioManager.IsTemporaryMusicActive())
            {
                AudioManager.ResumeBackgroundMusic();
            }
            else
            {
                AudioManager.PlayBackgroundMusic();
            }
        }

        /// <summary>
        /// Gets the instance of AudioStateManager.
        /// </summary>
        public static AudioStateManager GetInstance()
        {
            return instance;
        }
    }
}
