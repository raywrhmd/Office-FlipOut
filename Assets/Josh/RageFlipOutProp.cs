using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class RageFlipOutProp : MonoBehaviour
{
    [Header("Listening")]
    [SerializeField] private bool listenForFlipOutBlast = true;
    [SerializeField, Min(0f)] private float additionalDetectionRadius = 0f;
    [SerializeField, Range(0f, 1f)] private float reactionChance = 1f;
    [SerializeField, Min(0f)] private float reactionCooldown = 0.1f;
    [SerializeField] private bool reactOnlyOnce;

    [Header("Funny Physics")]
    [SerializeField] private bool randomizeDirection = true;
    [SerializeField] private Vector2 extraUpImpulseRange = new Vector2(0.5f, 2f);
    [SerializeField] private Vector2 torqueMultiplierRange = new Vector2(0.7f, 1.6f);

    [Header("Buildup Shake")]
    [SerializeField] private bool useBuildupShake = true;
    [SerializeField, Min(0f)] private float buildupDelay = 0.05f;
    [SerializeField, Min(0.05f)] private float buildupDuration = 0.8f;
    [SerializeField, Min(1f)] private float vibrationBurstsPerSecond = 30f;
    [SerializeField] private Vector2 shakeImpulseRange = new Vector2(0.03f, 0.2f);
    [SerializeField] private Vector2 shakeTorqueRange = new Vector2(0.08f, 0.6f);

    [Header("Optional Overrides")]
    [SerializeField] private Rigidbody targetRigidbody;

    private bool hasReacted;
    private bool isReacting;
    private float lastReactionTime = -999f;
    private Coroutine reactionRoutine;

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }
    }

    private void OnEnable()
    {
        RageSignalHub.FlipOutBlastRaised += HandleFlipOutBlast;
    }

    private void OnDisable()
    {
        RageSignalHub.FlipOutBlastRaised -= HandleFlipOutBlast;

        if (reactionRoutine != null)
        {
            StopCoroutine(reactionRoutine);
            reactionRoutine = null;
            isReacting = false;
        }
    }

    private void HandleFlipOutBlast(RageFlipOutBlastData blastData)
    {
        if (!listenForFlipOutBlast || targetRigidbody == null)
        {
            return;
        }

        if (reactOnlyOnce && hasReacted)
        {
            return;
        }

        if (isReacting)
        {
            return;
        }

        if (Time.time < lastReactionTime + reactionCooldown)
        {
            return;
        }

        if (Random.value > reactionChance)
        {
            return;
        }

        float triggerRadius = blastData.Radius + additionalDetectionRadius;
        float distance = Vector3.Distance(transform.position, blastData.Origin);
        if (distance > triggerRadius)
        {
            return;
        }

        hasReacted = true;
        lastReactionTime = Time.time;

        Vector3 direction = transform.position - blastData.Origin;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Random.onUnitSphere;
        }

        direction.Normalize();
        if (randomizeDirection)
        {
            direction = (direction + (Random.insideUnitSphere * 0.5f)).normalized;
        }

        float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, triggerRadius));

        if (!useBuildupShake)
        {
            ApplyLaunch(blastData, direction, falloff);
            return;
        }

        reactionRoutine = StartCoroutine(BuildupThenLaunch(blastData, direction, falloff));
    }

    private IEnumerator BuildupThenLaunch(RageFlipOutBlastData blastData, Vector3 launchDirection, float falloff)
    {
        isReacting = true;
        targetRigidbody.WakeUp();

        if (buildupDelay > 0f)
        {
            yield return new WaitForSeconds(buildupDelay);
        }

        float burstInterval = 1f / Mathf.Max(1f, vibrationBurstsPerSecond);
        float elapsed = 0f;

        while (elapsed < buildupDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, buildupDuration));
            float shakeImpulse = Mathf.Lerp(shakeImpulseRange.x, shakeImpulseRange.y, t) * Mathf.Max(0.1f, falloff);
            float shakeTorque = Mathf.Lerp(shakeTorqueRange.x, shakeTorqueRange.y, t);

            Vector3 jitterDirection = Random.insideUnitSphere;
            if (jitterDirection.sqrMagnitude < 0.001f)
            {
                jitterDirection = Vector3.up;
            }

            targetRigidbody.AddForce(jitterDirection.normalized * shakeImpulse, ForceMode.Impulse);
            targetRigidbody.AddTorque(Random.onUnitSphere * shakeTorque, ForceMode.Impulse);

            elapsed += burstInterval;
            yield return new WaitForSeconds(burstInterval);
        }

        ApplyLaunch(blastData, launchDirection, falloff);

        reactionRoutine = null;
        isReacting = false;
    }

    private void ApplyLaunch(RageFlipOutBlastData blastData, Vector3 direction, float falloff)
    {
        Vector3 radialForce = direction * (blastData.Force * falloff);
        targetRigidbody.AddForce(radialForce, blastData.ForceMode);

        float upImpulse = Random.Range(extraUpImpulseRange.x, extraUpImpulseRange.y) * blastData.UpwardsModifier * falloff;
        targetRigidbody.AddForce(Vector3.up * upImpulse, ForceMode.Impulse);

        float torqueMultiplier = Random.Range(torqueMultiplierRange.x, torqueMultiplierRange.y);
        Vector3 randomTorque = Random.onUnitSphere * blastData.RandomTorque * torqueMultiplier;
        targetRigidbody.AddTorque(randomTorque, ForceMode.Impulse);
    }

    [ContextMenu("Debug/Test Prop Flip Out")]
    private void DebugTestPropFlipOut()
    {
        RageSignalHub.RaiseFlipOutBlast(new RageFlipOutBlastData(
            transform.position + new Vector3(0f, 0f, -1.5f),
            4f,
            10f,
            1f,
            8f,
            ForceMode.Impulse,
            gameObject));
    }
}
