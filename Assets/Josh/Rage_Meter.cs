using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Rage_Meter : MonoBehaviour
{
    [Header("Rage Setup")]
    [SerializeField, Min(1)] private int requiredSignals = 3;
    [SerializeField] private bool countUniqueSignalsOnly = true;
    [SerializeField] private bool canFlipOutOnlyOnce = true;

    [Header("Signal Listening")]
    [SerializeField] private bool listenToGlobalSignalHub = true;
    [Tooltip("Optional: only accept global signals targeting this id. Leave empty to accept untargeted signals.")]
    [SerializeField] private string npcSignalId;

    [Header("Flip Out Trigger")]
    [SerializeField] private MonoBehaviour flipOutReceiver;
    [SerializeField] private string flipOutMethodName = "FlipOut";
    [SerializeField] private UnityEvent onFlipOut;

    [Header("Flip Out Physics Blast")]
    [SerializeField] private bool emitFlipOutPhysicsBlast = true;
    [SerializeField, Min(0.1f)] private float flipOutBlastRadius = 6f;
    [SerializeField] private float flipOutBlastForce = 14f;
    [SerializeField] private float flipOutBlastUpwardsModifier = 1.5f;
    [SerializeField] private float flipOutBlastRandomTorque = 10f;
    [SerializeField] private ForceMode flipOutBlastForceMode = ForceMode.Impulse;

    [Header("Visual Rage Icons")]
    [SerializeField] private Transform iconAnchor;
    [SerializeField] private SpriteRenderer rageFaceRenderer;
    [SerializeField] private Sprite neutralFaceSprite;
    [SerializeField] private Sprite[] angerFaceSprites = new Sprite[3];
    [SerializeField] private Sprite flipOutFaceSprite;
    [SerializeField] private float iconVerticalOffset = 2f;
    [SerializeField] private Vector3 rageFaceScale = new Vector3(0.25f, 0.25f, 1f);

    [Header("Debug Testing")]
    [SerializeField] private bool enableDebugInput;
    [SerializeField] private KeyCode addRageKey = KeyCode.Equals;
    [SerializeField] private KeyCode removeRageKey = KeyCode.Minus;
    [SerializeField] private KeyCode resetRageKey = KeyCode.Backspace;

    public int CurrentRage => currentRage;
    public int RequiredSignals => requiredSignals;
    public bool IsFlippedOut => isFlippedOut;

    private readonly HashSet<string> receivedSignalIds = new HashSet<string>();

    private int currentRage;
    private bool isFlippedOut;

    private void Awake()
    {
        EnsureAnchor();
        BuildIcons();
        RefreshIcons();
    }

    private void OnEnable()
    {
        if (listenToGlobalSignalHub)
        {
            RageSignalHub.SignalRaised += HandleGlobalSignal;
        }
    }

    private void OnDisable()
    {
        if (listenToGlobalSignalHub)
        {
            RageSignalHub.SignalRaised -= HandleGlobalSignal;
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
        AddSignal(signalId);
    }

    public void AddSignal(string signalId)
    {
        if (isFlippedOut && canFlipOutOnlyOnce)
        {
            return;
        }

        if (countUniqueSignalsOnly)
        {
            if (string.IsNullOrWhiteSpace(signalId))
            {
                signalId = "anonymous_" + (currentRage + 1);
            }

            if (!receivedSignalIds.Add(signalId))
            {
                return;
            }
        }

        currentRage = Mathf.Min(currentRage + 1, requiredSignals);
        RefreshIcons();

        if (currentRage >= requiredSignals)
        {
            EnterRageState();
        }
    }

    public void ResetRage()
    {
        currentRage = 0;
        isFlippedOut = false;
        receivedSignalIds.Clear();
        RefreshIcons();
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

        RefreshIcons();
    }

    [ContextMenu("Debug/Add 1 Rage")]
    private void DebugAddOneRage()
    {
        AddSignal("debug_" + Time.frameCount + "_" + Random.Range(0, 100000));
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

    private void EnterRageState()
    {
        if (isFlippedOut && canFlipOutOnlyOnce)
        {
            return;
        }

        isFlippedOut = true;
        RefreshIcons();
        onFlipOut?.Invoke();

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

    private void EnsureAnchor()
    {
        if (iconAnchor != null)
        {
            return;
        }

        GameObject anchorObject = new GameObject("RageIconAnchor");
        anchorObject.transform.SetParent(transform);
        anchorObject.transform.localPosition = Vector3.up * iconVerticalOffset;
        anchorObject.transform.localRotation = Quaternion.identity;
        iconAnchor = anchorObject.transform;
    }

    private void BuildIcons()
    {
        if (iconAnchor == null)
        {
            return;
        }

        if (rageFaceRenderer == null)
        {
            Transform existing = iconAnchor.Find("RageFaceVisual");
            if (existing != null)
            {
                rageFaceRenderer = existing.GetComponent<SpriteRenderer>();
            }

            if (rageFaceRenderer == null)
            {
                GameObject faceObject = new GameObject("RageFaceVisual");
                faceObject.transform.SetParent(iconAnchor);
                faceObject.transform.localPosition = Vector3.zero;
                faceObject.transform.localRotation = Quaternion.identity;
                faceObject.transform.localScale = rageFaceScale;
                rageFaceRenderer = faceObject.AddComponent<SpriteRenderer>();
            }
        }

        rageFaceRenderer.transform.localScale = rageFaceScale;
    }

    private void RefreshIcons()
    {
        if (rageFaceRenderer == null)
        {
            return;
        }

        Sprite desiredSprite;

        if (isFlippedOut && flipOutFaceSprite != null)
        {
            desiredSprite = flipOutFaceSprite;
        }
        else if (currentRage <= 0 || angerFaceSprites == null || angerFaceSprites.Length == 0)
        {
            desiredSprite = neutralFaceSprite;
        }
        else
        {
            float normalizedRage = (float)currentRage / Mathf.Max(1, requiredSignals);
            int stageIndex = Mathf.CeilToInt(normalizedRage * angerFaceSprites.Length) - 1;
            stageIndex = Mathf.Clamp(stageIndex, 0, angerFaceSprites.Length - 1);
            desiredSprite = angerFaceSprites[stageIndex] != null ? angerFaceSprites[stageIndex] : neutralFaceSprite;
        }

        rageFaceRenderer.sprite = desiredSprite;
        rageFaceRenderer.enabled = desiredSprite != null;
    }

    private void OnValidate()
    {
        if (requiredSignals < 1)
        {
            requiredSignals = 1;
        }

        currentRage = Mathf.Clamp(currentRage, 0, requiredSignals);

        if (iconAnchor != null && rageFaceRenderer != null)
        {
            rageFaceRenderer.transform.localPosition = Vector3.zero;
            rageFaceRenderer.transform.localScale = rageFaceScale;
            RefreshIcons();
        }
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
