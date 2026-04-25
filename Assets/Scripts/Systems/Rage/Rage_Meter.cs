using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using OfficeFlipOut.Systems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Rage_Meter : MonoBehaviour
{
    [Header("Rage Setup")]
    [SerializeField, Min(1)] private int requiredSignals = 3;

    [Header("Signal Listening")]
    [Tooltip("Optional: only accept global signals targeting this id. Leave empty to accept untargeted signals.")]
    [SerializeField] private string npcSignalId;

    [Header("Boss gating")]
    [Tooltip("When true (e.g. Da Boss), sabotage signals are ignored until all other NPC meters are flipped.")]
    [SerializeField] private bool requireAllOtherNpcsFlippedBeforeAcceptingSignals;

    [Header("Projectile Hit Rage")]
    [Tooltip("When enabled, this NPC tracks collisions from player-thrown objects and grants the HitNpcWithProjectile signal after enough hits.")]
    [SerializeField] private bool enableProjectileHitRage;
    [SerializeField, Min(1)] private int projectileHitsRequiredForSignal = 3;
    [SerializeField, Min(0f)] private float projectileHitMinImpactSpeed = 1.25f;
    [SerializeField] private bool logProjectileHitDebug = true;

    [Header("Flip Out Trigger")]
    [SerializeField] private MonoBehaviour flipOutReceiver;
    [SerializeField] private string flipOutMethodName = "FlipOut";

    [Header("Flip Out Cinematic")]
    [SerializeField] private bool playFlipOutCinematic = true;
    [SerializeField, Min(0f)] private float thresholdToCameraMoveDelay = 0.15f;
    [SerializeField, Min(0.05f)] private float cameraMoveToNpcDuration = 0.85f;
    [SerializeField, Min(0.05f)] private float npcRageDuration = 2.0f;
    [SerializeField, Min(0.05f)] private float cameraReturnToPlayerDuration = 0.5f;
    [SerializeField] private float cinematicLookTargetHeight = 1.4f;
    [SerializeField] private bool lockGameplayInputDuringCinematic = true;
    [SerializeField] private string cinematicCameraName = "RageCinematicCamera";
    [SerializeField, Min(0.5f)] private float cinematicCameraDistance = 3.5f;
    [SerializeField, Min(0f)] private float cinematicCameraHeight = 1.7f;

    [Header("Flip Out Physics Blast")]
    [SerializeField] private bool emitFlipOutPhysicsBlast = true;
    [SerializeField, Min(0.1f)] private float flipOutBlastRadius = 6f;
    [SerializeField] private float flipOutBlastForce = 14f;
    [SerializeField] private float flipOutBlastUpwardsModifier = 1.5f;
    [SerializeField] private float flipOutBlastRandomTorque = 10f;
    [SerializeField] private ForceMode flipOutBlastForceMode = ForceMode.Impulse;

    [Header("Visuals")]
    [SerializeField] private bool ensureNpcFacesCamera = true;
    [SerializeField] private SpriteRenderer npcBodyRenderer;
    [SerializeField] private Sprite npcCalmSprite;
    [SerializeField] private Sprite npcAngrySprite;
    [SerializeField] private Sprite npcFlipOutBodySprite;
    [SerializeField] private bool showRageFaceOverlay = true;
    [SerializeField] private Transform rageFaceAnchor;
    [SerializeField] private SpriteRenderer rageFaceRenderer;
    [SerializeField] private Sprite rageFaceCalmSprite;
    [SerializeField] private Sprite rageFaceRage1Sprite;
    [SerializeField] private Sprite rageFaceRage2Sprite;
    [SerializeField] private Sprite rageFaceRage3Sprite;
    [SerializeField] private Sprite rageFaceRage4Sprite;
    [SerializeField] private Sprite rageFaceFlipOutSprite;
    [SerializeField] private float rageFaceVerticalOffset = 2f;
    [SerializeField] private Vector3 rageFaceScale = new Vector3(0.25f, 0.25f, 1f);

    [Header("Debug Testing")]
    [SerializeField] private bool enableDebugInput;
    [SerializeField] private KeyCode addRageKey = KeyCode.Equals;
    [SerializeField] private KeyCode removeRageKey = KeyCode.Minus;
    [SerializeField] private KeyCode resetRageKey = KeyCode.Backspace;

    public int CurrentRage => currentRage;
    public int RequiredSignals => requiredSignals;
    public bool IsFlippedOut => isFlippedOut;
    public string NpcSignalId => npcSignalId;

    /// <summary>Fired when a new unique sabotage signal increases rage (for VFX / juice).</summary>
    public event Action<Rage_Meter> SuccessfulSabotageSignal;

    /// <summary>Fired once when this NPC enters flip-out state.</summary>
    public event Action<Rage_Meter> FlippedOut;

    public Sprite CurrentRageFaceSprite
    {
        get
        {
            return GetCurrentRageFaceSprite();
        }
    }

    public Sprite ClipboardPortraitSprite
    {
        get
        {
            if (npcCalmSprite != null)
            {
                return npcCalmSprite;
            }

            if (npcBodyRenderer != null && npcBodyRenderer.sprite != null)
            {
                return npcBodyRenderer.sprite;
            }

            if (npcAngrySprite != null)
            {
                return npcAngrySprite;
            }

            return npcFlipOutBodySprite;
        }
    }

    private readonly HashSet<string> receivedSignalIds = new HashSet<string>();
    private readonly HashSet<int> countedProjectileObjectIds = new HashSet<int>();

    private int currentRage;
    private int projectileHitCount;
    private bool isFlippedOut;
    private bool isFlipOutSequenceRunning;
    private AudioListener temporarilyDisabledSourceAudioListener;

    private void Awake()
    {
        npcSignalId = NormalizeLegacyNpcSignalId(npcSignalId);

        if (ensureNpcFacesCamera && GetComponent<NpcCameraBillboard>() == null)
        {
            gameObject.AddComponent<NpcCameraBillboard>();
        }
        AutoAssignVisualReferences();
        EnsureRageFaceVisual();
        RefreshNpcBodySprite();
        RefreshRageFaceSprite();
    }

    /// <summary>
    /// Matches EmployeeProfileData ids (npc_1) if older scenes used npc1 / npc2 without underscores.
    /// </summary>
    private static string NormalizeLegacyNpcSignalId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        switch (id.Trim())
        {
            case "npc1": return "npc_1";
            case "npc2": return "npc_2";
            case "npc3": return "npc_3";
            default: return id;
        }
    }

    private void OnEnable()
    {
        RageSignalHub.SignalRaised += HandleGlobalSignal;
    }

    private void OnDisable()
    {
        RageSignalHub.SignalRaised -= HandleGlobalSignal;

        if (isFlipOutSequenceRunning)
        {
            isFlipOutSequenceRunning = false;
            GameRuntimeState.SetCinematicInputLocked(false);
        }
    }

    private void Update()
    {
        if (!enableDebugInput)
        {
            return;
        }

        if (IsDebugKeyPressed(addRageKey))
        {
            DebugAddOneRage();
        }

        if (IsDebugKeyPressed(removeRageKey))
        {
            DebugRemoveOneRage();
        }

        if (IsDebugKeyPressed(resetRageKey))
        {
            DebugResetRage();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHandleProjectileHit(collision);
    }

    public bool RegisterProjectileHit(ProjectileHitNpcRage projectile, Collision collision)
    {
        if (!enableProjectileHitRage || projectile == null || collision == null)
        {
            return false;
        }

        if (isFlippedOut || isFlipOutSequenceRunning)
        {
            return false;
        }

        if (HasReceivedSignal(RageSignalIds.HitNpcWithProjectile))
        {
            return true;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < projectileHitMinImpactSpeed)
        {
            if (logProjectileHitDebug)
            {
                Debug.Log(
                    "[Rage_Meter] Ignored low-impact projectile on " + name +
                    " (" + impactSpeed.ToString("F2") + " < " + projectileHitMinImpactSpeed.ToString("F2") + ").",
                    this);
            }

            return false;
        }

        int projectileObjectId = projectile.GetObjectInstanceId();
        if (!countedProjectileObjectIds.Add(projectileObjectId))
        {
            if (logProjectileHitDebug)
            {
                Debug.Log(
                    "[Rage_Meter] Ignored duplicate projectile object on " + name + ".",
                    this);
            }

            return false;
        }

        projectileHitCount++;

        if (logProjectileHitDebug)
        {
            Debug.Log(
                "[Rage_Meter] " + name +
                " projectile hits: " + projectileHitCount + " / " + projectileHitsRequiredForSignal +
                " after hit by '" + projectile.name + "'.",
                this);
        }

        if (projectileHitCount < projectileHitsRequiredForSignal)
        {
            return true;
        }

        if (logProjectileHitDebug)
        {
            Debug.Log(
                "[Rage_Meter] " + name +
                " reached projectile threshold and will receive signal '" + RageSignalIds.HitNpcWithProjectile + "'.",
                this);
        }

        AddSignal(RageSignalIds.HitNpcWithProjectile);
        return true;
    }

    private bool IsDebugKeyPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Key inputSystemKey = ConvertToInputSystemKey(keyCode);
            if (inputSystemKey != Key.None)
            {
                return Keyboard.current[inputSystemKey].wasPressedThisFrame;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(keyCode);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private Key ConvertToInputSystemKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Minus:
                return Key.Minus;
            case KeyCode.Equals:
                return Key.Equals;
            case KeyCode.Backspace:
                return Key.Backspace;
            case KeyCode.KeypadMinus:
                return Key.NumpadMinus;
            case KeyCode.KeypadPlus:
                return Key.NumpadPlus;
            default:
                return Key.None;
        }
    }
