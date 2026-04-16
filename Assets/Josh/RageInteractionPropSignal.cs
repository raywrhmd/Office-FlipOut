using UnityEngine;
using UnityEngine.Events;

public enum RageSignalEventType
{
    SpillDrinkOnDesk = 0,
    MicrowaveFish = 1,
    TakeStaplerFromDesk = 2
}

public static class RageSignalIds
{
    public const string SpillDrinkOnDesk = "spill_drink_on_desk";
    public const string MicrowaveFish = "microwave_fish";
    public const string TakeStaplerFromDesk = "take_stapler_from_desk";
}

[DisallowMultipleComponent]
public class RageInteractionPropSignal : MonoBehaviour
{
    [Header("Signal")]
    [SerializeField] private RageSignalEventType signalEventType = RageSignalEventType.SpillDrinkOnDesk;

    [Header("Target")]
    [Tooltip("Optional: direct NPC target. If set, signal is sent directly to this Rage_Meter.")]
    [SerializeField] private Rage_Meter targetRageMeter;
    [Tooltip("Optional: drag an NPC Rage_Meter here to auto-use its NPC signal id when broadcasting.")]
    [SerializeField] private Rage_Meter targetNpcForSignalId;
    [Tooltip("Used only when no direct target is assigned and no NPC reference is set.")]
    [SerializeField] private string targetNpcId;

    [Header("Activation")]
    [SerializeField] private bool sendOnTriggerEnter = true;
    [Tooltip("Optional: leave empty to allow any collider.")]
    [SerializeField] private string requiredTag;
    [SerializeField] private bool sendOnDistanceFromOrigin;
    [SerializeField, Min(0.01f)] private float requiredDistanceFromOrigin = 1.5f;
    [SerializeField] private bool horizontalDistanceOnly;
    [SerializeField] private bool distanceTriggerOnlyOnce = true;

    [Header("Limits")]
    [SerializeField, Min(0f)] private float sendCooldown = 0.1f;
    [SerializeField] private bool sendOnlyOnce;

    [Header("Events")]
    [SerializeField] private UnityEvent onSignalSent;

    [Header("Debug")]
    [SerializeField] private bool logSignalSendToConsole = true;

    public RageSignalEventType SignalEventType => signalEventType;

    private bool hasSent;
    private bool hasTriggeredDistanceSignal;
    private float lastSendTime = -999f;
    private Vector3 originPosition;

    private void Awake()
    {
        originPosition = transform.position;
    }

    private void Update()
    {
        if (!sendOnDistanceFromOrigin)
        {
            return;
        }

        if (distanceTriggerOnlyOnce && hasTriggeredDistanceSignal)
        {
            return;
        }

        Vector3 displacement = transform.position - originPosition;
        if (horizontalDistanceOnly)
        {
            displacement.y = 0f;
        }

        float requiredDistanceSquared = requiredDistanceFromOrigin * requiredDistanceFromOrigin;
        if (displacement.sqrMagnitude < requiredDistanceSquared)
        {
            return;
        }

        hasTriggeredDistanceSignal = true;
        TrySendSignal("distance-from-origin threshold exceeded");
    }

    public void Interact()
    {
        TrySendSignal("Interact() called");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!sendOnTriggerEnter)
        {
            return;
        }

        if (!PassesTagFilter(other.gameObject))
        {
            return;
        }

        TrySendSignal("trigger enter by " + other.gameObject.name);
    }

    [ContextMenu("Debug/Send Rage Signal")]
    private void DebugSendSignal()
    {
        TrySendSignal("debug context menu");
    }

    public void SendSpillDrinkSignal()
    {
        TrySendSpecificSignal(RageSignalIds.SpillDrinkOnDesk, "SendSpillDrinkSignal() called");
    }

    public void SendMicrowaveFishSignal()
    {
        TrySendSpecificSignal(RageSignalIds.MicrowaveFish, "SendMicrowaveFishSignal() called");
    }

    public void SendTakeStaplerSignal()
    {
        TrySendSpecificSignal(RageSignalIds.TakeStaplerFromDesk, "SendTakeStaplerSignal() called");
    }

    private bool PassesTagFilter(GameObject other)
    {
        if (string.IsNullOrWhiteSpace(requiredTag))
        {
            return true;
        }

        return other.CompareTag(requiredTag);
    }

    private void TrySendSignal()
    {
        TrySendSpecificSignal(ResolveSignalId(), "TrySendSignal() default path");
    }

    private void TrySendSignal(string sendReason)
    {
        TrySendSpecificSignal(ResolveSignalId(), sendReason);
    }

    private void TrySendSpecificSignal(string resolvedSignalId)
    {
        TrySendSpecificSignal(resolvedSignalId, "TrySendSpecificSignal() default path");
    }

    private void TrySendSpecificSignal(string resolvedSignalId, string sendReason)
    {
        if (sendOnlyOnce && hasSent)
        {
            return;
        }

        if (Time.time < lastSendTime + sendCooldown)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(resolvedSignalId))
        {
            return;
        }

        if (targetRageMeter == null && targetNpcForSignalId != null && string.IsNullOrWhiteSpace(targetNpcForSignalId.NpcSignalId))
        {
            if (logSignalSendToConsole)
            {
                Debug.LogWarning(
                    "[RageInteractionPropSignal] " + name +
                    " did not send signal '" + resolvedSignalId +
                    "' because targetNpcForSignalId is set but its NpcSignalId is empty.",
                    this);
            }

            return;
        }

        string targetInfo;

        if (targetRageMeter != null)
        {
            targetRageMeter.ReceiveSignal(resolvedSignalId);
            targetInfo = "direct Rage_Meter: " + targetRageMeter.name;
        }
        else
        {
            string resolvedTargetNpcId = ResolveTargetNpcId();
            RageSignalHub.RaiseSignal(resolvedSignalId, resolvedTargetNpcId);
            targetInfo = string.IsNullOrWhiteSpace(resolvedTargetNpcId)
                ? "global broadcast"
                : "target npc id: " + resolvedTargetNpcId;
        }

        hasSent = true;
        lastSendTime = Time.time;
        onSignalSent?.Invoke();

        if (logSignalSendToConsole)
        {
            Debug.Log(
                "[RageInteractionPropSignal] " + name +
                " sent signal '" + resolvedSignalId +
                "' because " + sendReason +
                " (" + targetInfo + ").",
                this);
        }
    }

    private string ResolveSignalId()
    {
        switch (signalEventType)
        {
            case RageSignalEventType.SpillDrinkOnDesk:
                return RageSignalIds.SpillDrinkOnDesk;
            case RageSignalEventType.MicrowaveFish:
                return RageSignalIds.MicrowaveFish;
            case RageSignalEventType.TakeStaplerFromDesk:
                return RageSignalIds.TakeStaplerFromDesk;
            default:
                return RageSignalIds.SpillDrinkOnDesk;
        }
    }

    private string ResolveTargetNpcId()
    {
        if (targetNpcForSignalId != null && !string.IsNullOrWhiteSpace(targetNpcForSignalId.NpcSignalId))
        {
            return targetNpcForSignalId.NpcSignalId;
        }

        return targetNpcId;
    }

    [ContextMenu("Debug/Set Current Position As Origin")]
    private void DebugSetCurrentPositionAsOrigin()
    {
        originPosition = transform.position;
        hasTriggeredDistanceSignal = false;
    }
}
