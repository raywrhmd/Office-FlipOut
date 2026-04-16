using UnityEngine;
using OfficeFlipOut.UI;

public class PhysicsGrab : MonoBehaviour
{
    public Camera cam;
    public Transform holdPoint;

    public float grabDistance = 3f;
    public float throwForce = 10f;

    [Header("Aiming Reticle")]
    public bool showAimReticle = true;
    public float reticleSize = 10f;
    public float reticleThickness = 2f;
    public Color defaultReticleColor = Color.white;
    public Color interactReticleColor = new Color(0.3f, 1f, 0.3f, 1f);

    [Header("Rage Interaction")]
    public bool requireFishForMicrowaveInteraction = true;
    public string fishNameToken = "fish";

    [Header("Microwave Auto Snap")]
    public bool autoSnapFishIntoMicrowave = true;
    public float fishAutoSnapDistance = 1f;
    public Vector3 fishSnapLocalPosition = new Vector3(0f, 0.08f, 0f);
    public Vector3 fishSnapLocalEulerAngles = Vector3.zero;

    [Header("Coffee Spill Rage")]
    public bool enableCoffeeSpillRage = true;
    public string[] coffeeNameTokens = new string[] { "coffee", "cup", "mug" };
    public Rage_Meter coffeeSpillTargetRageMeter;
    public string coffeeSpillTargetNpcId = "npc_1";
    public float coffeeSpillNpcRadius = 2f;
    public float coffeeSpillMinImpactSpeed = 1.2f;
    public bool logCoffeeSpillDebug = true;

    [Header("Coffee Spill Snap (Button Interaction)")]
    [Tooltip("When the player presses E on the coffee spill signal while holding coffee, lock it into this pose.")]
    public bool snapCoffeeIntoSpilledState = true;
    public Vector3 coffeeSpillSnapLocalPosition = Vector3.zero;
    public Vector3 coffeeSpillSnapLocalEulerAngles = Vector3.zero;
    [Tooltip("Optional: if the held coffee already has a CoffeeSpillRage component, disable it so physics collisions don't double-trigger.")]
    public bool disableCoffeeSpillRageOnSnap = true;

    private Rigidbody heldObject;

    void Awake()
    {
        if (coffeeSpillTargetRageMeter == null)
        {
            coffeeSpillTargetRageMeter = FindRageMeterByNpcId(coffeeSpillTargetNpcId);
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

        if (Input.GetKeyDown(KeyCode.E))
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

        if (heldObject != null && Input.GetMouseButtonDown(0))
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

    void TryGrab()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();

            if (rb != null && !IsLockedInMicrowave(rb) && !IsLockedInCoffeeSpill(rb))
            {
                heldObject = rb;

                heldObject.useGravity = false;
                heldObject.isKinematic = false;
                heldObject.linearVelocity = Vector3.zero;
                heldObject.angularVelocity = Vector3.zero;
            }
        }
    }

    bool TryInteractByRaycast()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, grabDistance))
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

                string heldName = heldObject.name.ToLowerInvariant();
                if (!heldName.Contains(fishNameToken.ToLowerInvariant()))
                {
                    return false;
                }
            }

            signal.Interact();
            return true;
        }

        // Coffee spill: allow interaction when holding coffee.
        if (signal.SignalEventType == RageSignalEventType.SpillDrinkOnDesk)
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
        coffeeBody.transform.localPosition = coffeeSpillSnapLocalPosition;
        coffeeBody.transform.localRotation = Quaternion.Euler(coffeeSpillSnapLocalEulerAngles);

        coffeeBody.linearVelocity = Vector3.zero;
        coffeeBody.angularVelocity = Vector3.zero;
        coffeeBody.useGravity = false;
        coffeeBody.isKinematic = true;

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

    RageInteractionPropSignal FindClosestMicrowaveSignal(Vector3 fromPosition)
    {
        RageInteractionPropSignal[] signals =
            FindObjectsByType<RageInteractionPropSignal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        RageInteractionPropSignal best = null;
        float bestDistanceSq = float.MaxValue;

        for (int i = 0; i < signals.Length; i++)
        {
            RageInteractionPropSignal signal = signals[i];
            if (signal == null || signal.SignalEventType != RageSignalEventType.MicrowaveFish)
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
        fishBody.transform.localPosition = fishSnapLocalPosition;
        fishBody.transform.localRotation = Quaternion.Euler(fishSnapLocalEulerAngles);

        fishBody.linearVelocity = Vector3.zero;
        fishBody.angularVelocity = Vector3.zero;
        fishBody.useGravity = false;
        fishBody.isKinematic = true;

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

    void MoveHeldObject()
    {
        Vector3 moveDir = holdPoint.position - heldObject.position;

        heldObject.linearVelocity = moveDir * 10f;
    }

    void Drop()
    {
        heldObject.useGravity = true;
        heldObject.isKinematic = false;
        heldObject = null;
    }

    void Throw()
    {
        heldObject.useGravity = true;
        heldObject.isKinematic = false;
        heldObject.AddForce(cam.transform.forward * throwForce, ForceMode.Impulse);
        heldObject = null;
    }

    void OnGUI()
    {
        if (!showAimReticle || ClipboardUIState.IsOpen)
        {
            return;
        }

        Color color = GetReticleColor();
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;

        float halfSize = reticleSize * 0.5f;
        float halfThickness = reticleThickness * 0.5f;
        Color previous = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(
            new Rect(centerX - halfSize, centerY - halfThickness, reticleSize, reticleThickness),
            Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(centerX - halfThickness, centerY - halfSize, reticleThickness, reticleSize),
            Texture2D.whiteTexture);

        GUI.color = previous;
    }

    Color GetReticleColor()
    {
        if (cam == null)
        {
            return defaultReticleColor;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, grabDistance))
        {
            return defaultReticleColor;
        }

        RageInteractionPropSignal signal = hit.collider.GetComponentInParent<RageInteractionPropSignal>();
        if (signal != null)
        {
            // Microwave: show interact only when holding valid fish.
            if (signal.SignalEventType == RageSignalEventType.MicrowaveFish)
            {
                if (!requireFishForMicrowaveInteraction || (heldObject != null && IsFishObject(heldObject)))
                {
                    return interactReticleColor;
                }
            }
            // Coffee spill: show interact only when holding coffee.
            else if (signal.SignalEventType == RageSignalEventType.SpillDrinkOnDesk)
            {
                Rigidbody coffeeRb = hit.collider.GetComponent<Rigidbody>();
                bool canInteract =
                    enableCoffeeSpillRage &&
                    ((heldObject != null && IsCoffeeObject(heldObject)) ||
                     (coffeeRb != null && IsCoffeeObject(coffeeRb)));

                if (canInteract)
                {
                    return interactReticleColor;
                }
            }
            else
            {
                // Other signals are not currently handled by prototype input.
            }
        }

        Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
        if (rb != null && !IsLockedInMicrowave(rb) && !IsLockedInCoffeeSpill(rb))
        {
            return interactReticleColor;
        }

        return defaultReticleColor;
    }
}