#endif

    public void ReceiveSignal(string signalId)
    {
        TryAddSignal(signalId, false);
    }

    public void AddSignal(string signalId)
    {
        TryAddSignal(signalId, false);
    }

    /// <summary>Debug / cheat paths can bypass boss ordering.</summary>
    public void AddSignalIgnoringBossGate(string signalId)
    {
        TryAddSignal(signalId, true);
    }

    private void TryAddSignal(string signalId, bool ignoreBossGate)
    {
        if (isFlippedOut)
        {
            return;
        }

        if (isFlipOutSequenceRunning)
        {
            return;
        }

        if (!ignoreBossGate && !MayAcceptSabotageSignals())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(signalId))
        {
            signalId = "anonymous_" + (currentRage + 1);
        }

        if (!receivedSignalIds.Add(signalId))
        {
            return;
        }

        if (signalId == RageSignalIds.HitNpcWithProjectile)
        {
            projectileHitCount = Mathf.Max(projectileHitCount, projectileHitsRequiredForSignal);
        }

        currentRage = Mathf.Min(currentRage + 1, requiredSignals);
        RefreshNpcBodySprite();
        RefreshRageFaceSprite();

        SuccessfulSabotageSignal?.Invoke(this);

        if (currentRage >= requiredSignals)
        {
            BeginFlipOutSequence();
        }
    }

    private bool MayAcceptSabotageSignals()
    {
        if (!requireAllOtherNpcsFlippedBeforeAcceptingSignals)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(npcSignalId))
        {
            return true;
        }

        if (ProgressTracker.Instance == null)
        {
            return false;
        }

        return ProgressTracker.Instance.AreAllCoworkersFlipped(npcSignalId);
    }

    public bool HasReceivedSignal(string signalId)
    {
        if (string.IsNullOrWhiteSpace(signalId))
        {
            return false;
        }

        return receivedSignalIds.Contains(signalId);
    }

    public void ResetRage()
    {
        currentRage = 0;
        projectileHitCount = 0;
        isFlippedOut = false;
        isFlipOutSequenceRunning = false;
        receivedSignalIds.Clear();
        countedProjectileObjectIds.Clear();
        GameRuntimeState.SetCinematicInputLocked(false);
        RefreshNpcBodySprite();
        RefreshRageFaceSprite();
    }

    public void RemoveRage(int amount = 1)
    {
        if (amount < 1)
        {
            return;
        }

        currentRage = Mathf.Max(0, currentRage - amount);
        if (currentRage < requiredSignals)
        {
            isFlippedOut = false;
        }

        RefreshNpcBodySprite();
        RefreshRageFaceSprite();
    }

    [ContextMenu("Debug/Add 1 Rage")]
    private void DebugAddOneRage()
    {
        AddSignalIgnoringBossGate("debug_" + Time.frameCount + "_" + UnityEngine.Random.Range(0, 100000));
    }

    [ContextMenu("Debug/Remove 1 Rage")]
    private void DebugRemoveOneRage()
    {
        RemoveRage(1);
    }

    [ContextMenu("Debug/Reset Rage")]
    private void DebugResetRage()
    {
        ResetRage();
    }

    private void BeginFlipOutSequence()
    {
        if (isFlippedOut || isFlipOutSequenceRunning)
        {
            return;
        }

        if (!playFlipOutCinematic)
        {
            EnterRageState();
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            EnterRageState();
            return;
        }

        StartCoroutine(FlipOutSequenceRoutine(mainCamera.transform));
    }

    private IEnumerator FlipOutSequenceRoutine(Transform cameraTransform)
    {
        isFlipOutSequenceRunning = true;

        bool shouldLockInput = lockGameplayInputDuringCinematic && !GameRuntimeState.ShouldBlockGameplayInput;
        if (shouldLockInput)
        {
            GameRuntimeState.SetCinematicInputLocked(true);
        }

        Camera mainCamera = cameraTransform.GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Vector3 cameraStartPosition = cameraTransform.position;
        Quaternion cameraStartRotation = cameraTransform.rotation;
        Camera cinematicCamera = null;
        Transform cinematicTransform = cameraTransform;
        if (mainCamera != null)
        {
            cinematicCamera = SpawnCinematicCamera(mainCamera);
            if (cinematicCamera != null)
            {
                cinematicTransform = cinematicCamera.transform;
                mainCamera.enabled = false;
            }
        }

        Vector3 cinematicFocusPosition;
        Quaternion cinematicFocusRotation;
        CalculateCinematicPose(cameraStartPosition, out cinematicFocusPosition, out cinematicFocusRotation);

        // Step 1: Threshold reached, brief pre-camera delay.
        if (thresholdToCameraMoveDelay > 0f)
        {
            yield return new WaitForSeconds(thresholdToCameraMoveDelay);
        }

        // Step 2: Player camera moves to NPC cinematic framing.
        float focusElapsed = 0f;
        while (focusElapsed < cameraMoveToNpcDuration)
        {
            focusElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(focusElapsed / cameraMoveToNpcDuration);
            float eased = t * t * (3f - 2f * t);
            cinematicTransform.position = Vector3.Lerp(cameraStartPosition, cinematicFocusPosition, eased);
            Quaternion dynamicLookRotation = GetLookRotationFromPosition(cinematicTransform.position, cinematicFocusRotation);
            cinematicTransform.rotation = Quaternion.Slerp(cameraStartRotation, dynamicLookRotation, eased);
            yield return null;
        }

        cinematicTransform.position = cinematicFocusPosition;
        cinematicTransform.rotation = GetLookRotationFromPosition(cinematicTransform.position, cinematicFocusRotation);

        EnterRageState();

        // Step 3: NPC rages while camera stays locked on them.
        if (npcRageDuration > 0f)
        {
            float holdElapsed = 0f;
            while (holdElapsed < npcRageDuration)
            {
                holdElapsed += Time.deltaTime;
                cinematicTransform.rotation = GetLookRotationFromPosition(cinematicTransform.position, cinematicFocusRotation);
                yield return null;
            }
        }

        // Step 4: Player camera returns.
        float returnElapsed = 0f;
        Vector3 returnStartPosition = cinematicTransform.position;
        Quaternion returnStartRotation = cinematicTransform.rotation;
        while (returnElapsed < cameraReturnToPlayerDuration)
        {
            returnElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(returnElapsed / cameraReturnToPlayerDuration);
            float eased = t * t * (3f - 2f * t);
            cinematicTransform.position = Vector3.Lerp(returnStartPosition, cameraStartPosition, eased);
            cinematicTransform.rotation = Quaternion.Slerp(returnStartRotation, cameraStartRotation, eased);
            yield return null;
        }

        cinematicTransform.position = cameraStartPosition;
        cinematicTransform.rotation = cameraStartRotation;

        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }

        if (temporarilyDisabledSourceAudioListener != null)
        {
            temporarilyDisabledSourceAudioListener.enabled = true;
            temporarilyDisabledSourceAudioListener = null;
        }

        if (cinematicCamera != null)
        {
            Destroy(cinematicCamera.gameObject);
        }

        if (shouldLockInput)
        {
            GameRuntimeState.SetCinematicInputLocked(false);
        }

        isFlipOutSequenceRunning = false;
    }

    private Camera SpawnCinematicCamera(Camera sourceCamera)
    {
        if (sourceCamera == null)
        {
            return null;
        }

        GameObject cameraObject = new GameObject(cinematicCameraName);
        cameraObject.transform.position = sourceCamera.transform.position;
        cameraObject.transform.rotation = sourceCamera.transform.rotation;

        Camera newCamera = cameraObject.AddComponent<Camera>();
        newCamera.CopyFrom(sourceCamera);
        newCamera.enabled = true;

        AudioListener sourceListener = sourceCamera.GetComponent<AudioListener>();
        if (sourceListener != null && sourceListener.enabled)
        {
            temporarilyDisabledSourceAudioListener = sourceListener;
            sourceListener.enabled = false;
            AudioListener newListener = cameraObject.AddComponent<AudioListener>();
            newListener.enabled = true;
        }

        return newCamera;
    }

    private void CalculateCinematicPose(Vector3 sourceCameraPosition, out Vector3 desiredPosition, out Quaternion desiredRotation)
    {
        Vector3 lookTarget = transform.position + Vector3.up * cinematicLookTargetHeight;
        Vector3 fromNpcToPlayerCam = sourceCameraPosition - transform.position;
        fromNpcToPlayerCam.y = 0f;
        if (fromNpcToPlayerCam.sqrMagnitude <= 0.001f)
        {
            fromNpcToPlayerCam = -transform.forward;
            fromNpcToPlayerCam.y = 0f;
        }

        Vector3 direction = fromNpcToPlayerCam.normalized;
        desiredPosition = lookTarget + (direction * cinematicCameraDistance);
        desiredPosition.y = transform.position.y + cinematicCameraHeight;
        desiredRotation = Quaternion.LookRotation(lookTarget - desiredPosition, Vector3.up);
    }

    private Quaternion GetLookRotationFromPosition(Vector3 cameraPosition, Quaternion fallbackRotation)
    {
        Vector3 lookTarget = transform.position + Vector3.up * cinematicLookTargetHeight;
        Vector3 lookDirection = lookTarget - cameraPosition;
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return fallbackRotation;
        }

        return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private void EnterRageState()
    {
        if (isFlippedOut)
        {
            return;
        }

        isFlippedOut = true;
        RefreshNpcBodySprite();
        RefreshRageFaceSprite();
        FlippedOut?.Invoke(this);

        if (flipOutReceiver != null && !string.IsNullOrEmpty(flipOutMethodName))
        {
            flipOutReceiver.SendMessage(flipOutMethodName, SendMessageOptions.DontRequireReceiver);
        }

        if (emitFlipOutPhysicsBlast)
        {
            RageSignalHub.RaiseFlipOutBlast(new RageFlipOutBlastData(
                transform.position,
                flipOutBlastRadius,
                flipOutBlastForce,
                flipOutBlastUpwardsModifier,
                flipOutBlastRandomTorque,
                flipOutBlastForceMode,
                gameObject));
        }
    }

    private void HandleGlobalSignal(string signalId, string targetNpcId)
    {
        if (!string.IsNullOrEmpty(targetNpcId) && targetNpcId != npcSignalId)
        {
            return;
        }

        AddSignal(signalId);
    }

    private void TryHandleProjectileHit(Collision collision)
    {
        if (!enableProjectileHitRage || isFlippedOut || isFlipOutSequenceRunning || collision == null)
        {
            return;
        }

        ProjectileHitNpcRage thrownMarker = ResolveThrownMarker(collision);
        if (thrownMarker == null)
        {
            return;
        }

        if (thrownMarker.HasBeenConsumedByNpc)
        {
            return;
        }

        if (RegisterProjectileHit(thrownMarker, collision))
        {
            thrownMarker.MarkConsumedByNpc();
        }
    }

    private static ProjectileHitNpcRage ResolveThrownMarker(Collision collision)
    {
        if (collision == null)
        {
            return null;
        }

        if (collision.rigidbody != null)
        {
            ProjectileHitNpcRage onRigidbody = collision.rigidbody.GetComponent<ProjectileHitNpcRage>();
            if (onRigidbody != null)
            {
                return onRigidbody;
            }
        }

        if (collision.collider != null)
        {
            ProjectileHitNpcRage onCollider = collision.collider.GetComponentInParent<ProjectileHitNpcRage>();
            if (onCollider != null)
            {
                return onCollider;
            }
        }

        return null;
    }

    private void AutoAssignVisualReferences()
    {
        if (npcBodyRenderer == null)
        {
            npcBodyRenderer = FindBestNpcBodyRenderer();
        }

        if (npcCalmSprite == null && npcBodyRenderer != null)
        {
            npcCalmSprite = npcBodyRenderer.sprite;
        }
    }

    private SpriteRenderer FindBestNpcBodyRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer candidate = renderers[i];
            if (candidate == null || candidate == rageFaceRenderer)
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void RefreshNpcBodySprite()
    {
        if (npcBodyRenderer == null)
        {
            return;
        }

        Sprite sprite = GetCurrentNpcBodySprite();
        if (sprite != null)
        {
            npcBodyRenderer.sprite = sprite;
        }
    }

    private void EnsureRageFaceVisual()
    {
        if (!showRageFaceOverlay)
        {
            return;
        }

        if (rageFaceAnchor == null)
        {
            return;
        }

        if (rageFaceRenderer == null)
        {
            rageFaceRenderer = rageFaceAnchor.GetComponent<SpriteRenderer>();
            if (rageFaceRenderer != null)
            {
                return;
            }

            Transform existing = rageFaceAnchor.Find("RageFaceVisual");
            if (existing != null)
            {
                rageFaceRenderer = existing.GetComponent<SpriteRenderer>();
            }
        }

        if (rageFaceRenderer != null)
        {
            rageFaceRenderer.transform.localScale = rageFaceScale;
        }
    }

    private void RefreshRageFaceSprite()
    {
        if (!showRageFaceOverlay)
        {
            if (rageFaceRenderer != null)
            {
                rageFaceRenderer.enabled = false;
            }

            return;
        }

        EnsureRageFaceVisual();
        if (rageFaceRenderer == null)
        {
            return;
        }

        Sprite faceSprite = GetCurrentRageFaceSprite();
        rageFaceRenderer.sprite = faceSprite;
        rageFaceRenderer.enabled = faceSprite != null;
    }

    private Sprite GetCurrentNpcBodySprite()
    {
        if (isFlippedOut && npcFlipOutBodySprite != null)
        {
            return npcFlipOutBodySprite;
        }

        if (!isFlippedOut && currentRage > 0 && npcAngrySprite != null)
        {
            return npcAngrySprite;
        }

        return npcCalmSprite;
    }

    private Sprite GetCurrentRageFaceSprite()
    {
        bool hasReachedFlipThreshold = currentRage >= requiredSignals;

        // As soon as threshold is reached, force full-rage face even before flip-out state toggles.
        // This avoids a one-frame fallback to calm when intermediate rage face sprites are not assigned.
        if (hasReachedFlipThreshold)
        {
            if (rageFaceFlipOutSprite != null)
            {
                return rageFaceFlipOutSprite;
            }

            Sprite thresholdFace = GetRageFaceSpriteForNormalizedProgress(1f);
            if (thresholdFace != null)
            {
                return thresholdFace;
            }
        }

        if (isFlippedOut)
        {
            if (rageFaceFlipOutSprite != null)
            {
                return rageFaceFlipOutSprite;
            }

            // If no dedicated flip-out face exists, keep strongest rage face instead of dropping to calm.
            Sprite maxRage = GetRageFaceSpriteForNormalizedProgress(1f);
            if (maxRage != null)
            {
                return maxRage;
            }
        }

        if (!isFlippedOut && currentRage > 0)
        {
            float normalizedRage = (float)currentRage / Mathf.Max(1, requiredSignals);
            Sprite stageSprite = GetRageFaceSpriteForNormalizedProgress(normalizedRage);
            if (stageSprite != null)
            {
                return stageSprite;
            }
        }

        return rageFaceCalmSprite;
    }

    private Sprite GetRageFaceSpriteForNormalizedProgress(float normalizedProgress)
    {
        float clamped = Mathf.Clamp01(normalizedProgress);
        // Use floor-based bucketing so early rage values start at stage 1
        // (ceil would jump to stage 2 too quickly, e.g. 1/3 rage).
        int stage = Mathf.Clamp(Mathf.FloorToInt(clamped * 4f), 1, 4);

        switch (stage)
        {
            case 4:
                return rageFaceRage4Sprite ?? rageFaceRage3Sprite ?? rageFaceRage2Sprite ?? rageFaceRage1Sprite;
            case 3:
                return rageFaceRage3Sprite ?? rageFaceRage2Sprite ?? rageFaceRage1Sprite;
            case 2:
                return rageFaceRage2Sprite ?? rageFaceRage1Sprite;
            default:
                return rageFaceRage1Sprite;
        }
    }

    private void OnValidate()
    {
        if (requiredSignals < 1)
        {
            requiredSignals = 1;
        }

        if (projectileHitsRequiredForSignal < 1)
        {
            projectileHitsRequiredForSignal = 1;
        }

        projectileHitMinImpactSpeed = Mathf.Max(0f, projectileHitMinImpactSpeed);

        rageFaceVerticalOffset = Mathf.Max(0f, rageFaceVerticalOffset);
        currentRage = Mathf.Clamp(currentRage, 0, requiredSignals);
        AutoAssignVisualReferences();
        EnsureRageFaceVisual();

        if (npcBodyRenderer != null)
        {
            RefreshNpcBodySprite();
        }

        RefreshRageFaceSprite();
    }
}

