using System;
using System.Collections.Generic;
using OfficeFlipOut.Data;
using OfficeFlipOut.Systems;
using OfficeFlipOut.UI.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OfficeFlipOut.UI.Hud
{
    /// <summary>
    /// Possible reticle / interaction states broadcast by gameplay systems
    /// (currently <see cref="PhysicsGrab"/>) and consumed by the HUD.
    /// </summary>
    public enum ReticleState
    {
        Idle = 0,
        Grab = 1,
        Interact = 2,
    }

    /// <summary>
    /// Static event bus for HUD-facing signals so gameplay systems don't have
    /// to know there's a HUD at all. Mirrors the pattern in <see cref="ClipboardUIState"/>.
    /// </summary>
    public static class HudSignals
    {
        private const string HintsEnabledPrefKey = "office_flipout.ui.hints_enabled";
        private const string ReduceMotionPrefKey = "office_flipout.ui.reduce_motion";
        public static ReticleState CurrentReticleState { get; private set; } = ReticleState.Idle;
        public static bool HintsEnabled { get; private set; } =
            PlayerPrefs.GetInt(HintsEnabledPrefKey, 1) == 1;
        public static bool ReduceMotion { get; private set; } =
            PlayerPrefs.GetInt(ReduceMotionPrefKey, 0) == 1;

        public static event Action<ReticleState> ReticleStateChanged;
        public static event Action<string> HintRequested;
        public static event Action<bool> HintsEnabledChanged;
        public static event Action<bool> ReduceMotionChanged;

        public static void SetReticleState(ReticleState state)
        {
            if (CurrentReticleState == state) return;
            CurrentReticleState = state;
            ReticleStateChanged?.Invoke(state);
        }

        public static void RequestHint(string message)
        {
            if (!HintsEnabled) return;
            if (string.IsNullOrWhiteSpace(message)) return;
            HintRequested?.Invoke(message);
        }

        public static void SetHintsEnabled(bool enabled)
        {
            if (HintsEnabled == enabled) return;
            HintsEnabled = enabled;
            PlayerPrefs.SetInt(HintsEnabledPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            HintsEnabledChanged?.Invoke(enabled);
        }

        public static void SetReduceMotion(bool enabled)
        {
            if (ReduceMotion == enabled) return;
            ReduceMotion = enabled;
            PlayerPrefs.SetInt(ReduceMotionPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ReduceMotionChanged?.Invoke(enabled);
        }
    }

    /// <summary>
    /// Drives the screen-space HUD: reticle, per-NPC rage roster, hint toast,
    /// and win overlay. Owns its own <see cref="UIDocument"/> so it stacks
    /// independently of the clipboard / menus.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        private const string ReticleClassPrefix = "reticle--";
        private const float HintToastVisibleSeconds = 2.0f;
        private const float RosterRefreshIntervalSeconds = 0.2f;

        // GameObject creation is handled by UIRootBootstrap; this controller
        // just attaches to whatever UIDocument host it sits on.

        [SerializeField] private VisualTreeAsset hudDocumentOverride;

        [Header("Input")]
        [Tooltip("Toggles the top-left roster panel.")]
        [SerializeField] private KeyCode hideRosterKey = KeyCode.H;

        private const string RosterHiddenPrefKey = "office_flipout.ui.hud_roster_hidden";

        private UIDocument uiDocument;
        private VisualElement hudRoot;
        private VisualElement reticle;
        private VisualElement rageRoster;
        private VisualElement rosterBody;
        private VisualElement rosterPipStrip;
        private VisualElement rosterPeek;
        private Label rosterCount;
        private bool rosterHidden;
        private VisualElement hintToast;
        private Label hintLabel;
        private VisualElement winScreen;
        private Button winRestart;
        private Button winQuit;

        private ProgressTracker progressTracker;
        private readonly List<Rage_Meter> trackedMeters = new List<Rage_Meter>();
        private readonly Dictionary<string, RosterRow> rosterRows = new Dictionary<string, RosterRow>();
        private readonly List<string> rosterOrder = new List<string>();
        private readonly List<VisualElement> headerPips = new List<VisualElement>();
        // Per-row pulse expiry. When unscaledTime exceeds the stored time we
        // strip the pulse class so the USS transition can settle.
        private readonly Dictionary<string, float> rowPulseUntil = new Dictionary<string, float>();
        private float rosterTimer;
        private bool initialized;
        private bool winVisible;
        private float hintHideAt;
        private bool hintVisible;

        private const float RowPulseSeconds = 0.18f;
        private const float HotRatioThreshold = 0.66f;

        private struct RosterRow
        {
            public VisualElement root;
            public VisualElement thumb;
            public Label name;
            public VisualElement taskStrip;
            public VisualElement track;
            public VisualElement fill;
            public Label chip;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            HudSignals.ReticleStateChanged += HandleReticleStateChanged;
            HudSignals.HintRequested += HandleHintRequested;
            HudSignals.HintsEnabledChanged += HandleHintsEnabledChanged;
            GameRuntimeState.PauseChanged += HandlePauseChanged;
            GameRuntimeState.WinChanged += HandleWinChanged;
            TryInitialize();
        }

        private void OnDisable()
        {
            HudSignals.ReticleStateChanged -= HandleReticleStateChanged;
            HudSignals.HintRequested -= HandleHintRequested;
            HudSignals.HintsEnabledChanged -= HandleHintsEnabledChanged;
            GameRuntimeState.PauseChanged -= HandlePauseChanged;
            GameRuntimeState.WinChanged -= HandleWinChanged;
            DetachProgressTracker();
            DetachAllMeters();
        }

        private void TryInitialize()
        {
            if (initialized || uiDocument == null) return;

            if (hudDocumentOverride != null)
            {
                uiDocument.visualTreeAsset = hudDocumentOverride;
            }
            else if (uiDocument.visualTreeAsset == null)
            {
                VisualTreeAsset asset = Resources.Load<VisualTreeAsset>("UI/Hud/HUD");
                if (asset != null)
                {
                    uiDocument.visualTreeAsset = asset;
                }
            }

            if (uiDocument.rootVisualElement == null) return;

            hudRoot = uiDocument.rootVisualElement.Q<VisualElement>("HudRoot");
            if (hudRoot == null) return;

            reticle = hudRoot.Q<VisualElement>("Reticle");
            rageRoster = hudRoot.Q<VisualElement>("RageRoster");
            rosterBody = hudRoot.Q<VisualElement>("RosterBody");
            rosterPipStrip = hudRoot.Q<VisualElement>("RosterPips");
            rosterPeek = hudRoot.Q<VisualElement>("RosterPeek");
            rosterCount = hudRoot.Q<Label>("RosterCount");
            hintToast = hudRoot.Q<VisualElement>("HintToast");
            hintLabel = hudRoot.Q<Label>("HintLabel");
            winScreen = hudRoot.Q<VisualElement>("WinScreen");
            winRestart = hudRoot.Q<Button>("WinRestart");
            winQuit = hudRoot.Q<Button>("WinQuit");

            if (winRestart != null) winRestart.clicked += HandleRestartClicked;
            if (winQuit != null) winQuit.clicked += HandleQuitClicked;

            ApplyReticleState(HudSignals.CurrentReticleState);
            SetHintVisible(false, instant: true);
            SetWinVisible(GameRuntimeState.IsWin, instant: true);

            // Restore persisted "roster hidden" preference. Default = visible.
            rosterHidden = PlayerPrefs.GetInt(RosterHiddenPrefKey, 0) == 1;
            ApplyRosterHiddenState();

            BindProgressTracker();
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) { TryInitialize(); if (!initialized) return; }

            // Roster hide / show key. Suppressed while any blocking UI is up
            // so we don't fight the clipboard / menus / pause for the key.
            if (!GameRuntimeState.ShouldBlockGameplayInput
                && IsHideRosterPressedThisFrame())
            {
                ToggleRosterHidden();
            }

            // Hide the reticle whenever screen-space UI is on top of the world,
            // so the player isn't aiming through menus.
            ApplyReticleVisibility();

            // Roster refresh: cheap, polls ProgressTracker snapshots which it
            // owns - but we throttle to keep allocations minimal.
            rosterTimer += Time.unscaledDeltaTime;
            if (rosterTimer >= RosterRefreshIntervalSeconds)
            {
                rosterTimer = 0f;
                RefreshRoster();
            }

            // Hint auto-hide
            if (hintVisible && Time.unscaledTime >= hintHideAt)
            {
                SetHintVisible(false, instant: false);
            }

            ClearExpiredRowPulses();
        }

        private void ClearExpiredRowPulses()
        {
            if (rowPulseUntil.Count == 0) return;
            float now = Time.unscaledTime;
            // Iterate over a copy of keys so we can mutate while walking.
            List<string> expired = null;
            foreach (KeyValuePair<string, float> kv in rowPulseUntil)
            {
                if (now < kv.Value) continue;
                (expired ??= new List<string>()).Add(kv.Key);
            }
            if (expired == null) return;
            for (int i = 0; i < expired.Count; i++)
            {
                string key = expired[i];
                rowPulseUntil.Remove(key);
                if (rosterRows.TryGetValue(key, out RosterRow row) && row.root != null)
                {
                    row.root.RemoveFromClassList("roster-row--pulse");
                }
            }
        }

        private void HandleWinChanged(bool isWin)
        {
            if (!initialized) { TryInitialize(); if (!initialized) return; }
            SetWinVisible(isWin, instant: false);
        }

        // ----------------------------------------------------------------
        // Roster visibility (H key toggles the top-left panel)
        // ----------------------------------------------------------------

        private void ToggleRosterHidden()
        {
            rosterHidden = !rosterHidden;
            PlayerPrefs.SetInt(RosterHiddenPrefKey, rosterHidden ? 1 : 0);
            PlayerPrefs.Save();
            ApplyRosterHiddenState();

            // One-line confirmation toast so the action feels acknowledged.
            // Reuses the existing hint pipeline; the toast already obeys the
            // "hints enabled" pref and auto-hides.
            HudSignals.RequestHint(rosterHidden ? "Roster hidden  -  [H] to show"
                                                : "Roster shown");
        }

        private void ApplyRosterHiddenState()
        {
            if (rageRoster != null)
            {
                rageRoster.EnableInClassList("rage-roster--hidden", rosterHidden);
            }
            if (rosterPeek != null)
            {
                // Peek pill is the inverse of the panel: visible only when
                // the panel is hidden, so the H reminder is always on screen.
                rosterPeek.EnableInClassList("roster-peek--hidden", !rosterHidden);
            }
        }

        private bool IsHideRosterPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch (hideRosterKey)
            {
                case KeyCode.H:           return Keyboard.current.hKey.wasPressedThisFrame;
                case KeyCode.J:           return Keyboard.current.jKey.wasPressedThisFrame;
                case KeyCode.K:           return Keyboard.current.kKey.wasPressedThisFrame;
                case KeyCode.L:           return Keyboard.current.lKey.wasPressedThisFrame;
                case KeyCode.M:           return Keyboard.current.mKey.wasPressedThisFrame;
                case KeyCode.N:           return Keyboard.current.nKey.wasPressedThisFrame;
                case KeyCode.B:           return Keyboard.current.bKey.wasPressedThisFrame;
                case KeyCode.V:           return Keyboard.current.vKey.wasPressedThisFrame;
                case KeyCode.G:           return Keyboard.current.gKey.wasPressedThisFrame;
                case KeyCode.Y:           return Keyboard.current.yKey.wasPressedThisFrame;
                case KeyCode.U:           return Keyboard.current.uKey.wasPressedThisFrame;
                case KeyCode.I:           return Keyboard.current.iKey.wasPressedThisFrame;
                case KeyCode.O:           return Keyboard.current.oKey.wasPressedThisFrame;
                case KeyCode.P:           return Keyboard.current.pKey.wasPressedThisFrame;
                case KeyCode.BackQuote:   return Keyboard.current.backquoteKey.wasPressedThisFrame;
                case KeyCode.F1:          return Keyboard.current.f1Key.wasPressedThisFrame;
                case KeyCode.F2:          return Keyboard.current.f2Key.wasPressedThisFrame;
                case KeyCode.F3:          return Keyboard.current.f3Key.wasPressedThisFrame;
                case KeyCode.F4:          return Keyboard.current.f4Key.wasPressedThisFrame;
                default: return false;
            }
#else
            return false;
#endif
        }

        // ----------------------------------------------------------------
        // Reticle
        // ----------------------------------------------------------------

        private void HandleReticleStateChanged(ReticleState state) => ApplyReticleState(state);

        private void ApplyReticleState(ReticleState state)
        {
            if (reticle == null) return;
            reticle.RemoveFromClassList(ReticleClassPrefix + "idle");
            reticle.RemoveFromClassList(ReticleClassPrefix + "grab");
            reticle.RemoveFromClassList(ReticleClassPrefix + "interact");
            reticle.AddToClassList(ReticleClassPrefix + state.ToString().ToLowerInvariant());
        }

        private void ApplyReticleVisibility()
        {
            if (reticle == null) return;
            // Match the legacy IMGUI behavior: reticle is hidden any time the
            // player is reading screen-space UI (clipboard, main menu, win,
            // pause). GameRuntimeState aggregates all of those.
            bool shouldHide = GameRuntimeState.ShouldBlockGameplayInput;
            reticle.EnableInClassList("reticle--hidden", shouldHide);
        }

        // ----------------------------------------------------------------
        // Hint toast
        // ----------------------------------------------------------------

        private void HandleHintRequested(string message)
        {
            if (hintLabel == null || hintToast == null) return;
            hintLabel.text = message;
            hintHideAt = Time.unscaledTime + HintToastVisibleSeconds;
            SetHintVisible(true, instant: false);
        }

        private void HandleHintsEnabledChanged(bool enabled)
        {
            if (!enabled)
            {
                SetHintVisible(false, instant: true);
            }
        }

        private void SetHintVisible(bool visible, bool instant)
        {
            hintVisible = visible;
            if (hintToast == null) return;
            hintToast.EnableInClassList("hint-toast--hidden", !visible);
            if (instant)
            {
                // No additional behavior required - USS handles the transition.
            }
        }

        // ----------------------------------------------------------------
        // Win screen
        // ----------------------------------------------------------------

        private void HandlePauseChanged(bool _) { /* no-op for now; future overlays might react */ }

        private void SetWinVisible(bool visible, bool instant)
        {
            winVisible = visible;
            if (winScreen == null) return;
            winScreen.EnableInClassList("win-screen--hidden", !visible);
            winScreen.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        private void HandleRestartClicked()
        {
            GameRuntimeState.SetWin(false);
            ClipboardUIState.SetOpen(false);
            GameRuntimeState.ResetSession();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

        private void HandleQuitClicked()
        {
            GameRuntimeState.SetWin(false);
            GameRuntimeState.SetMainMenuOpen(true);
        }

        // ----------------------------------------------------------------
        // Roster (per-NPC rage dots)
        // ----------------------------------------------------------------

        private void BindProgressTracker()
        {
            if (progressTracker != null) return;
            progressTracker = ProgressTracker.Instance != null
                ? ProgressTracker.Instance
                : ProjectBootstrap.FindFirst<ProgressTracker>();
            if (progressTracker == null) return;

            progressTracker.ProgressChanged += HandleProgressChanged;
            AttachAllMeters();
            RefreshRoster();
        }

        private void DetachProgressTracker()
        {
            if (progressTracker == null) return;
            progressTracker.ProgressChanged -= HandleProgressChanged;
            progressTracker = null;
        }

        private void HandleProgressChanged()
        {
            if (!initialized) return;
            RefreshRoster();
        }

        private void AttachAllMeters()
        {
            DetachAllMeters();
            Rage_Meter[] meters = FindObjectsByType<Rage_Meter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < meters.Length; i++)
            {
                Rage_Meter meter = meters[i];
                if (meter == null) continue;
                trackedMeters.Add(meter);
                meter.SuccessfulSabotageSignal += HandleSabotageSignal;
                meter.FlippedOut += HandleNpcFlippedOut;
            }
        }

        private void DetachAllMeters()
        {
            for (int i = 0; i < trackedMeters.Count; i++)
            {
                Rage_Meter meter = trackedMeters[i];
                if (meter == null) continue;
                meter.SuccessfulSabotageSignal -= HandleSabotageSignal;
                meter.FlippedOut -= HandleNpcFlippedOut;
            }
            trackedMeters.Clear();
        }

        private void HandleSabotageSignal(Rage_Meter meter)
        {
            if (!initialized || meter == null) return;
            // Refresh first so the row exists / has new values, then pulse it.
            RefreshRoster();
            PulseRowForMeter(meter);
        }

        private void PulseRowForMeter(Rage_Meter meter)
        {
            string key = KeyForMeter(meter);
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!rosterRows.TryGetValue(key, out RosterRow row) || row.root == null) return;
            row.root.AddToClassList("roster-row--pulse");
            rowPulseUntil[key] = Time.unscaledTime + RowPulseSeconds;
        }

        private static string KeyForMeter(Rage_Meter meter)
        {
            if (meter == null) return string.Empty;
            return string.IsNullOrWhiteSpace(meter.NpcSignalId) ? meter.name : meter.NpcSignalId;
        }

        private void HandleNpcFlippedOut(Rage_Meter meter)
        {
            if (!initialized || meter == null) return;
            RefreshRoster();
            string npcName = ResolveNpcDisplayName(meter);
            HudSignals.RequestHint(string.IsNullOrWhiteSpace(npcName)
                ? "Coworker flipped out"
                : npcName + " flipped out");
        }

        private string ResolveNpcDisplayName(Rage_Meter meter)
        {
            if (meter == null) return string.Empty;
            string npcId = meter.NpcSignalId;
            if (progressTracker != null && !string.IsNullOrWhiteSpace(npcId))
            {
                IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps = progressTracker.GetSnapshots();
                for (int i = 0; i < snaps.Count; i++)
                {
                    ProgressTracker.EmployeeProgressSnapshot snap = snaps[i];
                    if (snap != null && snap.npcId == npcId && !string.IsNullOrWhiteSpace(snap.displayName))
                    {
                        return snap.displayName;
                    }
                }
            }

            return !string.IsNullOrWhiteSpace(meter.name) ? meter.name : npcId;
        }

        private void RefreshRoster()
        {
            if (!initialized || rageRoster == null) return;
            if (progressTracker == null)
            {
                BindProgressTracker();
                if (progressTracker == null) return;
            }

            IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps = progressTracker.GetSnapshots();

            // Track which ids are alive so we can prune deleted rows later.
            HashSet<string> alive = new HashSet<string>();

            VisualElement rowParent = rosterBody != null ? rosterBody : rageRoster;

            for (int i = 0; i < snaps.Count; i++)
            {
                ProgressTracker.EmployeeProgressSnapshot snap = snaps[i];
                if (snap == null) continue;
                string key = string.IsNullOrWhiteSpace(snap.npcId) ? snap.displayName : snap.npcId;
                if (string.IsNullOrWhiteSpace(key)) continue;
                alive.Add(key);

                if (!rosterRows.TryGetValue(key, out RosterRow row))
                {
                    row = CreateRosterRow();
                    rowParent.Add(row.root);
                    rosterRows[key] = row;
                    rosterOrder.Add(key);
                }

                BindRosterRow(row, snap);
            }

            // Prune rows whose npc id disappeared (rare, but safe).
            for (int i = rosterOrder.Count - 1; i >= 0; i--)
            {
                string key = rosterOrder[i];
                if (alive.Contains(key)) continue;
                if (rosterRows.TryGetValue(key, out RosterRow row))
                {
                    if (row.root != null && row.root.parent != null)
                    {
                        row.root.RemoveFromHierarchy();
                    }
                    rosterRows.Remove(key);
                    rowPulseUntil.Remove(key);
                }
                rosterOrder.RemoveAt(i);
            }

            RefreshHeader(snaps);
        }

        // ----------------------------------------------------------------
        // Header (overall flip-out summary)
        // ----------------------------------------------------------------

        /// <summary>
        /// True when the NPC is either already flipped or has hit the rage
        /// threshold (Rage_Meter only toggles IsFlippedOut partway through
        /// its multi-second cinematic, so threshold-hit is also "flipped"
        /// for HUD reporting purposes).
        /// </summary>
        private static bool IsEffectivelyFlipped(ProgressTracker.EmployeeProgressSnapshot snap)
        {
            if (snap == null) return false;
            if (snap.isFlippedOut) return true;
            return snap.requiredSignals > 0 && snap.currentRage >= snap.requiredSignals;
        }

        private void RefreshHeader(IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps)
        {
            if (rosterPipStrip == null && rosterCount == null) return;
            if (snaps == null) return;

            int total = snaps.Count;
            int flipped = 0;
            for (int i = 0; i < snaps.Count; i++)
            {
                if (IsEffectivelyFlipped(snaps[i])) flipped++;
            }

            if (rosterCount != null)
            {
                rosterCount.text = flipped + " / " + total;
            }

            if (rosterPipStrip == null) return;

            // Grow / shrink the pip strip to match the snapshot count.
            while (headerPips.Count < total)
            {
                VisualElement pip = new VisualElement();
                pip.AddToClassList("roster-pip");
                pip.pickingMode = PickingMode.Ignore;
                rosterPipStrip.Add(pip);
                headerPips.Add(pip);
            }
            while (headerPips.Count > total)
            {
                int last = headerPips.Count - 1;
                VisualElement pip = headerPips[last];
                if (pip != null && pip.parent != null) pip.RemoveFromHierarchy();
                headerPips.RemoveAt(last);
            }

            for (int i = 0; i < headerPips.Count; i++)
            {
                VisualElement pip = headerPips[i];
                ProgressTracker.EmployeeProgressSnapshot snap = snaps[i];
                pip.RemoveFromClassList("roster-pip--flipped");
                pip.RemoveFromClassList("roster-pip--locked");
                if (snap == null) continue;
                if (IsEffectivelyFlipped(snap))
                {
                    pip.AddToClassList("roster-pip--flipped");
                }
                else if (snap.npcId == NpcIds.Boss)
                {
                    // Boss stays locked-looking until everyone else flips.
                    bool everyoneElseFlipped =
                        progressTracker != null && progressTracker.AreAllCoworkersFlipped(snap.npcId);
                    if (!everyoneElseFlipped) pip.AddToClassList("roster-pip--locked");
                }
            }
        }

        // ----------------------------------------------------------------
        // Row construction / binding
        // ----------------------------------------------------------------

        private static RosterRow CreateRosterRow()
        {
            VisualElement root = new VisualElement();
            root.AddToClassList("roster-row");
            root.pickingMode = PickingMode.Ignore;

            VisualElement thumb = new VisualElement { name = "Thumb" };
            thumb.AddToClassList("roster-row-thumb");
            thumb.pickingMode = PickingMode.Ignore;
            root.Add(thumb);

            VisualElement body = new VisualElement { name = "Body" };
            body.AddToClassList("roster-row-body");
            body.pickingMode = PickingMode.Ignore;
            root.Add(body);

            VisualElement top = new VisualElement { name = "Top" };
            top.AddToClassList("roster-row-top");
            top.pickingMode = PickingMode.Ignore;
            body.Add(top);

            Label name = new Label { name = "Name" };
            name.AddToClassList("roster-row-name");
            top.Add(name);

            Label chip = new Label { name = "Chip" };
            chip.AddToClassList("roster-row-chip");
            top.Add(chip);

            VisualElement taskStrip = new VisualElement { name = "TaskStrip" };
            taskStrip.AddToClassList("roster-task-strip");
            taskStrip.pickingMode = PickingMode.Ignore;
            body.Add(taskStrip);

            VisualElement track = new VisualElement { name = "Track" };
            track.AddToClassList("bar-track");
            track.AddToClassList("bar-track--sm");
            track.AddToClassList("roster-bar-track");
            track.pickingMode = PickingMode.Ignore;
            body.Add(track);

            VisualElement fill = new VisualElement { name = "Fill" };
            fill.AddToClassList("bar-fill");
            fill.AddToClassList("bar-fill--rage");
            fill.AddToClassList("roster-bar-fill");
            fill.pickingMode = PickingMode.Ignore;
            track.Add(fill);

            return new RosterRow
            {
                root = root,
                thumb = thumb,
                name = name,
                taskStrip = taskStrip,
                track = track,
                fill = fill,
                chip = chip,
            };
        }

        private static void BindRosterRow(RosterRow row, ProgressTracker.EmployeeProgressSnapshot snap)
        {
            if (row.root == null || snap == null) return;
            row.name.text = string.IsNullOrWhiteSpace(snap.displayName) ? snap.npcId : snap.displayName;

            float ratio = snap.requiredSignals > 0
                ? Mathf.Clamp01((float)snap.currentRage / snap.requiredSignals)
                : 0f;
            bool isHot = ratio >= HotRatioThreshold;

            // Promote to "flipped" the moment the meter hits threshold. The
            // Rage_Meter only toggles isFlippedOut partway through its flip-out
            // cinematic, but ratio >= 1 means the flip is already inbound, so
            // the HUD should not sit on HOT for the ~3s cinematic window.
            ClipboardPresenter.NpcStatus status = ClipboardPresenter.DeriveStatus(snap, locked: false);
            if (IsEffectivelyFlipped(snap) && status != ClipboardPresenter.NpcStatus.Locked)
            {
                status = ClipboardPresenter.NpcStatus.FlippedOut;
            }

            BindThumbnail(row.thumb, snap, status, isHot);
            BindTaskPips(row.taskStrip, snap);
            BindBar(row.fill, ratio, status, isHot);
            BindChip(row.chip, status, isHot, ratio);

            // Sticky "flipped" row flash. Cleared automatically when status
            // returns to a non-flipped state (resets between sessions).
            row.root.EnableInClassList("roster-row--flipped", status == ClipboardPresenter.NpcStatus.FlippedOut);
        }

        private static void BindThumbnail(
            VisualElement thumb,
            ProgressTracker.EmployeeProgressSnapshot snap,
            ClipboardPresenter.NpcStatus status,
            bool isHot)
        {
            if (thumb == null) return;
            thumb.style.backgroundImage = snap.rageFaceSprite != null
                ? new StyleBackground(snap.rageFaceSprite)
                : new StyleBackground(StyleKeyword.None);

            thumb.RemoveFromClassList("roster-row-thumb--hot");
            thumb.RemoveFromClassList("roster-row-thumb--flipped");
            thumb.RemoveFromClassList("roster-row-thumb--locked");
            switch (status)
            {
                case ClipboardPresenter.NpcStatus.FlippedOut:
                    thumb.AddToClassList("roster-row-thumb--flipped");
                    break;
                case ClipboardPresenter.NpcStatus.Locked:
                    thumb.AddToClassList("roster-row-thumb--locked");
                    break;
                default:
                    if (isHot) thumb.AddToClassList("roster-row-thumb--hot");
                    break;
            }
        }

        private static void BindTaskPips(VisualElement strip, ProgressTracker.EmployeeProgressSnapshot snap)
        {
            if (strip == null) return;
            List<ClipboardPresenter.TaskRow> tasks = ClipboardPresenter.BuildTaskRows(snap);
            int desired = tasks != null ? tasks.Count : 0;

            // Reuse existing pip children, only create / destroy on size change.
            while (strip.childCount < desired)
            {
                VisualElement pip = new VisualElement();
                pip.AddToClassList("roster-task-pip");
                pip.pickingMode = PickingMode.Ignore;
                strip.Add(pip);
            }
            while (strip.childCount > desired)
            {
                strip.RemoveAt(strip.childCount - 1);
            }

            for (int i = 0; i < desired; i++)
            {
                VisualElement pip = strip[i];
                pip.EnableInClassList("roster-task-pip--done", tasks[i].Done);
            }
        }

        private static void BindBar(
            VisualElement fill,
            float ratio,
            ClipboardPresenter.NpcStatus status,
            bool isHot)
        {
            if (fill == null) return;
            fill.RemoveFromClassList("roster-bar-fill--hot");
            fill.RemoveFromClassList("roster-bar-fill--flipped");
            fill.RemoveFromClassList("roster-bar-fill--locked");

            if (status == ClipboardPresenter.NpcStatus.FlippedOut)
            {
                fill.AddToClassList("roster-bar-fill--flipped");
                fill.style.width = new StyleLength(new Length(100f, LengthUnit.Percent));
                return;
            }
            if (status == ClipboardPresenter.NpcStatus.Locked)
            {
                fill.AddToClassList("roster-bar-fill--locked");
            }
            else if (isHot)
            {
                fill.AddToClassList("roster-bar-fill--hot");
            }
            fill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
        }

        private static void BindChip(Label chip, ClipboardPresenter.NpcStatus status, bool isHot, float ratio)
        {
            if (chip == null) return;
            chip.RemoveFromClassList("roster-row-chip--calm");
            chip.RemoveFromClassList("roster-row-chip--agitated");
            chip.RemoveFromClassList("roster-row-chip--hot");
            chip.RemoveFromClassList("roster-row-chip--flipped");
            chip.RemoveFromClassList("roster-row-chip--locked");

            switch (status)
            {
                case ClipboardPresenter.NpcStatus.FlippedOut:
                    chip.text = "FLIPPED";
                    chip.AddToClassList("roster-row-chip--flipped");
                    break;
                case ClipboardPresenter.NpcStatus.Locked:
                    chip.text = "LOCKED";
                    chip.AddToClassList("roster-row-chip--locked");
                    break;
                case ClipboardPresenter.NpcStatus.Agitated:
                    if (isHot)
                    {
                        chip.text = "HOT";
                        chip.AddToClassList("roster-row-chip--hot");
                    }
                    else
                    {
                        chip.text = Mathf.RoundToInt(ratio * 100f) + "%";
                        chip.AddToClassList("roster-row-chip--agitated");
                    }
                    break;
                default:
                    chip.text = "CALM";
                    chip.AddToClassList("roster-row-chip--calm");
                    break;
            }
        }
    }
}
