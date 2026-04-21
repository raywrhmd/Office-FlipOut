using System.Collections;
using UnityEngine;

/// <summary>
/// Optional feedback when sabotage lands on this NPC: shake, particles, light camera punch.
/// Attach next to <see cref="Rage_Meter"/> on the NPC root (or child).
/// </summary>
[DisallowMultipleComponent]
public class NpcSabotageJuice : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Rage_Meter rageMeter;
    [SerializeField] private Transform shakeRoot;

    [Header("Shake (per sabotage hit)")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.12f;
    [SerializeField, Min(0f)] private float shakePosStrength = 0.035f;
    [SerializeField, Min(0f)] private float shakeRotStrength = 2.2f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem hitAngerBurst;
    [SerializeField] private ParticleSystem flipOutBurst;

    [Header("Camera")]
    [SerializeField, Min(0f)] private float cameraShakeAmplitude = 0.018f;
    [SerializeField, Min(0f)] private float cameraShakeDuration = 0.09f;

    private Coroutine shakeRoutine;
    private Vector3 shakeLocalOrigin;
    private Quaternion shakeLocalRotationOrigin;

    private void Awake()
    {
        if (rageMeter == null)
        {
            rageMeter = GetComponent<Rage_Meter>();
            if (rageMeter == null)
            {
                rageMeter = GetComponentInParent<Rage_Meter>();
            }
        }

        if (shakeRoot == null)
        {
            shakeRoot = transform;
        }

        shakeLocalOrigin = shakeRoot.localPosition;
        shakeLocalRotationOrigin = shakeRoot.localRotation;
    }

    private void OnEnable()
    {
        if (rageMeter != null)
        {
            rageMeter.SuccessfulSabotageSignal += OnSabotageHitMeter;
            rageMeter.FlippedOut += OnFlipOutMeter;
        }
    }

    private void OnDisable()
    {
        if (rageMeter != null)
        {
            rageMeter.SuccessfulSabotageSignal -= OnSabotageHitMeter;
            rageMeter.FlippedOut -= OnFlipOutMeter;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (shakeRoot != null)
        {
            shakeRoot.localPosition = shakeLocalOrigin;
            shakeRoot.localRotation = shakeLocalRotationOrigin;
        }
    }

    private void OnSabotageHitMeter(Rage_Meter meter)
    {
        if (meter != rageMeter)
        {
            return;
        }

        if (hitAngerBurst != null)
        {
            hitAngerBurst.Play();
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeRoutine());
        TryCameraShake();
    }

    private void OnFlipOutMeter(Rage_Meter meter)
    {
        if (meter != rageMeter)
        {
            return;
        }

        if (flipOutBurst != null)
        {
            flipOutBurst.Play();
        }
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, shakeDuration));
            float wobble = t * t;

            float ox = (Mathf.PerlinNoise(Time.time * 44f, 0.3f) - 0.5f) * 2f;
            float oy = (Mathf.PerlinNoise(0.7f, Time.time * 44f) - 0.5f) * 2f;
            float oz = (Mathf.PerlinNoise(Time.time * 41f, 2.1f) - 0.5f) * 2f;
            shakeRoot.localPosition = shakeLocalOrigin + new Vector3(ox, oy, oz) * (shakePosStrength * wobble);

            float rx = (Mathf.PerlinNoise(5.2f, Time.time * 38f) - 0.5f) * 2f;
            float ry = (Mathf.PerlinNoise(Time.time * 38f, 8.1f) - 0.5f) * 2f;
            float rz = (Mathf.PerlinNoise(1.2f, Time.time * 36f) - 0.5f) * 2f;
            shakeRoot.localRotation = shakeLocalRotationOrigin * Quaternion.Euler(new Vector3(rx, ry, rz) * (shakeRotStrength * wobble));

            yield return null;
        }

        shakeRoot.localPosition = shakeLocalOrigin;
        shakeRoot.localRotation = shakeLocalRotationOrigin;
        shakeRoutine = null;
    }

    private void TryCameraShake()
    {
        if (cameraShakeDuration <= 0f || cameraShakeAmplitude <= 0f)
        {
            return;
        }

        Camera main = Camera.main;
        if (main == null)
        {
            return;
        }

        StartCoroutine(QuickCameraKick(main.transform));
    }

    private IEnumerator QuickCameraKick(Transform cam)
    {
        Vector3 baseLocal = cam.localPosition;
        float elapsed = 0f;
        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, cameraShakeDuration));
            float amp = cameraShakeAmplitude * t;
            float nx = (Mathf.PerlinNoise(Time.time * 60f, 0f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(0f, Time.time * 60f) - 0.5f) * 2f;
            cam.localPosition = baseLocal + new Vector3(nx, ny, 0f) * amp;
            yield return null;
        }

        cam.localPosition = baseLocal;
    }
}
