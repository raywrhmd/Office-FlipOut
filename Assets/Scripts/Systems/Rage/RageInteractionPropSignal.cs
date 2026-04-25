using UnityEngine;

public enum RageSignalEventType
{
    SpillDrinkOnDesk = 0,
    MicrowaveFish = 1,
    StealObject = 2,
    NoSignal = 3,
    MicrowaveFishItem = 4,
    KnockOver = 5,
    BringObjectNear = 6,
    HitNpcWithProjectile = 7,
    MakeLoudNoise = 8,
    UnplugDevice = 9,
    FinalBossSpillDrinkOnDesk = 10
}

public static class RageSignalIds
{
    public const string SpillDrinkOnDesk = "spill_drink_on_desk";
    public const string MicrowaveFish = "microwave_fish";
    public const string StealObject = "steal_object";
    public const string KnockOver = "knock_over";
    public const string BringObjectNear = "bring_object_near";
    public const string HitNpcWithProjectile = "hit_npc_with_projectile";
    public const string MakeLoudNoise = "make_loud_noise";
    public const string UnplugDevice = "unplug_device";
}

[DisallowMultipleComponent]
public class RageInteractionPropSignal : MonoBehaviour
{
    [Header("Signal")]
    [Tooltip("Set to NoSignal to keep hover hints without sending any rage signal.")]
    [SerializeField] private RageSignalEventType signalEventType = RageSignalEventType.SpillDrinkOnDesk;

    [Header("Target")]
    [Tooltip("Optional: drag the target NPC Rage_Meter here. Leave empty for global broadcast.")]
    [SerializeField] private Rage_Meter targetNpc;
    [Tooltip("If true, sends to all NPCs when no target is assigned. Keep false to avoid accidental global rage.")]
    [SerializeField] private bool allowGlobalBroadcastWhenTargetMissing = false;

    [Header("Steal Object Distance Trigger")]
    [SerializeField, Min(0.01f)] private float stealObjectRequiredDistanceFromOrigin = 0.15f;
    [SerializeField] private bool stealObjectHorizontalDistanceOnly = true;
    [SerializeField] private bool stealObjectTriggerOnlyOnce = true;

    [Header("Bring Object Near Trigger")]
    [Tooltip("When this object comes near the assigned Target NPC, trigger this rage signal.")]
    [SerializeField, Min(0.01f)] private float bringNearRequiredDistance = 1f;
    [SerializeField] private bool bringNearHorizontalDistanceOnly = true;
    [SerializeField] private bool bringNearTriggerOnlyOnce = true;

    [Header("Loud Noise Trigger")]
    [Tooltip("Only for MakeLoudNoise: Interact sends the signal only when the target NPC is within this radius.")]
    [SerializeField, Min(0.01f)] private float loudNoiseRequiredDistance = 2.5f;
    [SerializeField] private bool loudNoiseHorizontalDistanceOnly = true;

    [Header("Knock Over Interaction")]
    [SerializeField] private bool knockOverOnlyOnce = true;
    [SerializeField, Min(0f)] private float knockOverImpulseForce = 2.5f;
    [SerializeField, Min(0f)] private float knockOverTorqueForce = 3.5f;
    [SerializeField] private Vector3 knockOverLocalTorqueAxis = Vector3.right;

    [Header("Limits")]
    [SerializeField, Min(0f)] private float sendCooldown = 0.1f;
    [SerializeField] private bool sendOnlyOnce;

    [Header("Debug")]
    [SerializeField] private bool logSignalSendToConsole = true;

    [Header("Hover Hint Icon")]
    [SerializeField] private bool showHoverHintIcon = true;
    [SerializeField, Min(0f)] private float hoverHintVerticalOffset = 0.8f;
    [SerializeField, Min(0.01f)] private float hoverHintScale = 0.2f;
    [Tooltip("PNG sprite used for the floating hint icon.")]
    [SerializeField] private Sprite hoverHintSprite;
    [Tooltip("Optional: if set, icon follows this anchor instead of using vertical offset from this prop.")]
    [SerializeField] private Transform hoverHintAnchor;
    [SerializeField] private bool hideHintWhenLocked = true;

    public RageSignalEventType SignalEventType => signalEventType;

    private bool hasSent;
    private bool hasTriggeredDistanceSignal;
    private bool hasTriggeredBringNearSignal;
    private bool hasKnockedOver;
    private float lastSendTime = -999f;
    private Vector3 originPosition;
    private Camera cachedMainCamera;
    private Transform hoverHintTransform;
    private SpriteRenderer hoverHintRenderer;
    private CoffeeSpillLockedItem coffeeLock;
    private MicrowaveLockedItem microwaveLock;
    private bool loggedMissingSpriteWarning;
    private void Awake()
    {
        originPosition = transform.position;
        CacheLockReferences();
        CacheMainCamera();
    }

