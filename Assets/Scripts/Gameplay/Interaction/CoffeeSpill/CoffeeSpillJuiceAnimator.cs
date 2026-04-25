using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CoffeeSpillJuiceAnimator : MonoBehaviour
{
    [Header("Spill Motion")]
    [SerializeField, Min(0.01f)] private float tipDuration = 0.16f;
    [SerializeField, Min(0f)] private float splashHoldDuration = 0.05f;
    [SerializeField, Min(0.01f)] private float settleDuration = 0.28f;
    [SerializeField] private Vector3 splashOvershootEuler = new Vector3(0f, 0f, 18f);

    [Header("Optional Visuals")]
    [SerializeField] private GameObject splashVisual;
    [SerializeField] private GameObject stainVisual;

    [Header("Optional SFX")]
    [SerializeField] private AudioSource spillAudioSource;
    [SerializeField] private AudioClip spillClip;
    [SerializeField, Range(0f, 1f)] private float spillClipVolume = 0.9f;

    [Header("Camera Juice")]
    [SerializeField, Min(0f)] private float cameraShakeAmplitude = 0.028f;
    [SerializeField, Min(0f)] private float cameraShakeDuration = 0.12f;
    [SerializeField, Min(1f)] private float cameraShakeFrequency = 34f;

    private Coroutine spillRoutine;

    public bool TryPlaySpill(Vector3 targetLocalPosition, Quaternion targetLocalRotation, Transform cameraTransform)
    {
        if (spillRoutine != null)
        {
            StopCoroutine(spillRoutine);
            spillRoutine = null;
        }

        spillRoutine = StartCoroutine(PlaySpillRoutine(targetLocalPosition, targetLocalRotation, cameraTransform));
        return true;
    }

    private IEnumerator PlaySpillRoutine(Vector3 targetLocalPosition, Quaternion targetLocalRotation, Transform cameraTransform)
    {
        Quaternion startLocalRotation = transform.localRotation;
        transform.localPosition = targetLocalPosition;

        if (splashVisual != null)
        {
            splashVisual.SetActive(true);
        }

        PlaySpillSfx();

        if (cameraTransform == null)
        {
            Camera main = Camera.main;
            cameraTransform = main != null ? main.transform : null;
        }

        if (cameraTransform != null && cameraShakeDuration > 0f && cameraShakeAmplitude > 0f)
        {
            StartCoroutine(ShakeCameraRoutine(cameraTransform));
        }

        Quaternion overshootRotation = targetLocalRotation * Quaternion.Euler(splashOvershootEuler);

        float elapsed = 0f;
        while (elapsed < tipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, tipDuration));
            float eased = EaseOutCubic(t);
            transform.localRotation = Quaternion.Slerp(startLocalRotation, overshootRotation, eased);
            yield return null;
        }

        transform.localRotation = overshootRotation;

        if (splashHoldDuration > 0f)
        {
            yield return new WaitForSeconds(splashHoldDuration);
        }

        if (stainVisual != null)
        {
            stainVisual.SetActive(true);
        }

        elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, settleDuration));
            float eased = EaseOutBack(t);
            transform.localRotation = Quaternion.Slerp(overshootRotation, targetLocalRotation, eased);
            yield return null;
        }

        transform.localRotation = targetLocalRotation;

        if (splashVisual != null)
        {
            splashVisual.SetActive(false);
        }

        spillRoutine = null;
    }

    private void PlaySpillSfx()
    {
        if (spillClip == null)
        {
            return;
        }

        if (spillAudioSource == null)
        {
            spillAudioSource = GetComponent<AudioSource>();
        }

        if (spillAudioSource == null)
        {
            spillAudioSource = gameObject.AddComponent<AudioSource>();
            spillAudioSource.playOnAwake = false;
            spillAudioSource.spatialBlend = 1f;
            spillAudioSource.minDistance = 0.4f;
            spillAudioSource.maxDistance = 7f;
            spillAudioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        spillAudioSource.PlayOneShot(spillClip, spillClipVolume);
    }

    private IEnumerator ShakeCameraRoutine(Transform cameraTransform)
    {
        bool useLocalSpace = cameraTransform.parent != null;
        Vector3 basePosition = useLocalSpace ? cameraTransform.localPosition : cameraTransform.position;

        float elapsed = 0f;
        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, cameraShakeDuration));
            float damping = 1f - normalized;
            float timeFactor = Time.time * cameraShakeFrequency;

            float x = (Mathf.PerlinNoise(timeFactor, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, timeFactor) - 0.5f) * 2f;
            Vector3 offset = new Vector3(x, y, 0f) * cameraShakeAmplitude * damping;

            if (useLocalSpace)
            {
                cameraTransform.localPosition = basePosition + offset;
            }
            else
            {
                cameraTransform.position = basePosition + offset;
            }

            yield return null;
        }

        if (useLocalSpace)
        {
            cameraTransform.localPosition = basePosition;
        }
        else
        {
            cameraTransform.position = basePosition;
        }
    }

    private static float EaseOutCubic(float t)
    {
        float i = 1f - t;
        return 1f - (i * i * i);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + (c3 * x * x * x) + (c1 * x * x);
    }
}
