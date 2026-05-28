using UnityEngine;
using System.Collections;

namespace OfficeFlipOut.Systems
{
    /// <summary>
    /// Audio mixing utility that implements professional ducking - lowers ambient sounds
    /// when SFX or music need to be prominent. This prevents audio from sounding muddy.
    /// </summary>
    public class AudioDucker : MonoBehaviour
    {
        private static AudioDucker instance;
        private Coroutine duckRoutine;
        private float duckAmount = 0.4f; // Reduce ambient to 40% of original when ducking

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (ProjectBootstrap.FindFirst<AudioDucker>() != null)
            {
                return;
            }

            GameObject go = new GameObject("AudioDucker");
            go.AddComponent<AudioDucker>();
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Temporarily reduce ambient sounds (duck) for duration, then fade back in.
        /// </summary>
        public static void DuckAmbience(float durationSeconds = 0.5f)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.duckRoutine != null)
            {
                instance.StopCoroutine(instance.duckRoutine);
            }

            instance.duckRoutine = instance.StartCoroutine(instance.DuckRoutine(durationSeconds));
        }

        private IEnumerator DuckRoutine(float duration)
        {
            // Reduce ambient volumes
            AmbientSoundManager manager = AmbientSoundManager.GetInstance();
            if (manager != null)
            {
                manager.SetAmbientDuck(duckAmount, 0.1f);
            }

            // Wait for specified duration
            yield return new WaitForSeconds(duration);

            // Restore ambient volumes
            if (manager != null)
            {
                manager.SetAmbientDuck(1.0f, 0.2f);
            }

            duckRoutine = null;
        }

        public static AudioDucker GetInstance() => instance;
    }
}