    private void Update()
    {
        UpdateHoverHintVisibility();

        if (!UsesDistanceActivation())
        {
            return;
        }

        if (signalEventType == RageSignalEventType.StealObject)
        {
            if (stealObjectTriggerOnlyOnce && hasTriggeredDistanceSignal)
            {
                return;
            }

            Vector3 displacement = transform.position - originPosition;
            if (stealObjectHorizontalDistanceOnly)
            {
                displacement.y = 0f;
            }

            float requiredDistanceSquared = stealObjectRequiredDistanceFromOrigin * stealObjectRequiredDistanceFromOrigin;
            if (displacement.sqrMagnitude < requiredDistanceSquared)
            {
                return;
            }

            hasTriggeredDistanceSignal = true;
            TrySendSignal("distance-from-origin threshold exceeded");
            return;
        }

        if (signalEventType == RageSignalEventType.BringObjectNear)
        {
            if (bringNearTriggerOnlyOnce && hasTriggeredBringNearSignal)
            {
                return;
            }

            if (targetNpc == null)
            {
                return;
            }

            Vector3 toTargetNpc = targetNpc.transform.position - transform.position;
            if (bringNearHorizontalDistanceOnly)
            {
                toTargetNpc.y = 0f;
            }

            float requiredDistanceSquared = bringNearRequiredDistance * bringNearRequiredDistance;
            if (toTargetNpc.sqrMagnitude > requiredDistanceSquared)
            {
                return;
            }

            hasTriggeredBringNearSignal = true;
            TrySendSignal("interaction prop brought near target npc");
        }
    }

    private void LateUpdate()
    {
        UpdateHoverHintBillboard();
    }

    public void Interact()
    {
        TrySendSignal("Interact() called");
    }

    public bool CanPerformKnockOver(Collider hitCollider = null)
    {
        if (signalEventType != RageSignalEventType.KnockOver)
        {
            return false;
        }

        if (knockOverOnlyOnce && hasKnockedOver)
        {
            return false;
        }

        return ResolveKnockOverRigidbody(hitCollider) != null;
    }

