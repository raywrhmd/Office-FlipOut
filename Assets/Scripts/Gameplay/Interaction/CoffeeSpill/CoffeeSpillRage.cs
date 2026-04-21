using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class CoffeeSpillRage : MonoBehaviour
{
    private Rage_Meter targetRageMeter;
    private string targetNpcId;
    private string signalId;
    private float spillRadius;
    private float minImpactSpeed;
    private bool logDebug;

    private bool isArmed;
    private bool hasTriggered;
    private float armedAtTime;

    public void Arm(
        Rage_Meter targetMeter,
        string npcId,
        float npcRadius,
        float requiredImpactSpeed,
        string resolvedSignalId,
        bool enableDebugLogging)
    {
        targetRageMeter = targetMeter;
        targetNpcId = npcId;
        spillRadius = Mathf.Max(0.1f, npcRadius);
        minImpactSpeed = Mathf.Max(0f, requiredImpactSpeed);
        signalId = string.IsNullOrWhiteSpace(resolvedSignalId) ? RageSignalIds.SpillDrinkOnDesk : resolvedSignalId;
        logDebug = enableDebugLogging;

        isArmed = true;
        hasTriggered = false;
        armedAtTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isArmed || hasTriggered || targetRageMeter == null)
        {
            return;
        }

        if (Time.time < armedAtTime + 0.05f)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < minImpactSpeed)
        {
            return;
        }

        float distanceToNpc = Vector3.Distance(transform.position, targetRageMeter.transform.position);
        if (distanceToNpc > spillRadius)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(targetNpcId) && string.IsNullOrWhiteSpace(targetRageMeter.NpcSignalId))
        {
            if (logDebug)
            {
                Debug.LogWarning(
                    "[CoffeeSpillRage] Spill met distance/impact rules but target Rage_Meter has no npcSignalId mapping.",
                    this);
            }

            return;
        }

        hasTriggered = true;

        RageInteractionPropSignal signalSender = GetComponent<RageInteractionPropSignal>();
        if (signalSender != null)
        {
            signalSender.SendSpillDrinkSignal();
        }
        else if (!string.IsNullOrWhiteSpace(targetNpcId))
        {
            RageSignalHub.RaiseSignal(signalId, targetNpcId);
        }
        else
        {
            targetRageMeter.ReceiveSignal(signalId);
        }

        if (logDebug)
        {
            Debug.Log(
                "[CoffeeSpillRage] " + name +
                " triggered spill signal '" + signalId +
                "' near " + targetRageMeter.name +
                " after impact speed " + collision.relativeVelocity.magnitude.ToString("F2") + ".",
                this);
        }
    }
}
