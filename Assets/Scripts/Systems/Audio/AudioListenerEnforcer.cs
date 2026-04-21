using UnityEngine;
using UnityEngine.SceneManagement;

namespace OfficeFlipOut.Systems
{
    /// <summary>
    /// Keeps exactly one enabled AudioListener at runtime to avoid Unity warnings
    /// and unstable audio behavior when multiple cameras/listeners exist.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class AudioListenerEnforcer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeEnforcer()
        {
            GameObject existing = GameObject.Find("AudioListenerEnforcer");
            if (existing != null && existing.GetComponent<AudioListenerEnforcer>() != null)
            {
                DontDestroyOnLoad(existing);
                return;
            }

            GameObject go = new GameObject("AudioListenerEnforcer");
            DontDestroyOnLoad(go);
            go.AddComponent<AudioListenerEnforcer>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            EnsureSingleAudioListener();
        }

        private void LateUpdate()
        {
            EnsureSingleAudioListener();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSingleAudioListener();
        }

        private static void EnsureSingleAudioListener()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            if (listeners == null || listeners.Length == 0)
            {
                return;
            }

            AudioListener keep = null;
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                keep = mainCamera.GetComponent<AudioListener>();
            }

            if (keep == null)
            {
                for (int i = 0; i < listeners.Length; i++)
                {
                    if (listeners[i] != null && listeners[i].enabled)
                    {
                        keep = listeners[i];
                        break;
                    }
                }
            }

            if (keep == null)
            {
                keep = listeners[0];
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener == null)
                {
                    continue;
                }

                listener.enabled = listener == keep;
            }
        }
    }
}