    public bool TryPerformKnockOver(Transform interactor = null, Collider hitCollider = null, Vector3? hitPoint = null)
    {
        if (!CanPerformKnockOver(hitCollider))
        {
            return false;
        }

        Rigidbody targetBody = ResolveKnockOverRigidbody(hitCollider);
        if (targetBody == null)
        {
            return false;
        }

        targetBody.useGravity = true;
        targetBody.isKinematic = false;

        Vector3 impulseDirection = interactor != null ? interactor.forward : transform.forward;
        impulseDirection.y = 0f;
        if (impulseDirection.sqrMagnitude < 0.0001f)
        {
            impulseDirection = transform.right;
        }

        impulseDirection.Normalize();

        if (knockOverImpulseForce > 0f)
        {
            Vector3 forcePoint = hitPoint ?? targetBody.worldCenterOfMass;
            targetBody.AddForceAtPosition(impulseDirection * knockOverImpulseForce, forcePoint, ForceMode.Impulse);
        }

        if (knockOverTorqueForce > 0f)
        {
            Vector3 axis = knockOverLocalTorqueAxis.sqrMagnitude > 0.0001f
                ? knockOverLocalTorqueAxis.normalized
                : Vector3.right;
            Vector3 worldAxis = transform.TransformDirection(axis);
            targetBody.AddTorque(worldAxis * knockOverTorqueForce, ForceMode.Impulse);
        }

        hasKnockedOver = true;
        TrySendSignal("knock-over interaction");
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Trigger-enter activation is not used for current rage prop interactions.
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

    public void SendStealObjectSignal()
    {
        TrySendSpecificSignal(RageSignalIds.StealObject, "SendStealObjectSignal() called");
    }

    public void SendHitNpcWithProjectileSignal()
    {
        TrySendSpecificSignal(RageSignalIds.HitNpcWithProjectile, "SendHitNpcWithProjectileSignal() called");
    }

    public void SendMakeLoudNoiseSignal()
    {
        TrySendSpecificSignal(RageSignalIds.MakeLoudNoise, "SendMakeLoudNoiseSignal() called");
    }

    public void SendUnplugDeviceSignal()
    {
        TrySendSpecificSignal(RageSignalIds.UnplugDevice, "SendUnplugDeviceSignal() called");
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
        bool allowRepeatedFinalBossSpill = signalEventType == RageSignalEventType.FinalBossSpillDrinkOnDesk;

        if (!allowRepeatedFinalBossSpill && sendOnlyOnce && hasSent)
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

        if (targetNpc != null && string.IsNullOrWhiteSpace(targetNpc.NpcSignalId))
        {
            if (logSignalSendToConsole)
            {
                Debug.LogWarning(
                    "[RageInteractionPropSignal] " + name +
                    " has target NPC assigned but its NpcSignalId is empty.",
                    this);
            }

            return;
        }

        if (targetNpc == null && !allowGlobalBroadcastWhenTargetMissing)
        {
            if (logSignalSendToConsole)
            {
                Debug.LogWarning(
                    "[RageInteractionPropSignal] " + name +
                    " did not send signal '" + resolvedSignalId +
                    "' because no target NPC is assigned.",
                    this);
            }

            return;
        }

        if (signalEventType == RageSignalEventType.MakeLoudNoise)
        {
            float currentDistance;
            if (!IsTargetNpcWithinDistance(loudNoiseRequiredDistance, loudNoiseHorizontalDistanceOnly, out currentDistance))
            {
                if (logSignalSendToConsole)
                {
                    Debug.Log(
                        "[RageInteractionPropSignal] " + name +
                        " did not send signal '" + resolvedSignalId +
                        "' because target NPC is outside loud-noise radius (" +
                        currentDistance.ToString("F2") + " > " + loudNoiseRequiredDistance.ToString("F2") + ").",
                        this);
                }

                return;
            }
        }

        string resolvedTargetNpcId = ResolveTargetNpcId();
        RageSignalHub.RaiseSignal(resolvedSignalId, resolvedTargetNpcId);
        string targetInfo = string.IsNullOrWhiteSpace(resolvedTargetNpcId)
            ? "global broadcast"
            : "target npc id: " + resolvedTargetNpcId;

        if (!allowRepeatedFinalBossSpill)
        {
            hasSent = true;
        }
        lastSendTime = Time.time;

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
            case RageSignalEventType.FinalBossSpillDrinkOnDesk:
                return RageSignalIds.SpillDrinkOnDesk;
            case RageSignalEventType.MicrowaveFish:
                return RageSignalIds.MicrowaveFish;
            case RageSignalEventType.StealObject:
                return RageSignalIds.StealObject;
            case RageSignalEventType.KnockOver:
                return RageSignalIds.KnockOver;
            case RageSignalEventType.BringObjectNear:
                return RageSignalIds.BringObjectNear;
            case RageSignalEventType.HitNpcWithProjectile:
                return RageSignalIds.HitNpcWithProjectile;
            case RageSignalEventType.MakeLoudNoise:
                return RageSignalIds.MakeLoudNoise;
            case RageSignalEventType.UnplugDevice:
                return RageSignalIds.UnplugDevice;
            case RageSignalEventType.NoSignal:
            case RageSignalEventType.MicrowaveFishItem:
                return string.Empty;
            default:
                return string.Empty;
        }
    }

    private string ResolveTargetNpcId()
    {
        if (targetNpc == null)
        {
            return string.Empty;
        }

        return targetNpc.NpcSignalId;
    }

    [ContextMenu("Debug/Set Current Position As Origin")]
    private void DebugSetCurrentPositionAsOrigin()
    {
        originPosition = transform.position;
        hasTriggeredDistanceSignal = false;
    }

    private void CacheLockReferences()
    {
        coffeeLock = GetComponent<CoffeeSpillLockedItem>();
        if (coffeeLock == null)
        {
            coffeeLock = GetComponentInParent<CoffeeSpillLockedItem>();
        }

        if (coffeeLock == null)
        {
            coffeeLock = GetComponentInChildren<CoffeeSpillLockedItem>();
        }

        microwaveLock = GetComponent<MicrowaveLockedItem>();
        if (microwaveLock == null)
        {
            microwaveLock = GetComponentInParent<MicrowaveLockedItem>();
        }

        if (microwaveLock == null)
        {
            microwaveLock = GetComponentInChildren<MicrowaveLockedItem>();
        }
    }

