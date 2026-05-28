using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace OfficeFlipOut.Systems
{
    /// <summary>
    /// Centralized audio management system that loads and provides access to all game audio clips.
    /// Handles background music, SFX, and ambient sounds.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class AudioManager : MonoBehaviour
    {
        public enum AudioClipType
        {
            BackgroundMusic,
            MicrowaveDing,
            WalkingSteps,
            AirConditioner,
            FluorescentLights,
            KeyboardTyping,
            TitleScreenMusic,
            RideOfTheValkyries
        }

        [Header("Audio Clip References")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip microwaveDing;
        [SerializeField] private AudioClip walkingSteps;
        [SerializeField] private AudioClip airConditioner;
        [SerializeField] private AudioClip fluorescentLights;
        [SerializeField] private AudioClip keyboardTyping;
        [SerializeField] private AudioClip titleScreenMusic;
        [SerializeField] private AudioClip rideOfTheValkyries;

        [Header("Music Settings")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
        [SerializeField] private bool playBackgroundMusicOnStart = true;

        [Header("SFX Settings")]
        [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 0.65f;
        [SerializeField, Min(4)] private int pooledSfxSources = 16;

        [Header("Mixer Routing (Optional)")]
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup ambienceMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;

        private static AudioManager instance;
        private Dictionary<AudioClipType, AudioClip> clipDictionary;
        private AudioSource backgroundMusicSource;
        private readonly Queue<AudioSource> availableSources = new Queue<AudioSource>();
        private readonly List<AudioSource> inUseSources = new List<AudioSource>();
        private readonly Dictionary<AudioBus, AudioMixerGroup> mixerGroups = new Dictionary<AudioBus, AudioMixerGroup>();
        private readonly Dictionary<AudioSource, Transform> followTargets = new Dictionary<AudioSource, Transform>();
        private Coroutine musicFadeRoutine;
        private Coroutine temporaryMusicRoutine;
        private bool isTemporaryMusicActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            AudioManager existing = ProjectBootstrap.FindFirst<AudioManager>();
            if (existing != null)
            {
                Debug.Log("[AudioManager] Already exists in scene.");
                return;
            }

            try
            {
                GameObject go = new GameObject("AudioManager");
                AudioManager manager = go.AddComponent<AudioManager>();
                Debug.Log("[AudioManager] Created new instance at runtime.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AudioManager] Failed to create: {ex.Message}");
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeClipDictionary();
                CacheMixerGroups();
                SetupBackgroundMusicSource();
                BuildSfxPool();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            AudioEvents.OneShotRequested += HandleOneShotRequested;
        }

        private void OnDisable()
        {
            AudioEvents.OneShotRequested -= HandleOneShotRequested;
        }

        private void Start()
        {
            if (playBackgroundMusicOnStart && backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
            {
                PlayBackgroundMusic();
            }
        }

        private void InitializeClipDictionary()
        {
            clipDictionary = new Dictionary<AudioClipType, AudioClip>
            {
                { AudioClipType.BackgroundMusic, backgroundMusic },
                { AudioClipType.MicrowaveDing, microwaveDing },
                { AudioClipType.WalkingSteps, walkingSteps },
                { AudioClipType.AirConditioner, airConditioner },
                { AudioClipType.FluorescentLights, fluorescentLights },
                { AudioClipType.KeyboardTyping, keyboardTyping },
                { AudioClipType.TitleScreenMusic, titleScreenMusic },
                { AudioClipType.RideOfTheValkyries, rideOfTheValkyries }
            };
        }

        private void CacheMixerGroups()
        {
            mixerGroups[AudioBus.Master] = null;
            mixerGroups[AudioBus.Music] = musicMixerGroup;
            mixerGroups[AudioBus.Sfx] = sfxMixerGroup;
            mixerGroups[AudioBus.Ambience] = ambienceMixerGroup;
            mixerGroups[AudioBus.Ui] = uiMixerGroup;
        }

        private void SetupBackgroundMusicSource()
        {
            backgroundMusicSource = GetComponent<AudioSource>();
            if (backgroundMusicSource == null)
            {
                backgroundMusicSource = gameObject.AddComponent<AudioSource>();
            }

            backgroundMusicSource.loop = true;
            backgroundMusicSource.spatialBlend = 0f;
            backgroundMusicSource.volume = musicVolume;
            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.outputAudioMixerGroup = musicMixerGroup;
        }

        private void BuildSfxPool()
        {
            for (int i = 0; i < pooledSfxSources; i++)
            {
                AudioSource source = CreatePooledSource(i);
                source.gameObject.SetActive(false);
                availableSources.Enqueue(source);
            }
        }

        private AudioSource CreatePooledSource(int index)
        {
            GameObject go = new GameObject($"PooledAudioSource_{index}");
            go.transform.SetParent(transform);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.4f;
            source.maxDistance = 8f;
            return source;
        }

        private void HandleOneShotRequested(AudioPlaybackRequest request)
        {
            AudioClip clip = GetClip(request.ClipType);
            if (clip == null)
            {
                return;
            }

            float finalVolume = request.Volume * masterSfxVolume;
            AudioSource source = AcquireSource();
            source.transform.position = request.Position;
            source.spatialBlend = request.SpatialBlend;
            source.pitch = request.Pitch;
            source.volume = finalVolume;
            source.outputAudioMixerGroup = ResolveMixerGroup(request.Bus);
            source.clip = clip;
            source.gameObject.SetActive(true);
            source.PlayOneShot(clip, finalVolume);

            if (request.FollowTarget && request.Target != null)
            {
                followTargets[source] = request.Target;
            }

            StartCoroutine(ReleaseSourceAfterPlayback(source, clip.length / Mathf.Max(0.01f, source.pitch)));
        }

        private AudioSource AcquireSource()
        {
            AudioSource source = availableSources.Count > 0
                ? availableSources.Dequeue()
                : CreatePooledSource(pooledSfxSources + inUseSources.Count);

            inUseSources.Add(source);
            return source;
        }

        private System.Collections.IEnumerator ReleaseSourceAfterPlayback(AudioSource source, float waitTime)
        {
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                elapsed += Time.deltaTime;

                if (followTargets.TryGetValue(source, out Transform target) && target != null)
                {
                    source.transform.position = target.position;
                }

                yield return null;
            }

            followTargets.Remove(source);
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);

            inUseSources.Remove(source);
            availableSources.Enqueue(source);
        }

        private AudioMixerGroup ResolveMixerGroup(AudioBus bus)
        {
            if (mixerGroups.TryGetValue(bus, out AudioMixerGroup group))
            {
                return group;
            }

            return null;
        }

        /// <summary>
        /// Gets an audio clip by type.
        /// </summary>
        public static AudioClip GetClip(AudioClipType clipType)
        {
            if (instance == null)
            {
                Debug.LogWarning("AudioManager instance not found!");
                return null;
            }

            if (instance.clipDictionary.TryGetValue(clipType, out AudioClip clip))
            {
                if (clip == null)
                {
                    Debug.LogWarning($"Audio clip of type {clipType} is not assigned in AudioManager!");
                }
                return clip;
            }

            Debug.LogWarning($"Audio clip type {clipType} not found in dictionary!");
            return null;
        }

        /// <summary>
        /// Plays background music, stopping any currently playing music.
        /// </summary>
        public static void PlayBackgroundMusic()
        {
            if (instance == null || instance.backgroundMusicSource == null)
            {
                Debug.LogWarning("Cannot play background music - AudioManager not properly initialized!");
                return;
            }

            if (instance.temporaryMusicRoutine != null)
            {
                instance.StopCoroutine(instance.temporaryMusicRoutine);
                instance.temporaryMusicRoutine = null;
            }
            instance.isTemporaryMusicActive = false;

            AudioClip clip = GetClip(AudioClipType.BackgroundMusic);
            if (clip == null)
            {
                return;
            }

            instance.backgroundMusicSource.clip = clip;
            instance.backgroundMusicSource.time = 0f;
            instance.backgroundMusicSource.volume = instance.musicVolume;
            instance.backgroundMusicSource.Play();
        }

        public static void PlayBackgroundMusicFromTime(float startTimeSeconds, float fadeInSeconds = 0.6f)
        {
            if (instance == null || instance.backgroundMusicSource == null)
            {
                Debug.LogWarning("Cannot play background music from time - AudioManager not initialized.");
                return;
            }

            AudioClip clip = GetClip(AudioClipType.RideOfTheValkyries);
            if (clip == null)
            {
                return;
            }

            float clampedTime = Mathf.Clamp(startTimeSeconds, 0f, Mathf.Max(0f, clip.length - 0.05f));
            instance.backgroundMusicSource.Stop();
            instance.backgroundMusicSource.clip = clip;
            instance.backgroundMusicSource.loop = true;
            instance.backgroundMusicSource.time = clampedTime;

            if (instance.musicFadeRoutine != null)
            {
                instance.StopCoroutine(instance.musicFadeRoutine);
            }

            instance.musicFadeRoutine = instance.StartCoroutine(instance.FadeInMusicRoutine(fadeInSeconds));
        }

        public static void PlayTemporaryMusicFromTime(
            AudioClipType temporaryClipType,
            float startTimeSeconds,
            float fadeInSeconds,
            float holdDurationSeconds,
            AudioClipType returnClipType = AudioClipType.BackgroundMusic,
            float returnFadeSeconds = 0.6f)
        {
            if (instance == null || instance.backgroundMusicSource == null)
            {
                Debug.LogWarning("Cannot play temporary music - AudioManager not initialized.");
                return;
            }

            AudioClip temporaryClip = GetClip(temporaryClipType);
            AudioClip returnClip = GetClip(returnClipType);
            if (temporaryClip == null || returnClip == null)
            {
                return;
            }

            if (instance.temporaryMusicRoutine != null)
            {
                instance.StopCoroutine(instance.temporaryMusicRoutine);
            }

            instance.isTemporaryMusicActive = true;
            instance.temporaryMusicRoutine = instance.StartCoroutine(instance.PlayTemporaryMusicRoutine(
                temporaryClip,
                returnClip,
                startTimeSeconds,
                Mathf.Max(0.05f, fadeInSeconds),
                Mathf.Max(0.05f, holdDurationSeconds),
                Mathf.Max(0.05f, returnFadeSeconds)));
        }

        /// <summary>
        /// Stops background music.
        /// </summary>
        public static void StopBackgroundMusic()
        {
            if (instance != null && instance.backgroundMusicSource != null)
            {
                instance.backgroundMusicSource.Stop();
            }
        }

        public static void PauseBackgroundMusic()
        {
            if (instance == null || instance.backgroundMusicSource == null)
            {
                return;
            }

            if (instance.backgroundMusicSource.isPlaying)
            {
                instance.backgroundMusicSource.Pause();
            }
        }

        public static void ResumeBackgroundMusic()
        {
            if (instance == null || instance.backgroundMusicSource == null)
            {
                return;
            }

            if (instance.backgroundMusicSource.clip == null)
            {
                PlayBackgroundMusic();
                return;
            }

            instance.backgroundMusicSource.UnPause();
            if (!instance.backgroundMusicSource.isPlaying)
            {
                instance.backgroundMusicSource.Play();
            }
        }

        /// <summary>
        /// Plays a one-shot sound effect at a specific position.
        /// </summary>
        public static void PlaySoundAtPosition(AudioClipType clipType, Vector3 position, float volume = 1f, float spatialBlend = 1f)
        {
            AudioEvents.RequestOneShot(clipType, position, volume, spatialBlend, AudioBus.Sfx);
        }

        /// <summary>
        /// Gets the instance of AudioManager.
        /// </summary>
        public static AudioManager GetInstance()
        {
            return instance;
        }

        public static float MusicVolume => instance != null ? instance.musicVolume : 0f;
        public static float SfxVolume => instance != null ? instance.masterSfxVolume : 0f;

        /// <summary>
        /// Sets the background music volume (0..1) and applies it to the
        /// currently playing source. Used by the settings UI.
        /// </summary>
        public static void SetMusicVolume(float volume01)
        {
            if (instance == null) return;
            instance.musicVolume = Mathf.Clamp01(volume01);
            if (instance.backgroundMusicSource != null)
            {
                instance.backgroundMusicSource.volume = instance.musicVolume;
            }
        }

        /// <summary>
        /// Sets the master SFX volume (0..1). New one-shots use it; running
        /// pooled sources keep their per-clip volumes.
        /// </summary>
        public static void SetSfxVolume(float volume01)
        {
            if (instance == null) return;
            instance.masterSfxVolume = Mathf.Clamp01(volume01);
        }

        public static bool IsTemporaryMusicActive()
        {
            return instance != null && instance.isTemporaryMusicActive;
        }

        private System.Collections.IEnumerator FadeInMusicRoutine(float fadeInSeconds)
        {
            backgroundMusicSource.volume = 0f;
            backgroundMusicSource.Play();

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, fadeInSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                backgroundMusicSource.volume = Mathf.Lerp(0f, musicVolume, t);
                yield return null;
            }

            backgroundMusicSource.volume = musicVolume;
            musicFadeRoutine = null;
        }

        private System.Collections.IEnumerator PlayTemporaryMusicRoutine(
            AudioClip temporaryClip,
            AudioClip returnClip,
            float temporaryStartTimeSeconds,
            float fadeInSeconds,
            float holdDurationSeconds,
            float returnFadeSeconds)
        {
            if (musicFadeRoutine != null)
            {
                StopCoroutine(musicFadeRoutine);
                musicFadeRoutine = null;
            }

            float clampedStart = Mathf.Clamp(temporaryStartTimeSeconds, 0f, Mathf.Max(0f, temporaryClip.length - 0.05f));
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = temporaryClip;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.time = clampedStart;
            backgroundMusicSource.volume = 0f;
            backgroundMusicSource.Play();

            float elapsedFadeIn = 0f;
            while (elapsedFadeIn < fadeInSeconds)
            {
                elapsedFadeIn += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedFadeIn / fadeInSeconds);
                backgroundMusicSource.volume = Mathf.Lerp(0f, musicVolume, t);
                yield return null;
            }

            backgroundMusicSource.volume = musicVolume;
            yield return new WaitForSeconds(holdDurationSeconds);

            float elapsedFadeOut = 0f;
            float startVolume = backgroundMusicSource.volume;
            while (elapsedFadeOut < returnFadeSeconds)
            {
                elapsedFadeOut += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedFadeOut / returnFadeSeconds);
                backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = returnClip;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.time = 0f;
            backgroundMusicSource.volume = 0f;
            backgroundMusicSource.Play();

            float elapsedReturnFade = 0f;
            while (elapsedReturnFade < returnFadeSeconds)
            {
                elapsedReturnFade += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedReturnFade / returnFadeSeconds);
                backgroundMusicSource.volume = Mathf.Lerp(0f, musicVolume, t);
                yield return null;
            }

            backgroundMusicSource.volume = musicVolume;
            isTemporaryMusicActive = false;
            temporaryMusicRoutine = null;
        }
    }
}
