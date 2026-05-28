using UnityEngine;
using OfficeFlipOut.Data;
using OfficeFlipOut.UI;
using OfficeFlipOut.UI.Hud;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Temporary component to store original rigidbody properties while held.
/// </summary>
public class HeldObjectState : MonoBehaviour
{
    public float originMass;
    public float originDrag;
    public float originAngularDrag;
    public bool originIsKinematic;
    public bool originUseGravity;
    public CollisionDetectionMode originCollisionMode;
    public RigidbodyInterpolation originInterpolation;
}

public class PhysicsGrab : MonoBehaviour
{
    [Header("Grab Settings")]
    public Camera cam;
    public Transform holdPoint;

    public float grabDistance = 3f;
    public float grabSphereRadius = 0.3f; // Radius for sphere cast to be more forgiving (increased from 0.2)
    public float throwForce = 10f;

    [Header("Aiming Reticle")]
    [Tooltip("When true, this component publishes interactable hover state to the HUD reticle each frame.")]
    public bool publishReticleHoverState = true;

    [Header("Rage Interaction")]
    public bool requireFishForMicrowaveInteraction = true;
    public string fishNameToken = "fish";

    [Header("Microwave Auto Snap")]
    public bool autoSnapFishIntoMicrowave = true;
    public float fishAutoSnapDistance = 1f;
    public Vector3 fishSnapLocalPosition = new Vector3(0f, 0.08f, 0f);
    public Vector3 fishSnapLocalEulerAngles = Vector3.zero;
    [Header("Fish Throw Microwave Snap")]
    public bool allowThrownFishMicrowaveSnap = true;
    public float thrownFishSnapDistance = 0.9f;
    public float thrownFishSnapMinSpeed = 3f;
    public float thrownFishSnapLifetime = 3f;

    [Header("Coffee Spill Rage")]
    public bool enableCoffeeSpillRage = true;
    public string[] coffeeNameTokens = new string[] { "coffee", "cup", "mug" };
    public Rage_Meter coffeeSpillTargetRageMeter;
    public string coffeeSpillTargetNpcId = NpcIds.Sandra;
    public float coffeeSpillNpcRadius = 2f;
    public float coffeeSpillMinImpactSpeed = 1.2f;
    public bool logCoffeeSpillDebug = true;

    [Header("Thrown Object Tracking")]
    public bool logThrownObjectDebug = false;

    [Header("Coffee Spill Snap (Button Interaction)")]
    [Tooltip("When the player presses E on the coffee spill signal while holding coffee, lock it into this pose.")]
    public bool snapCoffeeIntoSpilledState = true;
    [Tooltip("Treat coffeeSpillSnapLocalPosition as an offset from the coffee's current local position after snapping to the spill signal.")]
    public bool coffeeSpillPositionIsOffset = true;
    public Vector3 coffeeSpillSnapLocalPosition = Vector3.zero;
    public Vector3 coffeeSpillSnapLocalEulerAngles = new Vector3(0f, 0f, 90f);
    [Tooltip("Optional: if the held coffee already has a CoffeeSpillRage component, disable it so physics collisions don't double-trigger.")]
    public bool disableCoffeeSpillRageOnSnap = true;

    [Header("Grab Feel")]
    public float grabSnapTime = 0.15f; // How long to smoothly transition object to hand
    public AnimationCurve grabSnapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float grabForce = 20f;
    public float grabDamping = 10f;
    public float grabDistanceLimit = 3f;
    
    private Rigidbody heldObject;
    private float grabTransitionTimer;
    private Vector3 grabStartPosition;

    // Tracks which RageInteractionPropSignals we've already toast-hinted for.
    // Keys live for the lifetime of this PhysicsGrab (i.e. one play session).
    private readonly System.Collections.Generic.HashSet<int> hintedSignalIds =
        new System.Collections.Generic.HashSet<int>();

    void Awake()
    {
        if (coffeeSpillTargetRageMeter == null)
        {
            coffeeSpillTargetRageMeter = FindRageMeterByNpcId(coffeeSpillTargetNpcId);
        }
    }

    void OnEnable()
    {
        HudSignals.HintsEnabledChanged += HandleHintsEnabledChanged;
    }