    private void CacheMainCamera()
    {
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }
    }

    private Vector3 GetHintAnchorPosition()
    {
        if (hoverHintAnchor != null)
        {
            return hoverHintAnchor.position;
        }

        return transform.position + Vector3.up * hoverHintVerticalOffset;
    }

    private void EnsureHoverHintObject()
    {
        if (hoverHintTransform != null)
        {
            return;
        }

        GameObject iconObject = new GameObject("RageHintIcon");
        iconObject.transform.SetParent(transform, false);
        iconObject.transform.localScale = Vector3.one * hoverHintScale;

        hoverHintRenderer = iconObject.AddComponent<SpriteRenderer>();
        hoverHintRenderer.sprite = hoverHintSprite;
        hoverHintRenderer.color = Color.white;
        hoverHintRenderer.sortingOrder = 2000;

        hoverHintTransform = iconObject.transform;
        iconObject.SetActive(false);
    }

    private bool IsInteractionLocked()
    {
        if (!hideHintWhenLocked)
        {
            return false;
        }

        bool coffeeLocked = coffeeLock != null && coffeeLock.IsLocked;
        bool microwaveLocked = microwaveLock != null && microwaveLock.IsLocked;
        return coffeeLocked || microwaveLocked;
    }

    private void SetHoverHintActive(bool isVisible)
    {
        if (hoverHintTransform == null)
        {
            if (!isVisible)
            {
                return;
            }

            EnsureHoverHintObject();
        }

        if (hoverHintTransform.gameObject.activeSelf != isVisible)
        {
            hoverHintTransform.gameObject.SetActive(isVisible);
        }
    }

    private void UpdateHoverHintVisibility()
    {
        if (!showHoverHintIcon)
        {
            SetHoverHintActive(false);
            return;
        }

        bool allowRepeatedFinalBossSpill = signalEventType == RageSignalEventType.FinalBossSpillDrinkOnDesk;
        if (!allowRepeatedFinalBossSpill && sendOnlyOnce && hasSent)
        {
            SetHoverHintActive(false);
            return;
        }

        if (IsInteractionLocked())
        {
            SetHoverHintActive(false);
            return;
        }

        if (hoverHintSprite == null)
        {
            SetHoverHintActive(false);
            if (!loggedMissingSpriteWarning)
            {
                Debug.LogWarning(
                    "[RageInteractionPropSignal] " + name +
                    " has hover hint enabled but no Hover Hint Sprite assigned.",
                    this);
                loggedMissingSpriteWarning = true;
            }
            return;
        }

        loggedMissingSpriteWarning = false;

        Vector3 anchorPosition = GetHintAnchorPosition();
        SetHoverHintActive(true);
        if (hoverHintTransform == null)
        {
            return;
        }

        hoverHintTransform.localScale = Vector3.one * hoverHintScale;
        hoverHintTransform.position = anchorPosition;

        if (hoverHintRenderer != null && hoverHintRenderer.sprite != hoverHintSprite)
        {
            hoverHintRenderer.sprite = hoverHintSprite;
        }
    }

    private void UpdateHoverHintBillboard()
    {
        if (hoverHintTransform == null || !hoverHintTransform.gameObject.activeSelf)
        {
            return;
        }

        CacheMainCamera();
        if (cachedMainCamera == null)
        {
            return;
        }

        Vector3 lookDirection = hoverHintTransform.position - cachedMainCamera.transform.position;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        hoverHintTransform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private bool UsesDistanceActivation()
    {
        return signalEventType == RageSignalEventType.StealObject ||
               signalEventType == RageSignalEventType.BringObjectNear;
    }

    private bool IsTargetNpcWithinDistance(float requiredDistance, bool horizontalOnly, out float currentDistance)
    {
        currentDistance = float.PositiveInfinity;
        if (targetNpc == null)
        {
            return false;
        }

        Vector3 toTargetNpc = targetNpc.transform.position - transform.position;
        if (horizontalOnly)
        {
            toTargetNpc.y = 0f;
        }

        float requiredDistanceSqr = requiredDistance * requiredDistance;
        float distanceSqr = toTargetNpc.sqrMagnitude;
        currentDistance = Mathf.Sqrt(distanceSqr);
        return distanceSqr <= requiredDistanceSqr;
    }

    private Rigidbody ResolveKnockOverRigidbody(Collider hitCollider)
    {
        if (hitCollider != null)
        {
            if (hitCollider.attachedRigidbody != null)
            {
                return hitCollider.attachedRigidbody;
            }

            Rigidbody fromHitParent = hitCollider.GetComponentInParent<Rigidbody>();
            if (fromHitParent != null)
            {
                return fromHitParent;
            }
        }

        Rigidbody localBody = GetComponent<Rigidbody>();
        if (localBody != null)
        {
            return localBody;
        }

        Rigidbody parentBody = GetComponentInParent<Rigidbody>();
        if (parentBody != null)
        {
            return parentBody;
        }

        return GetComponentInChildren<Rigidbody>();
    }

}