public static class RageSignalHub
{
    public delegate void RageSignalDelegate(string signalId, string targetNpcId);
    public static event RageSignalDelegate SignalRaised;

    public delegate void RageFlipOutBlastDelegate(RageFlipOutBlastData blastData);
    public static event RageFlipOutBlastDelegate FlipOutBlastRaised;

    public static void RaiseSignal(string signalId, string targetNpcId = "")
    {
        SignalRaised?.Invoke(signalId, targetNpcId);
    }

    public static void RaiseFlipOutBlast(RageFlipOutBlastData blastData)
    {
        FlipOutBlastRaised?.Invoke(blastData);
    }
}

public struct RageFlipOutBlastData
{
    public Vector3 Origin;
    public float Radius;
    public float Force;
    public float UpwardsModifier;
    public float RandomTorque;
    public ForceMode ForceMode;
    public GameObject Source;

    public RageFlipOutBlastData(
        Vector3 origin,
        float radius,
        float force,
        float upwardsModifier,
        float randomTorque,
        ForceMode forceMode,
        GameObject source)
    {
        Origin = origin;
        Radius = radius;
        Force = force;
        UpwardsModifier = upwardsModifier;
        RandomTorque = randomTorque;
        ForceMode = forceMode;
        Source = source;
    }
}