    void OnDisable()
    {
        HudSignals.HintsEnabledChanged -= HandleHintsEnabledChanged;
    }

    private void HandleHintsEnabledChanged(bool enabled)
    {
        if (enabled)
        {
            hintedSignalIds.Clear();
        }
    }

    void Update()
    {
        if (ClipboardUIState.ShouldBlockGameplayInput)
        {
            return;
        }

        if (TryAutoSnapHeldFishToMicrowave())
        {
            return;
        }

        if (WasInteractPressed())
        {
            if (heldObject == null)
            {
                if (!TryInteractByRaycast())
                {
                    TryGrab();
                }
            }
            else
            {
                if (!TryInteractByRaycast())
                {
                    Drop();
                }
            }
        }

        if (heldObject != null && WasThrowPressed())
        {
            Throw();
        }
    }

    void FixedUpdate()
    {
        if (ClipboardUIState.ShouldBlockGameplayInput)
        {
            return;
        }

        if (heldObject != null)
        {
            MoveHeldObject();
        }
    }

    private static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    private static bool WasThrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return false;
#endif
    }

    void TryGrab()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Rigidbody rb = null;
        
        // First try: sphere cast for more forgiving detection
        bool hitSphere = Physics.SphereCast(ray, grabSphereRadius, out RaycastHit sphereHit, grabDistance);
        
        if (hitSphere)
        {
            rb = sphereHit.collider.attachedRigidbody;
            if (rb == null)
            {
                rb = sphereHit.collider.GetComponentInParent<Rigidbody>();
            }
        }
        
        // Fallback: try regular raycast for small objects the sphere missed
        if (rb == null)
        {
            bool hitRay = Physics.Raycast(ray, out RaycastHit rayHit, grabDistance);
            if (hitRay)
            {
                rb = rayHit.collider.attachedRigidbody;
                if (rb == null)
                {
                    rb = rayHit.collider.GetComponentInParent<Rigidbody>();
                }
            }
        }

        if (rb != null && !IsLockedInMicrowave(rb) && !IsLockedInCoffeeSpill(rb))
        {
            heldObject = rb;
            grabStartPosition = heldObject.position;
            grabTransitionTimer = 0f;

            // Unlock physics if this prop was locked at start
            if (heldObject.TryGetComponent<KinematicAtStart>(out KinematicAtStart kinematicStart))
            {
                kinematicStart.UnlockPhysics();
            }
            
            // Store initial rigidbody state before modifying
            if (!heldObject.TryGetComponent<HeldObjectState>(out _))
            {
                HeldObjectState state = heldObject.gameObject.AddComponent<HeldObjectState>();
                state.originMass = heldObject.mass;
                state.originDrag = heldObject.linearDamping;
                state.originAngularDrag = heldObject.angularDamping;
                state.originIsKinematic = heldObject.isKinematic;
                state.originUseGravity = heldObject.useGravity;
                state.originCollisionMode = heldObject.collisionDetectionMode;
                state.originInterpolation = heldObject.interpolation;
            }
            
            // Make non-kinematic for physics-based holding to avoid clipping and feel better
            heldObject.isKinematic = false;
            heldObject.useGravity = false;
            heldObject.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            heldObject.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Ensure floor detection is active
            if (!heldObject.TryGetComponent<PropFloorDetection>(out _))
            {
                PropFloorDetection floorDetection = heldObject.gameObject.AddComponent<PropFloorDetection>();
                floorDetection.SetSpawnPoint(heldObject.position, heldObject.rotation);
            }
            
            // Ensure physics stabilization
            if (!heldObject.TryGetComponent<PropPhysicsStabilizer>(out _))
            {
                heldObject.gameObject.AddComponent<PropPhysicsStabilizer>();
            }
        }
    }

    bool TryInteractByRaycast()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        // Use sphere cast for more reliable interaction detection
        // Try sphere cast first, then fallback to raycast for small signals
        bool hitSignal = Physics.SphereCast(ray, grabSphereRadius * 0.5f, out hit, grabDistance);
        
        if (!hitSignal)
        {
            // Fallback: regular raycast for tiny signal objects
            hitSignal = Physics.Raycast(ray, out hit, grabDistance);
        }
        
        if (!hitSignal)
        {
            return false;
        }

        RageInteractionPropSignal signal = hit.collider.GetComponentInParent<RageInteractionPropSignal>();
        if (signal == null)
        {
            return false;
        }

        // Microwave: only allow interaction when holding the right fish.
        if (signal.SignalEventType == RageSignalEventType.MicrowaveFish)
        {
            if (requireFishForMicrowaveInteraction)
            {
                if (heldObject == null)
                {
                    return false;
                }

                if (!IsFishObject(heldObject))
                {
                    return false;
                }
            }

            if (heldObject == null || !IsFishObject(heldObject))
            {
                return false;
            }

            LockFishIntoMicrowave(heldObject, signal);
            heldObject = null;
            return true;
        }

        // Coffee spill: allow interaction when holding coffee.
        if (signal.SignalEventType == RageSignalEventType.SpillDrinkOnDesk ||
            signal.SignalEventType == RageSignalEventType.FinalBossSpillDrinkOnDesk)
        {
            if (!enableCoffeeSpillRage)
            {
                return false;
            }

            // Support two use-cases:
            // 1) Player is holding coffee and presses E to spill it.
            // 2) Player is not holding coffee and presses E on the spill interaction (scene cup).
            Rigidbody coffeeBody = null;
            bool usingHeldObject = false;

            if (heldObject != null && IsCoffeeObject(heldObject))
            {
                coffeeBody = heldObject;
                usingHeldObject = true;
            }
            else
            {
                coffeeBody = hit.collider.GetComponentInParent<Rigidbody>();
                if (coffeeBody == null)
                {
                    coffeeBody = signal.GetComponentInParent<Rigidbody>();
                }
            }

            if (coffeeBody == null || !IsCoffeeObject(coffeeBody))
            {
                return false;
            }

            // Snap the rigidbody into the spilled pose and then raise the rage signal.
            TrySnapHeldCoffeeIntoSpilledState(signal, coffeeBody);
            signal.Interact();
            if (usingHeldObject)
            {
                heldObject = null;
            }
            return true;
        }

        // Knock over: push/tip the target rigidbody when pressing E.
        if (signal.SignalEventType == RageSignalEventType.KnockOver)
        {
            bool knockedOver = signal.TryPerformKnockOver(
                cam != null ? cam.transform : null,
                hit.collider,
                hit.point);
            if (knockedOver)
            {
                return true;
            }
        }

        if (signal.SignalEventType == RageSignalEventType.MakeLoudNoise ||
            signal.SignalEventType == RageSignalEventType.UnplugDevice)
        {
            signal.Interact();
            return true;
        }

        return false;
    }

    void TrySnapHeldCoffeeIntoSpilledState(RageInteractionPropSignal spillSignal, Rigidbody coffeeBody)
    {
        if (coffeeBody == null || spillSignal == null)
        {
            return;
        }

        if (!snapCoffeeIntoSpilledState)
        {
            return;
        }

        // Avoid parenting the transform to itself (possible if the signal is on the same GameObject as the rigidbody).
        if (coffeeBody.transform != spillSignal.transform)
        {
            coffeeBody.transform.SetParent(spillSignal.transform, true);
        }

        Vector3 spillTargetLocalPosition = coffeeSpillPositionIsOffset
            ? coffeeBody.transform.localPosition + coffeeSpillSnapLocalPosition
            : coffeeSpillSnapLocalPosition;
        Quaternion targetLocalRotation = Quaternion.Euler(coffeeSpillSnapLocalEulerAngles);

        if (!coffeeBody.isKinematic)
        {
            coffeeBody.linearVelocity = Vector3.zero;
            coffeeBody.angularVelocity = Vector3.zero;
        }
        coffeeBody.useGravity = false;
        coffeeBody.isKinematic = true;

        bool playedJuiceAnimation = false;
        CoffeeSpillJuiceAnimator spillJuiceAnimator = coffeeBody.GetComponent<CoffeeSpillJuiceAnimator>();
        if (spillJuiceAnimator == null)
        {
            spillJuiceAnimator = coffeeBody.gameObject.AddComponent<CoffeeSpillJuiceAnimator>();
        }

        if (spillJuiceAnimator != null)
        {
            Transform cameraTransform = cam != null ? cam.transform : null;
            playedJuiceAnimation = spillJuiceAnimator.TryPlaySpill(
                spillTargetLocalPosition,
                targetLocalRotation,
                cameraTransform);
        }

        if (!playedJuiceAnimation)
        {
            coffeeBody.transform.localPosition = spillTargetLocalPosition;
            coffeeBody.transform.localRotation = targetLocalRotation;
        }
        if (disableCoffeeSpillRageOnSnap)
        {
            CoffeeSpillRage spillRage = coffeeBody.GetComponent<CoffeeSpillRage>();
            if (spillRage != null)
            {
                spillRage.enabled = false;
            }
        }

        // Prevent the snapped/spilled coffee from being re-grabbed later.
        CoffeeSpillLockedItem lockState = coffeeBody.GetComponent<CoffeeSpillLockedItem>();
        if (lockState == null)
        {
            lockState = coffeeBody.gameObject.AddComponent<CoffeeSpillLockedItem>();
        }
        lockState.Lock();
    }

    bool TryAutoSnapHeldFishToMicrowave()
    {
        if (!autoSnapFishIntoMicrowave || heldObject == null || !IsFishObject(heldObject))
        {
            return false;
        }

        RageInteractionPropSignal closestMicrowave = FindClosestMicrowaveSignal(heldObject.position);
        if (closestMicrowave == null)
        {
            return false;
        }

        float maxDistanceSq = fishAutoSnapDistance * fishAutoSnapDistance;
        float distanceSq = (closestMicrowave.transform.position - heldObject.position).sqrMagnitude;
        if (distanceSq > maxDistanceSq)
        {
            return false;
        }

        LockFishIntoMicrowave(heldObject, closestMicrowave);
        heldObject = null;
        return true;
    }

    public bool TrySnapThrownFishToMicrowave(Rigidbody fishBody, float snapDistance)
    {
        if (fishBody == null || !IsFishObject(fishBody) || IsLockedInMicrowave(fishBody))
        {
            return false;
        }

        RageInteractionPropSignal closestMicrowave = FindClosestMicrowaveSignal(fishBody.position);
        if (closestMicrowave == null)
        {
            return false;
        }

        float maxDistanceSq = snapDistance * snapDistance;
        float distanceSq = (closestMicrowave.transform.position - fishBody.position).sqrMagnitude;
        if (distanceSq > maxDistanceSq)
        {
            return false;
        }

        LockFishIntoMicrowave(fishBody, closestMicrowave);
        return true;
    }

    RageInteractionPropSignal FindClosestMicrowaveSignal(Vector3 fromPosition)
    {
        RageInteractionPropSignal[] signals =
            FindObjectsByType<RageInteractionPropSignal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        RageInteractionPropSignal best = null;
        float bestDistanceSq = float.MaxValue;

        for (int i = 0; i < signals.Length; i++)
        {
            RageInteractionPropSignal signal = signals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.SignalEventType != RageSignalEventType.MicrowaveFish)
            {
                continue;
            }

            float d = (signal.transform.position - fromPosition).sqrMagnitude;
            if (d < bestDistanceSq)
            {
                bestDistanceSq = d;
                best = signal;
            }
        }

        return best;
    }

    void LockFishIntoMicrowave(Rigidbody fishBody, RageInteractionPropSignal microwaveSignal)
    {
        fishBody.transform.SetParent(microwaveSignal.transform, true);
        Vector3 targetLocalPosition = fishSnapLocalPosition;
        Quaternion targetLocalRotation = Quaternion.Euler(fishSnapLocalEulerAngles);

        fishBody.linearVelocity = Vector3.zero;
        fishBody.angularVelocity = Vector3.zero;
        fishBody.useGravity = false;
        fishBody.isKinematic = true;

        bool playedInsertAnimation = false;
        MicrowaveFishJuiceAnimator fishJuiceAnimator = fishBody.GetComponent<MicrowaveFishJuiceAnimator>();
        if (fishJuiceAnimator == null)
        {
            fishJuiceAnimator = fishBody.gameObject.AddComponent<MicrowaveFishJuiceAnimator>();
        }

        if (fishJuiceAnimator != null)
        {
            Transform cameraTransform = cam != null ? cam.transform : null;
            playedInsertAnimation = fishJuiceAnimator.TryPlayInsert(
                targetLocalPosition,
                targetLocalRotation,
                cameraTransform);
        }

        if (!playedInsertAnimation)
        {
            fishBody.transform.localPosition = targetLocalPosition;
            fishBody.transform.localRotation = targetLocalRotation;
        }

        MicrowaveLockedItem lockState = fishBody.GetComponent<MicrowaveLockedItem>();
        if (lockState == null)
        {
            lockState = fishBody.gameObject.AddComponent<MicrowaveLockedItem>();
        }

        lockState.Lock();
        microwaveSignal.Interact();
    }

    bool IsFishObject(Rigidbody rb)
    {
        if (rb == null)
        {
            return false;
        }

        RageInteractionPropSignal fishSignal = rb.GetComponentInParent<RageInteractionPropSignal>();
        if (fishSignal != null && fishSignal.SignalEventType == RageSignalEventType.MicrowaveFishItem)
        {
            return true;
        }

        return rb.name.ToLowerInvariant().Contains(fishNameToken.ToLowerInvariant());
    }

    bool IsCoffeeObject(Rigidbody rb)
    {
        if (rb == null)
        {
            return false;
        }

        string loweredName = rb.name.ToLowerInvariant();
        for (int i = 0; i < coffeeNameTokens.Length; i++)
        {
            string token = coffeeNameTokens[i];
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (loweredName.Contains(token.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    Rage_Meter FindRageMeterByNpcId(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return null;
        }

        Rage_Meter[] meters =
            FindObjectsByType<Rage_Meter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < meters.Length; i++)
        {
            Rage_Meter meter = meters[i];
            if (meter != null && meter.NpcSignalId == npcId)
            {
                return meter;
            }
        }

        return null;
    }

    void ArmCoffeeSpillIfNeeded(Rigidbody rb)
    {
        if (!enableCoffeeSpillRage || rb == null || !IsCoffeeObject(rb))
        {
            return;
        }

        Rage_Meter targetMeter = coffeeSpillTargetRageMeter;
        if (targetMeter == null)
        {
            targetMeter = FindRageMeterByNpcId(coffeeSpillTargetNpcId);
            coffeeSpillTargetRageMeter = targetMeter;
        }

        if (targetMeter == null)
        {
            if (logCoffeeSpillDebug)
            {
                Debug.LogWarning("[PhysicsGrab] Coffee spill was not armed because no target Rage_Meter was found.", this);
            }

            return;
        }

        CoffeeSpillRage spill = rb.GetComponent<CoffeeSpillRage>();
        if (spill == null)
        {
            spill = rb.gameObject.AddComponent<CoffeeSpillRage>();
        }

        spill.Arm(
            targetMeter,
            coffeeSpillTargetNpcId,
            coffeeSpillNpcRadius,
            coffeeSpillMinImpactSpeed,
            RageSignalIds.SpillDrinkOnDesk,
            logCoffeeSpillDebug);
    }

    void ArmThrownObjectMarker(Rigidbody rb)
    {
        if (rb == null)
        {
            return;
        }

        ProjectileHitNpcRage projectileMarker = rb.GetComponent<ProjectileHitNpcRage>();
        if (projectileMarker == null)
        {
            projectileMarker = rb.gameObject.AddComponent<ProjectileHitNpcRage>();
        }

        projectileMarker.Arm(logThrownObjectDebug);
    }

    bool IsLockedInMicrowave(Rigidbody rb)
    {
        if (rb == null)
        {
            return false;
        }

        MicrowaveLockedItem lockState = rb.GetComponent<MicrowaveLockedItem>();
        return lockState != null && lockState.IsLocked;
    }

    bool IsLockedInCoffeeSpill(Rigidbody rb)
    {
        if (rb == null)
        {
            return false;
        }

        CoffeeSpillLockedItem lockState = rb.GetComponent<CoffeeSpillLockedItem>();
        return lockState != null && lockState.IsLocked;
    }
    
    void RestoreHeldObjectProperties()
    {
        if (heldObject == null)
            return;
        
        if (heldObject.TryGetComponent<HeldObjectState>(out HeldObjectState state))
        {
            heldObject.mass = state.originMass;
            heldObject.linearDamping = state.originDrag;
            heldObject.angularDamping = state.originAngularDrag;
            heldObject.isKinematic = state.originIsKinematic;
            heldObject.useGravity = state.originUseGravity;
            
            // Cannot set continuous on a kinematic rigidbody, handle order carefully
            if (state.originIsKinematic)
            {
                heldObject.collisionDetectionMode = state.originCollisionMode;
                heldObject.isKinematic = true;
            }
            else
            {
                heldObject.isKinematic = false;
                heldObject.collisionDetectionMode = state.originCollisionMode;
            }
            
            heldObject.interpolation = state.originInterpolation;
            Object.Destroy(state);
        }
    }

    void MoveHeldObject()
    {
        if (heldObject == null)
            return;
            
        Vector3 targetPos = holdPoint.position;
        Vector3 camPos = cam.transform.position;
        Vector3 dirToHold = targetPos - camPos;
        float distToHold = dirToHold.magnitude;

        // Prevent putting object outside the map / through walls by raycasting from camera to hold point
        RaycastHit[] hits = Physics.RaycastAll(camPos, dirToHold.normalized, distToHold, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float closestHitDist = distToHold;
        bool hitObstacle = false;
        
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.attachedRigidbody == heldObject || hit.collider.transform.IsChildOf(heldObject.transform))
                continue;
                
            if (hit.distance < closestHitDist)
            {
                closestHitDist = hit.distance;
                hitObstacle = true;
            }
        }
        
        if (hitObstacle)
        {
            // Bring target pos slightly closer to camera if hitting a wall
            float hitBuffer = 0.2f;
            targetPos = camPos + dirToHold.normalized * Mathf.Max(0f, closestHitDist - hitBuffer);
        }

        // Distance check - drop if the object gets stuck behind something and player walks away
        float distance = Vector3.Distance(heldObject.position, targetPos);
        if (distance > grabDistanceLimit)
        {
            Drop();
            return;
        }

        // Smoothly transition to hand during grab snap time
        if (grabTransitionTimer < grabSnapTime)
        {
            grabTransitionTimer += Time.deltaTime;
            float t = grabSnapEase.Evaluate(Mathf.Clamp01(grabTransitionTimer / grabSnapTime));
            Vector3 snappedPos = Vector3.Lerp(grabStartPosition, targetPos, t);
            heldObject.MovePosition(snappedPos);
            heldObject.linearVelocity = Vector3.zero;
        }
        else
        {
            // Physics movement to feel responsive but respect collisions
            Vector3 velocityTarget = (targetPos - heldObject.position) * grabForce;
            heldObject.linearVelocity = velocityTarget;
            
            // Dampen angular velocity to prevent wild spinning when dragging along walls
            heldObject.angularVelocity = Vector3.Lerp(heldObject.angularVelocity, Vector3.zero, Time.fixedDeltaTime * grabDamping);
        }
    }

    void Drop()
    {
        if (heldObject != null)
        {
            Vector3 holdVelocity = Vector3.ClampMagnitude(heldObject.linearVelocity, 2f);
            holdVelocity += Vector3.down * 0.5f;

            RestoreHeldObjectProperties();
            heldObject.useGravity = true;
            heldObject.isKinematic = false;
            heldObject.linearVelocity = holdVelocity;
        }
        heldObject = null;
        grabTransitionTimer = 0f;
    }

    void Throw()
    {
        Rigidbody objectToThrow = heldObject;
        if (objectToThrow == null)
        {
            heldObject = null;
            grabTransitionTimer = 0f;
            return;
        }
        
        RestoreHeldObjectProperties();
        objectToThrow.useGravity = true;
        objectToThrow.isKinematic = false;
        
        // Apply throw force based on camera direction and up
        Vector3 throwVelocity = cam.transform.forward * throwForce;
        // Add slight upward bias to make throws feel punchier
        throwVelocity += cam.transform.up * (throwForce * 0.3f);
        throwVelocity = Vector3.ClampMagnitude(throwVelocity, 50f);
        objectToThrow.linearVelocity = throwVelocity;
        
        heldObject = null;
        grabTransitionTimer = 0f;

        if (allowThrownFishMicrowaveSnap && objectToThrow != null && IsFishObject(objectToThrow))
        {
            FishMicrowaveProjectile snapHelper = objectToThrow.GetComponent<FishMicrowaveProjectile>();
            if (snapHelper == null)
            {
                snapHelper = objectToThrow.gameObject.AddComponent<FishMicrowaveProjectile>();
            }

            snapHelper.Arm(this, objectToThrow, thrownFishSnapDistance, thrownFishSnapMinSpeed, thrownFishSnapLifetime);
        }

        ArmThrownObjectMarker(objectToThrow);
    }

    void LateUpdate()
    {
        if (!publishReticleHoverState)
        {
            return;
        }

        // Reticle hover state is pushed to the HUD via a static signal bus
        // so PhysicsGrab doesn't need a direct dependency on the HUD doc.
        HudSignals.SetReticleState(EvaluateReticleHoverState());
    }

    ReticleState EvaluateReticleHoverState()
    {
        if (cam == null)
        {
            return ReticleState.Idle;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, grabDistance))
        {
            return ReticleState.Idle;
        }

        RageInteractionPropSignal signal = hit.collider.GetComponentInParent<RageInteractionPropSignal>();
        if (signal != null)
        {
            TryEmitFirstHoverHint(signal);
            if (signal.SignalEventType == RageSignalEventType.MicrowaveFish)
            {
                if (!requireFishForMicrowaveInteraction || (heldObject != null && IsFishObject(heldObject)))
                {
                    return ReticleState.Interact;
                }
            }
            else if (signal.SignalEventType == RageSignalEventType.SpillDrinkOnDesk ||
                     signal.SignalEventType == RageSignalEventType.FinalBossSpillDrinkOnDesk)
            {
                Rigidbody coffeeRb = hit.collider.GetComponent<Rigidbody>();
                bool canInteract =
                    enableCoffeeSpillRage &&
                    ((heldObject != null && IsCoffeeObject(heldObject)) ||
                     (coffeeRb != null && IsCoffeeObject(coffeeRb)));

                if (canInteract)
                {
                    return ReticleState.Interact;
                }
            }
            else
            {
                if (signal.SignalEventType == RageSignalEventType.KnockOver &&
                    signal.CanPerformKnockOver(hit.collider))
                {
                    return ReticleState.Interact;
                }

                if (signal.SignalEventType == RageSignalEventType.MakeLoudNoise ||
                    signal.SignalEventType == RageSignalEventType.UnplugDevice)
                {
                    return ReticleState.Interact;
                }
            }
        }

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null)
        {
            rb = hit.collider.GetComponentInParent<Rigidbody>();
        }
        if (rb != null && !IsLockedInMicrowave(rb) && !IsLockedInCoffeeSpill(rb))
        {
            return ReticleState.Grab;
        }

        return ReticleState.Idle;
    }

    void TryEmitFirstHoverHint(RageInteractionPropSignal signal)
    {
        if (signal == null) return;
        if (!HudSignals.HintsEnabled) return;
        int id = signal.GetInstanceID();
        if (!hintedSignalIds.Add(id)) return;
        string hint = HintTextFor(signal.SignalEventType);
        if (!string.IsNullOrEmpty(hint))
        {
            HudSignals.RequestHint(hint);
        }
    }

    static string HintTextFor(RageSignalEventType type)
    {
        switch (type)
        {
            case RageSignalEventType.SpillDrinkOnDesk:
            case RageSignalEventType.FinalBossSpillDrinkOnDesk:
                return "Press E to spill drink";
            case RageSignalEventType.MicrowaveFish:
            case RageSignalEventType.MicrowaveFishItem:
                return "Bring fish, press E";
            case RageSignalEventType.StealObject:
                return "Grab it and move away";
            case RageSignalEventType.KnockOver:
                return "Press E to knock over";
            case RageSignalEventType.BringObjectNear:
                return "Carry near target";
            case RageSignalEventType.HitNpcWithProjectile:
                return "Throw object at NPC";
            case RageSignalEventType.MakeLoudNoise:
                return "Press E for loud noise";
            case RageSignalEventType.UnplugDevice:
                return "Press E to unplug";
            default:
                return string.Empty;
        }
    }
}
