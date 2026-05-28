using System.Collections;
using OfficeFlipOut.Systems;
using UnityEngine;

[DisallowMultipleComponent]
public class MicrowaveFishJuiceAnimator : MonoBehaviour
{
    [Header("Insert Motion")]
    [SerializeField] private float insertDuration = 0.2f;
    [SerializeField] private float overshootDistance = 0.12f;
    [SerializeField] private float squashAmount = 0.15f;
    [SerializeField] private float lookAtCameraWeight = 0.25f;

    [Header("Microwave Spin")]
    [SerializeField] private float spinDuration = 0.9f;
    [SerializeField] private float spinDegreesPerSecond = 720f;
    [SerializeField] private float spinWobbleAngle = 10f;
    [SerializeField] private Vector3 spinAxis = Vector3.up;

    [Header("SFX Settings")]
    [SerializeField, Range(0f, 1f)] private float microwaveSfxVolume = 0.65f;

    private Coroutine activeRoutine;
    private Vector3 baseScale = Vector3.one;
    private bool hasPlayed;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public bool TryPlayInsert(Vector3 targetLocalPosition, Quaternion targetLocalRotation, Transform cameraTransform)
    {
        if (hasPlayed)
        {
            return false;
        }

        if (!isActiveAndEnabled)
        {
            return false;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(
            PlayInsertRoutine(targetLocalPosition, targetLocalRotation, cameraTransform));
        return true;
    }

    private IEnumerator PlayInsertRoutine(Vector3 targetLocalPosition, Quaternion targetLocalRotation, Transform cameraTransform)
    {
        hasPlayed = true;
        Vector3 startLocalPosition = transform.localPosition;
        Quaternion startLocalRotation = transform.localRotation;

        Quaternion finalRotation = targetLocalRotation;
        if (cameraTransform != null)
        {
            Vector3 toCamera = cameraTransform.position - transform.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                Quaternion towardCamera = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
                finalRotation = Quaternion.Slerp(targetLocalRotation, towardCamera, Mathf.Clamp01(lookAtCameraWeight));
            }
        }

        Vector3 insertDirection = (targetLocalPosition - startLocalPosition).sqrMagnitude > 0.0001f
            ? (targetLocalPosition - startLocalPosition).normalized
            : Vector3.forward;
        Vector3 overshootPosition = targetLocalPosition + insertDirection * overshootDistance;

        float halfDuration = Mathf.Max(0.02f, insertDuration * 0.5f);
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / halfDuration);
            float eased = Easing.EaseOutCubic(progress);
            transform.localPosition = Vector3.Lerp(startLocalPosition, overshootPosition, eased);
            transform.localRotation = Quaternion.Slerp(startLocalRotation, finalRotation, eased);

            float stretch = Mathf.Lerp(1f, 1f + squashAmount, eased);
            float squash = Mathf.Lerp(1f, 1f - squashAmount, eased);
            transform.localScale = Vector3.Scale(baseScale, new Vector3(squash, stretch, squash));
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / halfDuration);
            float eased = Easing.SmoothStep01(progress);
            transform.localPosition = Vector3.Lerp(overshootPosition, targetLocalPosition, eased);
            transform.localRotation = Quaternion.Slerp(finalRotation, targetLocalRotation, eased);
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, eased);
            yield return null;
        }

        transform.localPosition = targetLocalPosition;
        transform.localRotation = targetLocalRotation;
        transform.localScale = baseScale;

        PlayMicrowaveSfx();
        yield return SpinRoutine(targetLocalRotation);

        activeRoutine = null;
    }

    private IEnumerator SpinRoutine(Quaternion baseRotation)
    {
        float duration = Mathf.Max(0f, spinDuration);
        if (duration <= 0f || spinDegreesPerSecond <= 0f)
        {
            yield break;
        }

        Vector3 axis = spinAxis.sqrMagnitude > 0.0001f ? spinAxis.normalized : Vector3.up;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float damping = 1f - t;

            float spinAngle = elapsed * spinDegreesPerSecond;
            float wobble = Mathf.Sin(elapsed * 18f) * spinWobbleAngle * damping;
            Quaternion wobbleRotation = Quaternion.AngleAxis(wobble, Vector3.right);
            Quaternion spinRotation = Quaternion.AngleAxis(spinAngle, axis);

            transform.localRotation = baseRotation * spinRotation * wobbleRotation;
            yield return null;
        }

        transform.localRotation = baseRotation;
    }

    private void PlayMicrowaveSfx()
    {
        AudioEvents.RequestAttachedOneShot(
            AudioManager.AudioClipType.MicrowaveDing,
            transform,
            microwaveSfxVolume,
            spatialBlend: 1f,
            bus: AudioBus.Sfx);
    }
}
