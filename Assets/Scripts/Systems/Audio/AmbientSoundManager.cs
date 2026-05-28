using UnityEngine;
using OfficeFlipOut.Systems;

namespace OfficeFlipOut.Systems
{
    /// <summary>
    /// Manages ambient sounds that loop continuously in the background.
    /// Includes air conditioning, fluorescent lights, and other environmental audio.
    /// </summary>
    [DefaultExecutionOrder(-99)]
    public class AmbientSoundManager : MonoBehaviour
    {
        [Header("Ambient Audio Sources")]
        [SerializeField] private AudioSource airConditionerSource;
        [SerializeField] private AudioSource fluorescentLightsSource;
        [SerializeField] private AudioSource keyboardTypingSource;

        [Header("Volume Settings - Subtle Background Ambience")]
        [SerializeField, Range(0f, 1f)] private float airConditionerVolume = 0.12f;
        [SerializeField, Range(0f, 1f)] private float fluorescentLightsVolume = 0.08f;
        [SerializeField, Range(0f, 1f)] private float keyboardTypingVolume = 0.06f;

        [Header("Ambient Settings")]
        [SerializeField] private bool playAmbientOnStart = true;

        private static AmbientSoundManager instance;
        private float baseAirConditionerVolume;
        private float baseFluorescentLightsVolume;
        private float baseKeyboardTypingVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (ProjectBootstrap.FindFirst<AmbientSoundManager>() != null)
            {
                return;
            }

            GameObject go = new GameObject("AmbientSoundManager");
            go.AddComponent<AmbientSoundManager>();
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAmbientSources();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (playAmbientOnStart)
            {
                PlayAllAmbient();
            }
        }

        private void InitializeAmbientSources()
        {
            airConditionerSource = SetupAmbientSource(airConditionerSource, "AirConditioner");
            fluorescentLightsSource = SetupAmbientSource(fluorescentLightsSource, "FluorescentLights");
            keyboardTypingSource = SetupAmbientSource(keyboardTypingSource, "KeyboardTyping");
        }

        private AudioSource SetupAmbientSource(AudioSource source, string name)
        {
            if (source == null)
            {
                GameObject go = new GameObject($"AmbientAudio_{name}");
                go.transform.SetParent(transform);
                source = go.AddComponent<AudioSource>();
            }

            source.loop = true;
            source.spatialBlend = 0f; // 2D audio
            source.playOnAwake = false;

            return source;
        }

        /// <summary>
        /// Plays all ambient sounds.
        /// </summary>
        public static void PlayAllAmbient()
        {
            if (instance == null)
            {
                Debug.LogWarning("AmbientSoundManager instance not found!");
                return;
            }

            instance.PlayAmbientSound(instance.airConditionerSource, AudioManager.AudioClipType.AirConditioner, instance.airConditionerVolume);
            instance.PlayAmbientSound(instance.fluorescentLightsSource, AudioManager.AudioClipType.FluorescentLights, instance.fluorescentLightsVolume);
            instance.PlayAmbientSound(instance.keyboardTypingSource, AudioManager.AudioClipType.KeyboardTyping, instance.keyboardTypingVolume);
        }

        /// <summary>
        /// Stops all ambient sounds.
        /// </summary>
        public static void StopAllAmbient()
        {
            if (instance == null)
            {
                Debug.LogWarning("AmbientSoundManager instance not found!");
                return;
            }

            if (instance.airConditionerSource != null)
                instance.airConditionerSource.Stop();
            if (instance.fluorescentLightsSource != null)
                instance.fluorescentLightsSource.Stop();
            if (instance.keyboardTypingSource != null)
                instance.keyboardTypingSource.Stop();
        }

        /// <summary>
        /// Pauses all ambient sounds.
        /// </summary>
        public static void PauseAllAmbient()
        {
            if (instance == null)
            {
                Debug.LogWarning("AmbientSoundManager instance not found!");
                return;
            }

            if (instance.airConditionerSource != null && instance.airConditionerSource.isPlaying)
                instance.airConditionerSource.Pause();
            if (instance.fluorescentLightsSource != null && instance.fluorescentLightsSource.isPlaying)
                instance.fluorescentLightsSource.Pause();
            if (instance.keyboardTypingSource != null && instance.keyboardTypingSource.isPlaying)
                instance.keyboardTypingSource.Pause();
        }

        /// <summary>
        /// Resumes all paused ambient sounds.
        /// </summary>
        public static void ResumeAllAmbient()
        {
            if (instance == null)
            {
                Debug.LogWarning("AmbientSoundManager instance not found!");
                return;
            }

            if (instance.airConditionerSource != null && !instance.airConditionerSource.isPlaying)
                instance.airConditionerSource.Play();
            if (instance.fluorescentLightsSource != null && !instance.fluorescentLightsSource.isPlaying)
                instance.fluorescentLightsSource.Play();
            if (instance.keyboardTypingSource != null && !instance.keyboardTypingSource.isPlaying)
                instance.keyboardTypingSource.Play();
        }

        private void PlayAmbientSound(AudioSource source, AudioManager.AudioClipType clipType, float volume)
        {
            AudioClip clip = AudioManager.GetClip(clipType);
            if (clip == null)
            {
                return;
            }

            source.clip = clip;
            source.volume = volume;
            
            // Store base volumes for ducking
            if (source == airConditionerSource)
                baseAirConditionerVolume = volume;
            else if (source == fluorescentLightsSource)
                baseFluorescentLightsVolume = volume;
            else if (source == keyboardTypingSource)
                baseKeyboardTypingVolume = volume;

            source.Play();
        }

        /// <summary>
        /// Duck (reduce) ambient volumes to a multiplier, with fade time.
        /// Used for professional mixing when SFX should stand out.
        /// </summary>
        public void SetAmbientDuck(float volumeMultiplier, float fadeTime)
        {
            StartCoroutine(DuckCoroutine(volumeMultiplier, fadeTime));
        }

        private System.Collections.IEnumerator DuckCoroutine(float targetMultiplier, float fadeTime)
        {
            float elapsed = 0f;
            Vector3 startVolumes = new Vector3(
                airConditionerSource?.volume ?? baseAirConditionerVolume,
                fluorescentLightsSource?.volume ?? baseFluorescentLightsVolume,
                keyboardTypingSource?.volume ?? baseKeyboardTypingVolume
            );

            Vector3 targetVolumes = new Vector3(
                baseAirConditionerVolume * targetMultiplier,
                baseFluorescentLightsVolume * targetMultiplier,
                baseKeyboardTypingVolume * targetMultiplier
            );

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, fadeTime));

                if (airConditionerSource != null)
                    airConditionerSource.volume = Mathf.Lerp(startVolumes.x, targetVolumes.x, t);
                if (fluorescentLightsSource != null)
                    fluorescentLightsSource.volume = Mathf.Lerp(startVolumes.y, targetVolumes.y, t);
                if (keyboardTypingSource != null)
                    keyboardTypingSource.volume = Mathf.Lerp(startVolumes.z, targetVolumes.z, t);

                yield return null;
            }

            // Ensure final values are exact
            if (airConditionerSource != null)
                airConditionerSource.volume = targetVolumes.x;
            if (fluorescentLightsSource != null)
                fluorescentLightsSource.volume = targetVolumes.y;
            if (keyboardTypingSource != null)
                keyboardTypingSource.volume = targetVolumes.z;
        }

        /// <summary>
        /// Gets the instance of AmbientSoundManager.
        /// </summary>
        public static AmbientSoundManager GetInstance()
        {
            return instance;
        }
    }
}
