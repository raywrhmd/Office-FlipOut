using System.Collections.Generic;
using OfficeFlipOut.Data;
using OfficeFlipOut.Systems;
using OfficeFlipOut.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OfficeFlipOut.UI
{
    [DefaultExecutionOrder(-250)]
    [RequireComponent(typeof(UIDocument))]
    public class ClipboardUIToolkitManager : MonoBehaviour
    {
        private const int CardsPerPage = 3;
        private const int RuledLineCount = 20;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Runtime")]
        [SerializeField] private bool startOpen;

        [Header("Data")]
        [SerializeField] private EmployeeProfileDatabase database;

        [Header("Dossier Trigger Icons (existing assets)")]
        [Tooltip("Icon used for fish/microwave triggers. Wire to Assets/Art/Sprites/Blaine/Fish_smell_icon.png. Leave null to render as label-only chip.")]
        [SerializeField] private Sprite fishSmellIcon;
        [Tooltip("Icon used for loud-noise triggers. Wire to Assets/Art/Sprites/Blaine/Annyance_talk_bubble.png. Leave null to render as label-only chip.")]
        [SerializeField] private Sprite loudNoiseIcon;


        private UIDocument uiDocument;
        private VisualElement root;
        private bool initialized;
        private bool shellVisible;

        private CursorLockMode cachedCursorLockMode = CursorLockMode.Locked;
        private bool cachedCursorVisible;
        private bool cursorSnapshotCaptured;

        private VisualElement shell;
        private VisualElement dimmer;
        private VisualElement board;
        private VisualElement clipboardPaper;

        private VisualElement clipboardHint;

        // Tabs (3: Directory, Progress, Map)
        private VisualElement tabDirectoryRoot, tabProgressRoot, tabMapRoot;

        // Panels
        private VisualElement directoryPanel, progressPanel, mapPanel;

        // Directory: grid + dossier drill-down (P2)
        private VisualElement directoryGridView, dossierView;
        private VisualElement staffGrid;
        private bool dossierOpen;

        // Footer (P3)
        private Button footerBackDossier;
        private Button footerPrevious, footerNext, footerClose;
        private VisualElement footerCounterWrap;
        private Label pageCounterLabel;
        private VisualElement footerRow;

        // (Quit popup retired in Phase 4b-2; superseded by PauseMenuController.)

        // Directory state
        private int pageStart;

        // Detail state
        private int selectedProfileIndex;

        // Card refs (fixed-slot template; Location removed - lives in dossier).
        private struct CardElements
        {
            public VisualElement root;
            public VisualElement colorStrip;
            public Label nameLabel;
            public Label difficultyLabel;
            public Label roleLabel;
            public VisualElement portrait;
            public VisualElement portraitImage;
            public VisualElement rageFace;
            public VisualElement statusBadge;
            public Label statusText;
            public Label dislikesLabel;
            public VisualElement lockBadge;
            public Label lockText;
            public Button openButton;
        }
        private readonly CardElements[] cards = new CardElements[CardsPerPage];

        // Detail refs
        private VisualElement detailColorBanner, detailFolderTab;
        private VisualElement detailPortrait;
        private Label detailName, detailRole, detailDifficulty, detailPersonality;
        private VisualElement detailStatusRow, detailStatusBadge;
        private Label detailStatusText;
        private VisualElement detailLockBanner;
        private Label detailLockText;
        private Label detailLikesHeader, detailLikes;
        private Label detailDislikesHeader, detailDislikes;
        private VisualElement detailDislikesUnderline;
        private Label detailHint;
        // Whereabouts (DetailLocation / DetailSchedule) was retired in the
        // dossier UX pass since NPCs don't move yet. Re-add the fields here
        // and the matching UXML block when the location/schedule feature ships.
        private VisualElement detailObjectiveBlock;
        private Label detailObjectiveHeader, detailObjective;
        private Label detailTriggersHeader;
        private VisualElement detailTriggersList;

        // Progress refs (P0)
        private Label progressOverallLabel;
        private VisualElement progressOverallFill;
        private ListView progressNpcList;
        private Label progressObjective, progressNextAction;
        private Label progressSecurityLabel;
        private VisualElement progressSecurityFill;
        private readonly List<ProgressTracker.EmployeeProgressSnapshot> progressItemsBuffer
            = new List<ProgressTracker.EmployeeProgressSnapshot>();
        private bool progressListWired;

        private ProgressTracker progressTracker;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ClipboardUIState.ClipboardOpenChanged += HandleOpenChanged;
            ClipboardUIState.ClipboardTabChanged += HandleTabChanged;
            TryInitialize();
        }

        private void OnDisable()
        {
            ClipboardUIState.ClipboardOpenChanged -= HandleOpenChanged;
            ClipboardUIState.ClipboardTabChanged -= HandleTabChanged;
            GameRuntimeState.MainMenuOpenChanged -= HandleMainMenuOpenChanged;
            if (progressTracker != null)
            {
                progressTracker.ProgressChanged -= HandleProgressTrackerChanged;
            }
            if (cursorSnapshotCaptured) RestoreCursorSnapshot();
            // Make sure we don't leave the world frozen behind us if the
            // controller goes away with the clipboard still considered open.
            GameRuntimeState.SetClipboardOpen(false);
        }

        private void HandleProgressTrackerChanged()
        {
            if (!initialized || !ClipboardUIState.IsOpen)
            {
                return;
            }

            ClipboardTab tab = ClipboardUIState.ActiveTab;
            if (tab == ClipboardTab.Directory)
            {
                if (dossierOpen) RefreshDetail();
                else RefreshDirectory();
            }
            else if (tab == ClipboardTab.Progress)
            {
                RefreshProgress();
            }
        }

        private void Update()
        {
            if (!initialized) { TryInitialize(); if (!initialized) return; }

            // Main menu input is owned by MainMenuController. The clipboard
            // simply refuses to open while the main menu is showing.
            if (GameRuntimeState.IsMainMenuOpen) return;

            if (IsKeyPressedThisFrame(toggleKey))
                ClipboardUIState.SetOpen(!ClipboardUIState.IsOpen);

            if (!ClipboardUIState.IsOpen) return;

            if (IsKeyPressedThisFrame(closeKey)) { ClipboardUIState.SetOpen(false); return; }
            if (IsDigitPressedThisFrame(1)) ClipboardUIState.SetTab(ClipboardTab.Directory);
            else if (IsDigitPressedThisFrame(2)) ClipboardUIState.SetTab(ClipboardTab.Progress);
            else if (IsDigitPressedThisFrame(3)) ClipboardUIState.SetTab(ClipboardTab.Map);
        }

        // ----------------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------------

        private void TryInitialize()
        {
            if (initialized) return;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            root = uiDocument.rootVisualElement;
            if (root == null || root.childCount == 0) return;

            ResolveReferences();
            QueryElements();
            if (shell == null) return;

            GenerateRuledLines();
            BindMapStickyNotes();
            WireEvents();

            ClipboardUIState.SetTab(ClipboardTab.Directory);
            ClipboardUIState.SetOpen(startOpen);
            initialized = true;

            if (!startOpen)
            {
                shell.AddToClassList("hidden");
                board.AddToClassList("anim-closed");
                dimmer.AddToClassList("anim-hidden");
            }

            // Main menu visibility flips clipboard cursor handling, so still
            // listen for it - the menu lives in its own UIDocument now.
            GameRuntimeState.MainMenuOpenChanged += HandleMainMenuOpenChanged;

            ApplyState();
        }

        private void ResolveReferences()
        {
            if (progressTracker == null)
                progressTracker = ProjectBootstrap.FindFirst<ProgressTracker>();
            if (progressTracker == null)
                progressTracker = new GameObject("ProgressTracker").AddComponent<ProgressTracker>();

            if (database == null)
            {
                database = ProjectBootstrap.LoadEmployeeProfileDatabase(true);
            }

            if (progressTracker != null)
            {
                progressTracker.ProgressChanged -= HandleProgressTrackerChanged;
                progressTracker.ProgressChanged += HandleProgressTrackerChanged;
            }
        }

        private void QueryElements()
        {
            shell = root.Q<VisualElement>("ClipboardShell");
            dimmer = root.Q<VisualElement>("Dimmer");
            board = root.Q<VisualElement>("Board");
            clipboardPaper = root.Q<VisualElement>("ClipboardPaper");

            tabDirectoryRoot = root.Q<VisualElement>("TabDirectoryRoot");
            tabProgressRoot = root.Q<VisualElement>("TabProgressRoot");
            tabMapRoot = root.Q<VisualElement>("TabMapRoot");

            directoryPanel = root.Q<VisualElement>("DirectoryPanel");
            progressPanel = root.Q<VisualElement>("ProgressPanel");
            mapPanel = root.Q<VisualElement>("MapPanel");

            directoryGridView = root.Q<VisualElement>("DirectoryGridView");
            dossierView = root.Q<VisualElement>("DossierView");
            staffGrid = root.Q<VisualElement>("StaffGrid");

            footerRow = root.Q<VisualElement>("FooterRow");
            footerBackDossier = root.Q<Button>("FooterBackDossier");
            footerPrevious = root.Q<Button>("FooterPrevious");
            footerNext = root.Q<Button>("FooterNext");
            footerClose = root.Q<Button>("FooterClose");
            footerCounterWrap = root.Q<VisualElement>("FooterCounterWrap");
            pageCounterLabel = root.Q<Label>("PageCounter");

            for (int i = 0; i < CardsPerPage; i++)
            {
                VisualElement cardRoot = root.Q<VisualElement>("Card" + i);
                cards[i] = new CardElements
                {
                    root = cardRoot,
                    colorStrip = cardRoot?.Q<VisualElement>("ColorStrip"),
                    nameLabel = cardRoot?.Q<Label>("Name"),
                    difficultyLabel = cardRoot?.Q<Label>("Difficulty"),
                    roleLabel = cardRoot?.Q<Label>("Role"),
                    portrait = cardRoot?.Q<VisualElement>("Portrait"),
                    portraitImage = cardRoot?.Q<VisualElement>("PortraitImage"),
                    rageFace = cardRoot?.Q<VisualElement>("RageFace"),
                    statusBadge = cardRoot?.Q<VisualElement>("StatusBadge"),
                    statusText = cardRoot?.Q<Label>("StatusText"),
                    dislikesLabel = cardRoot?.Q<Label>("Dislikes"),
                    lockBadge = cardRoot?.Q<VisualElement>("LockBadge"),
                    lockText = cardRoot?.Q<Label>("LockText"),
                    openButton = cardRoot?.Q<Button>("OpenProfile")
                };
            }

            detailColorBanner = root.Q<VisualElement>("DetailColorBanner");
            detailFolderTab = root.Q<VisualElement>("DetailFolderTab");
            detailPortrait = root.Q<VisualElement>("DetailPortrait");
            detailName = root.Q<Label>("DetailName");
            detailRole = root.Q<Label>("DetailRole");
            detailDifficulty = root.Q<Label>("DetailDifficulty");
            detailPersonality = root.Q<Label>("DetailPersonality");
            detailStatusRow = root.Q<VisualElement>("DetailStatusRow");
            detailStatusBadge = root.Q<VisualElement>("DetailStatusBadge");
            detailStatusText = root.Q<Label>("DetailStatusText");
            detailLockBanner = root.Q<VisualElement>("DetailLockBanner");
            detailLockText = root.Q<Label>("DetailLockText");
            detailLikesHeader = root.Q<Label>("DetailLikesHeader");
            detailLikes = root.Q<Label>("DetailLikes");
            detailDislikesHeader = root.Q<Label>("DetailDislikesHeader");
            detailDislikes = root.Q<Label>("DetailDislikes");
            detailDislikesUnderline = root.Q<VisualElement>("DetailDislikesUnderline");
            detailHint = root.Q<Label>("DetailHint");
            detailObjectiveBlock = root.Q<VisualElement>("DetailObjectiveBlock");
            detailObjectiveHeader = root.Q<Label>("DetailObjectiveHeader");
            detailObjective = root.Q<Label>("DetailObjective");
            detailTriggersHeader = root.Q<Label>("DetailTriggersHeader");
            detailTriggersList = root.Q<VisualElement>("DetailTriggersList");

            progressOverallLabel = root.Q<Label>("ProgressOverallLabel");
            progressOverallFill = root.Q<VisualElement>("ProgressOverallFill");
            progressNpcList = root.Q<ListView>("ProgressNpcList");
            progressObjective = root.Q<Label>("ProgressObjective");
            progressNextAction = root.Q<Label>("ProgressNextAction");
            progressSecurityLabel = root.Q<Label>("ProgressSecurityLabel");
            progressSecurityFill = root.Q<VisualElement>("ProgressSecurityFill");

            clipboardHint = root.Q<VisualElement>("ClipboardHint");
        }

        private void GenerateRuledLines()
        {
            VisualElement container = root.Q<VisualElement>("RuledLines");
            if (container == null) return;

            for (int i = 0; i < RuledLineCount; i++)
            {
                float yPercent = 100f * (1f - ((float)(i + 1) / (RuledLineCount + 1)));
                VisualElement line = new VisualElement();
                line.AddToClassList("ruled-line");
                line.style.top = new StyleLength(new Length(yPercent, LengthUnit.Percent));
                line.pickingMode = PickingMode.Ignore;
                container.Add(line);
            }
        }

        // Bind the two Map placeholder sticky notes (instances of the shared
        // StickyNote.uxml template). Walking child elements rather than Q-by-name
        // because every Instance contributes its own "StickyText" Label.
        private void BindMapStickyNotes()
        {
            BindStickyNoteText("MapStickyChaos", "Stress spreads. Plan your chaos before you cause it.");
            BindStickyNoteText("MapStickyTodo", "TODO: actual floor map. For now, the Directory tab is your map.");
        }

        private void BindStickyNoteText(string instanceName, string text)
        {
            VisualElement instance = root.Q<VisualElement>(instanceName);
            if (instance == null) return;
            Label label = instance.Q<Label>("StickyText");
            if (label != null) label.text = text;
        }

        private void HandleMainMenuOpenChanged(bool _)
        {
            // Main menu lives in its own UIDocument now; we just need to make
            // sure the clipboard closes and our cursor handling re-evaluates.
            if (GameRuntimeState.IsMainMenuOpen)
            {
                ClipboardUIState.SetOpen(false);
            }
            ApplyCursor(ClipboardUIState.IsOpen || GameRuntimeState.IsMainMenuOpen);
            UpdateClipboardHintVisibility();
        }

        // ----------------------------------------------------------------
        // Event Wiring
        // ----------------------------------------------------------------

        private void WireEvents()
        {
            root.Q<Button>("TabDirectory").clicked += () => { CloseDossier(); ClipboardUIState.SetTab(ClipboardTab.Directory); };
            root.Q<Button>("TabProgress").clicked += () => { CloseDossier(); ClipboardUIState.SetTab(ClipboardTab.Progress); };
            root.Q<Button>("TabMap").clicked += () => { CloseDossier(); ClipboardUIState.SetTab(ClipboardTab.Map); };

            if (footerBackDossier != null)
                footerBackDossier.clicked += CloseDossier;
            footerPrevious.clicked += HandleFooterPrevious;
            footerNext.clicked += HandleFooterNext;
            footerClose.clicked += () => ClipboardUIState.SetOpen(false);

            for (int i = 0; i < CardsPerPage; i++)
            {
                int slot = i;
                if (cards[i].openButton != null)
                    cards[i].openButton.clicked += () => HandleOpenProfile(slot);
            }

            // The old in-clipboard "Quit popup" / corner-X is gone. Pause and
            // quit are now handled by PauseMenuController via Esc.

            dimmer.RegisterCallback<ClickEvent>(_ => ClipboardUIState.SetOpen(false));
        }

        // ----------------------------------------------------------------
        // State Management
        // ----------------------------------------------------------------

        private void HandleOpenChanged(bool isOpen)
        {
            // Clipboard is a tactical pause: freeze the world while reading
            // dossiers / progress so NPCs don't drift mid-read.
            GameRuntimeState.SetClipboardOpen(isOpen);
            ApplyState();
        }

        private void HandleTabChanged(ClipboardTab tab) => ApplyState();

        private void ApplyState()
        {
            if (!initialized) return;

            bool isOpen = ClipboardUIState.IsOpen;
            ClipboardTab tab = ClipboardUIState.ActiveTab;

            if (isOpen && !shellVisible)
            {
                shell.RemoveFromClassList("hidden");
                board.RemoveFromClassList("anim-closed");
                dimmer.RemoveFromClassList("anim-hidden");
                shellVisible = true;
            }
            else if (!isOpen && shellVisible)
            {
                shellVisible = false;
                board.AddToClassList("anim-closed");
                dimmer.AddToClassList("anim-hidden");
                board.schedule.Execute(() =>
                {
                    if (!shellVisible && shell != null)
                        shell.AddToClassList("hidden");
                }).ExecuteLater(300);
            }

            SetPanelVisible(directoryPanel, isOpen && tab == ClipboardTab.Directory);
            SetPanelVisible(progressPanel, isOpen && tab == ClipboardTab.Progress);
            SetPanelVisible(mapPanel, isOpen && tab == ClipboardTab.Map);

            UpdateStickyTabs(tab);
            UpdateFooter(tab);
            ApplyCursor(ClipboardUIState.IsOpen || GameRuntimeState.IsMainMenuOpen);

            if (isOpen)
            {
                if (tab == ClipboardTab.Directory)
                {
                    if (dossierOpen) RefreshDetail();
                    else RefreshDirectory();
                }
                if (tab == ClipboardTab.Progress) RefreshProgress();
            }

            UpdateDossierPaperDecor(isOpen);
            UpdateClipboardHintVisibility();
        }

        private void UpdateClipboardHintVisibility()
        {
            if (clipboardHint == null)
            {
                return;
            }

            bool shouldShow = !GameRuntimeState.IsMainMenuOpen && !ClipboardUIState.IsOpen;
            clipboardHint.EnableInClassList("clipboard-hint--visible", shouldShow);
        }

        private void UpdateDossierPaperDecor(bool clipboardOpen)
        {
            if (clipboardPaper == null) return;
            bool hideBoardClip = clipboardOpen
                && ClipboardUIState.ActiveTab == ClipboardTab.Directory
                && dossierOpen;
            clipboardPaper.EnableInClassList("dossier-detail-open", hideBoardClip);
        }

        private void SetPanelVisible(VisualElement panel, bool visible)
        {
            if (panel == null) return;
            if (visible) panel.RemoveFromClassList("hidden");
            else panel.AddToClassList("hidden");
        }

        private void UpdateStickyTabs(ClipboardTab active)
        {
            SetTabActive(tabDirectoryRoot, active == ClipboardTab.Directory);
            SetTabActive(tabProgressRoot, active == ClipboardTab.Progress);
            SetTabActive(tabMapRoot, active == ClipboardTab.Map);
        }

        private static void SetTabActive(VisualElement tabRoot, bool isActive)
        {
            if (tabRoot == null) return;
            if (isActive) tabRoot.AddToClassList("tab-active");
            else tabRoot.RemoveFromClassList("tab-active");
        }

        // ----------------------------------------------------------------
        // Footer (P3: contextual labels)
        // ----------------------------------------------------------------

        private void UpdateFooter(ClipboardTab tab)
        {
            bool showNav;
            if (tab == ClipboardTab.Directory)
            {
                if (dossierOpen)
                {
                    int count = database != null ? database.Count : 0;
                    footerPrevious.text = "< Prev File";
                    footerNext.text = "Next File >";
                    pageCounterLabel.text = count > 0
                        ? string.Format("File {0} of {1}", selectedProfileIndex + 1, count) : "";
                    showNav = true;
                }
                else
                {
                    footerPrevious.text = "< Prev Page";
                    footerNext.text = "Next Page >";
                    UpdateDirectoryPageCounter();
                    showNav = true;
                }
            }
            else
            {
                footerPrevious.text = "";
                footerNext.text = "";
                pageCounterLabel.text = "";
                showNav = false;
            }
            SetVisible(footerBackDossier, tab == ClipboardTab.Directory && dossierOpen);
            SetVisible(footerPrevious, showNav);
            SetVisible(footerNext, showNav);
            SetVisible(footerCounterWrap, showNav);
        }

        private void UpdateDirectoryPageCounter()
        {
            int count = database != null ? database.Count : 0;
            if (count > 0)
            {
                pageStart = ClampPageStart(pageStart, count);
                int page = (pageStart / CardsPerPage) + 1;
                int total = GetDirectoryPageCount(count);
                pageCounterLabel.text = string.Format("Page {0} / {1}", page, total);
            }
            else
            {
                pageCounterLabel.text = "";
            }
        }

        // ----------------------------------------------------------------
        // Dossier Drill-Down (P2)
        // ----------------------------------------------------------------

        private void OpenDossier(int profileIndex)
        {
            selectedProfileIndex = profileIndex;
            dossierOpen = true;
            SetVisible(directoryGridView, false);
            SetVisible(dossierView, true);
            RefreshDetail();
            UpdateFooter(ClipboardUIState.ActiveTab);
            UpdateDossierPaperDecor(ClipboardUIState.IsOpen);
        }

        private void CloseDossier()
        {
            if (!dossierOpen) return;
            dossierOpen = false;
            SetVisible(dossierView, false);
            SetVisible(directoryGridView, true);
            RefreshDirectory();
            UpdateFooter(ClipboardUIState.ActiveTab);
            UpdateDossierPaperDecor(ClipboardUIState.IsOpen);
        }

        // ----------------------------------------------------------------
        // Directory View
        // ----------------------------------------------------------------

        private void RefreshDirectory()
        {
            int count = database != null ? database.Count : 0;
            pageStart = ClampPageStart(pageStart, count);
            int visibleCards = 0;

            for (int i = 0; i < CardsPerPage; i++)
            {
                ref CardElements card = ref cards[i];
                if (card.root == null) continue;

                if (count == 0)
                {
                    // Keep one slot as an explicit empty-state card, hide the rest.
                    if (i == 0)
                    {
                        ShowCardFallback(ref card);
                        visibleCards = 1;
                    }
                    else
                    {
                        HideCardSlot(ref card);
                    }
                    continue;
                }

                int idx = pageStart + i;
                if (idx < 0 || idx >= count)
                {
                    HideCardSlot(ref card);
                    continue;
                }
                EmployeeProfileData profile = database.GetProfileAt(idx);
                if (profile == null)
                {
                    HideCardSlot(ref card);
                    continue;
                }

                SetVisible(card.root, true);
                visibleCards++;
                if (ClipboardPresenter.IsLocked(profile, progressTracker))
                {
                    ShowCardLocked(ref card);
                    continue;
                }
                ShowCardProfile(ref card, profile);
            }
            UpdateStaffGridLayoutClass(visibleCards);
            UpdateDirectoryPageCounter();
        }

        private void ShowCardProfile(ref CardElements card, EmployeeProfileData profile)
        {
            card.nameLabel.text = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? "Unknown" : profile.DisplayName;
            card.roleLabel.text = profile.Role;
            SetBackgroundColor(card.colorStrip, ClipboardPresenter.GetIdentityColor(profile.ColorIdentity));
            card.difficultyLabel.text = ClipboardPresenter.GetDifficultyLabel(profile.DifficultyTier);
            SetTextColor(card.difficultyLabel, ClipboardPresenter.GetDifficultyColor(profile.DifficultyTier));

            card.dislikesLabel.text = ClipboardPresenter.FormatDislikeSummary(profile.Dislikes);

            UpdateCardStatusAndRageFace(ref card, profile.NpcId);
            SetVisible(card.lockBadge, false);
            SetPortraitSprite(card.portraitImage, ResolvePortrait(profile));
            ApplyCardPortraitFraming(card.portraitImage, profile.NpcId);
            card.openButton.SetEnabled(true);
        }

        private void ShowCardLocked(ref CardElements card)
        {
            card.nameLabel.text = ClipboardPresenter.LockedNameFallback;
            card.roleLabel.text = "???";
            SetBackgroundColor(card.colorStrip, new Color32(42, 42, 48, 255));
            card.difficultyLabel.text = ClipboardPresenter.LockedDifficultyFallback;
            SetTextColor(card.difficultyLabel, ClipboardPresenter.GetDifficultyColor(EmployeeDifficultyTier.Final));
            card.dislikesLabel.text = ClipboardPresenter.LockedDislikesFallback;
            SetVisible(card.lockBadge, true);
            card.lockText.text = "LOCKED";
            SetVisible(card.statusBadge, false);
            SetVisible(card.rageFace, false);
            SetPortraitSprite(card.portraitImage, null);
            ClearCardPortraitFraming(card.portraitImage);
            card.portrait.style.backgroundColor = new StyleColor(new Color32(55, 50, 46, 255));
            card.openButton.SetEnabled(false);
        }

        private void ShowCardFallback(ref CardElements card)
        {
            SetVisible(card.root, true);
            card.nameLabel.text = "No Staff";
            card.roleLabel.text = "";
            SetBackgroundColor(card.colorStrip, new Color32(208, 202, 192, 255));
            card.difficultyLabel.text = "";
            card.dislikesLabel.text = "";
            SetVisible(card.lockBadge, false);
            SetVisible(card.statusBadge, false);
            SetVisible(card.rageFace, false);
            SetPortraitSprite(card.portraitImage, null);
            ClearCardPortraitFraming(card.portraitImage);
            card.openButton.SetEnabled(false);
        }

        private void HideCardSlot(ref CardElements card)
        {
            SetVisible(card.root, false);
            card.openButton?.SetEnabled(false);
        }

        private void UpdateStaffGridLayoutClass(int visibleCards)
        {
            if (staffGrid == null)
            {
                return;
            }

            // Cards are now strictly 320x460, so no count-based width overrides
            // are needed. Just opt the grid into a centered layout when fewer
            // than 3 cards are visible so a lone boss card sits in the middle
            // instead of hugging the left edge.
            bool centered = visibleCards < 3;
            staffGrid.EnableInClassList("staff-grid--centered", centered);
        }

        private void UpdateCardStatusAndRageFace(ref CardElements card, string npcId)
        {
            if (progressTracker == null)
            {
                SetVisible(card.statusBadge, false);
                SetVisible(card.rageFace, false);
                return;
            }

            ProgressTracker.EmployeeProgressSnapshot snap = ClipboardPresenter.FindSnapshot(progressTracker, npcId);
            ClipboardPresenter.NpcStatus status = ClipboardPresenter.DeriveStatus(snap, false);

            SetVisible(card.statusBadge, true);
            card.statusText.text = ClipboardPresenter.StatusLabel(status);
            card.statusBadge.RemoveFromClassList("status-active");
            card.statusBadge.RemoveFromClassList("status-flipped");
            card.statusBadge.AddToClassList(status == ClipboardPresenter.NpcStatus.FlippedOut
                ? "status-flipped" : "status-active");

            Sprite face = snap != null ? snap.rageFaceSprite : null;
            // Keep rage badge visible for all active employees to surface current emotional status.
            SetVisible(card.rageFace, true);
            if (card.rageFace == null)
            {
                return;
            }

            if (face != null)
            {
                card.rageFace.style.backgroundImage = new StyleBackground(face);
            }
            else
            {
                // Avoid stale sprite when no rage face is currently provided.
                card.rageFace.style.backgroundImage = StyleKeyword.None;
            }
        }

        // ----------------------------------------------------------------
        // Detail View (Dossier)
        // ----------------------------------------------------------------

        private void RefreshDetail()
        {
            EmployeeProfileData profile = database != null
                ? database.GetProfileAt(selectedProfileIndex) : null;

            if (profile == null) { ShowDetailEmpty(); return; }
            if (ClipboardPresenter.IsLocked(profile, progressTracker)) { ShowDetailLocked(); return; }
            ShowDetailFull(profile);
        }

        private void ShowDetailFull(EmployeeProfileData profile)
        {
            Color32 idColor = ClipboardPresenter.GetIdentityColor(profile.ColorIdentity);
            SetBackgroundColor(detailColorBanner, idColor);
            SetBackgroundColor(detailFolderTab, idColor);
            SetVisible(detailFolderTab, true);

            detailName.text = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? "Unknown Employee" : profile.DisplayName;
            detailRole.text = profile.Role + "  /  " + profile.ColorIdentity;
            detailDifficulty.text = ClipboardPresenter.GetDifficultyLabel(profile.DifficultyTier);
            SetTextColor(detailDifficulty, ClipboardPresenter.GetDifficultyColor(profile.DifficultyTier));
            detailPersonality.text = ClipboardPresenter.FormatPersonalityQuote(profile.PersonalitySummary);
            SetVisible(detailLockBanner, false);

            UpdateDetailStatusBadge(profile.NpcId);
            SetPortraitSprite(detailPortrait, ResolvePortrait(profile));

            detailLikesHeader.text = "LIKES";
            detailLikes.text = ClipboardPresenter.BuildBulletList(profile.Likes);
            detailDislikesHeader.text = "DISLIKES";
            detailDislikes.text = ClipboardPresenter.BuildBulletList(profile.Dislikes);
            SetVisible(detailDislikesUnderline,
                profile.Dislikes != null && profile.Dislikes.Count > 0);

            detailHint.text = ClipboardPresenter.FormatSabotageHint(profile.SabotageHint);

            BuildTriggerChips(profile);

            string nextAction = string.Empty;
            string objective = progressTracker != null
                ? progressTracker.GetObjectiveText(profile.NpcId, out nextAction)
                : "No live objective feed.";
            SetVisible(detailObjectiveBlock, true);
            detailObjectiveHeader.text = "CASE NOTES";
            detailObjective.text = !string.IsNullOrWhiteSpace(nextAction)
                ? objective + "\nNext: " + nextAction
                : objective;
        }

        private Sprite ResolvePortrait(EmployeeProfileData profile)
        {
            if (profile == null)
            {
                return null;
            }

            if (profile.Portrait != null)
            {
                return profile.Portrait;
            }

            return progressTracker != null ? progressTracker.GetNpcPortraitSprite(profile.NpcId) : null;
        }

        private void ShowDetailLocked()
        {
            Color32 dark = new Color32(42, 42, 48, 255);
            SetBackgroundColor(detailColorBanner, dark);
            SetBackgroundColor(detailFolderTab, dark);
            SetVisible(detailFolderTab, true);

            detailName.text = ClipboardPresenter.LockedNameFallback;
            detailRole.text = ClipboardPresenter.LockedRoleFallback;
            detailDifficulty.text = ClipboardPresenter.LockedDifficultyFallback;
            SetTextColor(detailDifficulty, ClipboardPresenter.GetDifficultyColor(EmployeeDifficultyTier.Final));
            detailPersonality.text = ClipboardPresenter.LockedPersonalityQuote;
            SetVisible(detailLockBanner, true);
            detailLockText.text = ClipboardPresenter.LockedDossierBanner;
            SetVisible(detailStatusRow, false);
            SetPortraitSprite(detailPortrait, null);

            detailLikesHeader.text = "LIKES";
            detailLikes.text = "  ???";
            detailDislikesHeader.text = "DISLIKES";
            detailDislikes.text = "  ???";
            SetVisible(detailDislikesUnderline, false);
            detailHint.text = ClipboardPresenter.LockedHintFallback;

            ClearTriggerChips();
            if (detailTriggersHeader != null) detailTriggersHeader.text = "";

            SetVisible(detailObjectiveBlock, true);
            detailObjectiveHeader.text = "CASE NOTES";
            detailObjective.text = "Complete all coworker flip-outs to unlock full boss dossier.";
        }

        private void ShowDetailEmpty()
        {
            SetBackgroundColor(detailColorBanner, new Color32(208, 202, 192, 255));
            SetVisible(detailFolderTab, false);
            detailName.text = "No Profile";
            detailRole.text = "";
            detailDifficulty.text = "";
            detailPersonality.text = "";
            SetVisible(detailLockBanner, false);
            SetVisible(detailStatusRow, false);
            SetPortraitSprite(detailPortrait, null);
            detailLikesHeader.text = "";
            detailLikes.text = "";
            detailDislikesHeader.text = "";
            detailDislikes.text = "";
            SetVisible(detailDislikesUnderline, false);
            detailHint.text = "";

            ClearTriggerChips();
            if (detailTriggersHeader != null) detailTriggersHeader.text = "";

            SetVisible(detailObjectiveBlock, false);
            detailObjectiveHeader.text = "";
            detailObjective.text = "";
        }

        // ----------------------------------------------------------------
        // Dossier - Triggers chip strip
        // ----------------------------------------------------------------

        private void BuildTriggerChips(EmployeeProfileData profile)
        {
            if (detailTriggersList == null)
            {
                return;
            }

            ClearTriggerChips();

            if (detailTriggersHeader != null) detailTriggersHeader.text = "TRIGGERS";

            ProgressTracker.EmployeeProgressSnapshot snap = profile != null && progressTracker != null
                ? ClipboardPresenter.FindSnapshot(progressTracker, profile.NpcId)
                : null;
            if (snap == null)
            {
                AddTriggerChipPlaceholder("No live trigger data");
                return;
            }

            List<ClipboardPresenter.TaskRow> rows = ClipboardPresenter.BuildTaskRows(snap);
            if (rows == null || rows.Count == 0)
            {
                AddTriggerChipPlaceholder("No actionable triggers identified");
                return;
            }

            Color32 idColor = ClipboardPresenter.GetIdentityColor(profile.ColorIdentity);
            for (int i = 0; i < rows.Count; i++)
            {
                AddTriggerChipTo(detailTriggersList, rows[i], idColor, i == rows.Count - 1);
            }
        }

        private void ClearTriggerChips()
        {
            if (detailTriggersList == null) return;
            detailTriggersList.Clear();
        }

        private void AddTriggerChipTo(
            VisualElement parent,
            ClipboardPresenter.TaskRow row,
            Color32 identityColor,
            bool isLast = false)
        {
            if (parent == null) return;

            VisualElement chip = new VisualElement();
            chip.AddToClassList("trigger-chip");
            chip.AddToClassList(row.Done ? "trigger-chip--done" : "trigger-chip--pending");
            if (isLast) chip.AddToClassList("trigger-chip--last");
            chip.style.borderLeftColor = new StyleColor(identityColor);

            VisualElement icon = new VisualElement();
            icon.AddToClassList("trigger-chip-icon");
            Sprite iconSprite = ResolveTriggerIcon(row.IconKey);
            if (iconSprite != null)
            {
                icon.style.backgroundImage = new StyleBackground(iconSprite);
            }
            else
            {
                icon.AddToClassList("trigger-chip-icon--hidden");
            }
            chip.Add(icon);

            Label label = new Label(row.Label);
            label.AddToClassList("trigger-chip-label");
            chip.Add(label);

            Label status = new Label(row.Done ? "DONE" : "TODO");
            status.AddToClassList("trigger-chip-status");
            chip.Add(status);

            parent.Add(chip);
        }

        private void AddTriggerChipPlaceholder(string text)
        {
            VisualElement chip = new VisualElement();
            chip.AddToClassList("trigger-chip");

            Label label = new Label(text);
            label.AddToClassList("trigger-chip-label");
            chip.Add(label);

            detailTriggersList.Add(chip);
        }

        private Sprite ResolveTriggerIcon(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return null;
            switch (iconKey)
            {
                case ClipboardPresenter.TriggerIconKeys.MicrowaveFish:
                    return fishSmellIcon;
                case ClipboardPresenter.TriggerIconKeys.MakeLoudNoise:
                    return loudNoiseIcon;
                default:
                    return null;
            }
        }

        private void UpdateDetailStatusBadge(string npcId)
        {
            if (progressTracker == null) { SetVisible(detailStatusRow, false); return; }

            SetVisible(detailStatusRow, true);
            ProgressTracker.EmployeeProgressSnapshot snap = ClipboardPresenter.FindSnapshot(progressTracker, npcId);
            bool flipped = snap != null && snap.isFlippedOut;
            detailStatusText.text = flipped ? "FLIPPED OUT" : "ACTIVE";
            detailStatusBadge.RemoveFromClassList("status-active");
            detailStatusBadge.RemoveFromClassList("status-flipped");
            detailStatusBadge.AddToClassList(flipped ? "status-flipped" : "status-active");
        }

        // ----------------------------------------------------------------
        // Progress Panel (P0)
        // ----------------------------------------------------------------

        private void RefreshProgress()
        {
            if (progressTracker == null) return;

            IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps = progressTracker.GetSnapshots();
            int totalNpcs = snaps.Count;
            int flippedCount = progressTracker.GetFlipOutCount();

            progressOverallLabel.text = string.Format("Coworkers Flipped: {0} / {1}", flippedCount, totalNpcs);
            float overallPct = totalNpcs > 0 ? (float)flippedCount / totalNpcs * 100f : 0f;
            progressOverallFill.style.width = new StyleLength(new Length(overallPct, LengthUnit.Percent));

            UpdateProgressList(snaps);

            string nextAction;
            string objectiveText = progressTracker.GetObjectiveText(null, out nextAction);
            progressObjective.text = objectiveText;
            progressNextAction.text = !string.IsNullOrWhiteSpace(nextAction)
                ? "Next: " + nextAction : "";

            float security = progressTracker.GetJobSecurity01();
            progressSecurityLabel.text = string.Format("{0}%", Mathf.RoundToInt(security * 100f));
            progressSecurityFill.style.width = new StyleLength(new Length(security * 100f, LengthUnit.Percent));
        }

        private void UpdateProgressList(IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps)
        {
            if (progressNpcList == null) return;

            progressItemsBuffer.Clear();
            for (int i = 0; i < snaps.Count; i++)
            {
                progressItemsBuffer.Add(snaps[i]);
            }

            if (!progressListWired)
            {
                progressListWired = true;
                progressNpcList.itemsSource = progressItemsBuffer;
                progressNpcList.makeItem = MakeProgressRow;
                progressNpcList.bindItem = BindProgressRow;
                progressNpcList.unbindItem = UnbindProgressRow;
            }
            else
            {
                progressNpcList.itemsSource = progressItemsBuffer;
            }

            progressNpcList.RefreshItems();
        }

        private static VisualElement MakeProgressRow()
        {
            VisualElement row = new VisualElement { name = "ProgressRow" };
            row.AddToClassList("progress-row");

            VisualElement thumb = new VisualElement { name = "Thumb" };
            thumb.AddToClassList("progress-row-thumb");
            row.Add(thumb);

            VisualElement body = new VisualElement { name = "Body" };
            body.AddToClassList("progress-row-body");
            row.Add(body);

            VisualElement top = new VisualElement { name = "Top" };
            top.AddToClassList("progress-row-top");
            body.Add(top);

            Label nameLabel = new Label { name = "Name" };
            nameLabel.AddToClassList("progress-row-name");
            top.Add(nameLabel);

            Label statusChip = new Label { name = "Status" };
            statusChip.AddToClassList("progress-row-chip");
            top.Add(statusChip);

            VisualElement taskStrip = new VisualElement { name = "TaskStrip" };
            taskStrip.AddToClassList("progress-task-strip");
            body.Add(taskStrip);

            VisualElement barTrack = new VisualElement { name = "BarTrack" };
            barTrack.AddToClassList("bar-track");
            barTrack.AddToClassList("bar-track--sm");
            barTrack.AddToClassList("progress-row-bar-track");
            body.Add(barTrack);

            VisualElement barFill = new VisualElement { name = "BarFill" };
            barFill.AddToClassList("bar-fill");
            barFill.AddToClassList("bar-fill--rage");
            barFill.AddToClassList("progress-row-bar-fill");
            barTrack.Add(barFill);

            return row;
        }

        private void BindProgressRow(VisualElement row, int index)
        {
            if (index < 0 || index >= progressItemsBuffer.Count) return;
            ProgressTracker.EmployeeProgressSnapshot snap = progressItemsBuffer[index];
            if (snap == null) return;

            EmployeeProfileData profile = database != null
                ? database.GetProfileByNpcId(snap.npcId)
                : null;
            bool locked = profile != null && ClipboardPresenter.IsLocked(profile, progressTracker);
            Color32 identityColor = ClipboardPresenter.GetIdentityColor(
                profile != null ? profile.ColorIdentity : null);

            Label nameLabel = row.Q<Label>("Name");
            VisualElement thumb = row.Q<VisualElement>("Thumb");
            VisualElement taskStrip = row.Q<VisualElement>("TaskStrip");
            VisualElement barRow = row.Q<VisualElement>("BarTrack");
            VisualElement barFill = row.Q<VisualElement>("BarFill");
            Label statusChip = row.Q<Label>("Status");

            row.style.borderLeftColor = new StyleColor(identityColor);
            row.EnableInClassList("progress-row--flipped", !locked && snap.isFlippedOut);
            row.EnableInClassList("progress-row--locked", locked);

            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrWhiteSpace(snap.displayName)
                    ? snap.npcId : snap.displayName;
            }

            ClipboardPresenter.NpcStatus status = ClipboardPresenter.DeriveStatus(snap, locked);
            int req = snap.requiredSignals > 0 ? snap.requiredSignals : 3;
            float ratio = req > 0 ? Mathf.Clamp01((float)snap.currentRage / req) : 0f;
            bool isHot = !locked && snap.currentRage >= req - 1 && !snap.isFlippedOut;

            BindProgressThumbnail(thumb, snap, status, isHot);
            ApplyProgressChip(statusChip, status, isHot, ratio);

            if (locked)
            {
                SetVisible(taskStrip, false);
                SetVisible(barRow, false);
            }
            else
            {
                SetVisible(taskStrip, true);
                SetVisible(barRow, true);
                BindProgressTaskPips(taskStrip, snap);
                BindProgressBar(barFill, ratio, status, isHot);
            }
        }

        private static void BindProgressThumbnail(
            VisualElement thumb,
            ProgressTracker.EmployeeProgressSnapshot snap,
            ClipboardPresenter.NpcStatus status,
            bool isHot)
        {
            if (thumb == null) return;
            thumb.style.backgroundImage = snap.rageFaceSprite != null
                ? new StyleBackground(snap.rageFaceSprite)
                : new StyleBackground(StyleKeyword.None);

            thumb.RemoveFromClassList("progress-row-thumb--hot");
            thumb.RemoveFromClassList("progress-row-thumb--flipped");
            thumb.RemoveFromClassList("progress-row-thumb--locked");
            switch (status)
            {
                case ClipboardPresenter.NpcStatus.FlippedOut:
                    thumb.AddToClassList("progress-row-thumb--flipped");
                    break;
                case ClipboardPresenter.NpcStatus.Locked:
                    thumb.AddToClassList("progress-row-thumb--locked");
                    break;
                default:
                    if (isHot) thumb.AddToClassList("progress-row-thumb--hot");
                    break;
            }
        }

        private static void BindProgressTaskPips(
            VisualElement strip,
            ProgressTracker.EmployeeProgressSnapshot snap)
        {
            if (strip == null) return;
            List<ClipboardPresenter.TaskRow> tasks = ClipboardPresenter.BuildTaskRows(snap);
            int desired = tasks != null ? tasks.Count : 0;

            while (strip.childCount < desired)
            {
                VisualElement pip = new VisualElement();
                pip.AddToClassList("progress-task-pip");
                strip.Add(pip);
            }
            while (strip.childCount > desired)
            {
                strip.RemoveAt(strip.childCount - 1);
            }

            for (int i = 0; i < desired; i++)
            {
                strip[i].EnableInClassList("progress-task-pip--done", tasks[i].Done);
            }
        }

        private static void BindProgressBar(
            VisualElement fill,
            float ratio,
            ClipboardPresenter.NpcStatus status,
            bool isHot)
        {
            if (fill == null) return;
            fill.RemoveFromClassList("progress-row-bar-fill--hot");
            fill.RemoveFromClassList("progress-row-bar-fill--flipped");
            fill.RemoveFromClassList("progress-row-bar-fill--locked");

            if (status == ClipboardPresenter.NpcStatus.FlippedOut)
            {
                fill.AddToClassList("progress-row-bar-fill--flipped");
                fill.style.width = new StyleLength(new Length(100f, LengthUnit.Percent));
                return;
            }
            if (status == ClipboardPresenter.NpcStatus.Locked)
            {
                fill.AddToClassList("progress-row-bar-fill--locked");
            }
            else if (isHot)
            {
                fill.AddToClassList("progress-row-bar-fill--hot");
            }
            fill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
        }

        private static void ApplyProgressChip(
            Label chip,
            ClipboardPresenter.NpcStatus status,
            bool isHot,
            float ratio)
        {
            if (chip == null) return;

            chip.RemoveFromClassList("progress-row-chip--calm");
            chip.RemoveFromClassList("progress-row-chip--agitated");
            chip.RemoveFromClassList("progress-row-chip--hot");
            chip.RemoveFromClassList("progress-row-chip--flipped");
            chip.RemoveFromClassList("progress-row-chip--locked");

            switch (status)
            {
                case ClipboardPresenter.NpcStatus.FlippedOut:
                    chip.text = "FLIPPED";
                    chip.AddToClassList("progress-row-chip--flipped");
                    break;
                case ClipboardPresenter.NpcStatus.Locked:
                    chip.text = "LOCKED";
                    chip.AddToClassList("progress-row-chip--locked");
                    break;
                case ClipboardPresenter.NpcStatus.Agitated:
                    if (isHot)
                    {
                        chip.text = "HOT";
                        chip.AddToClassList("progress-row-chip--hot");
                    }
                    else
                    {
                        chip.text = Mathf.RoundToInt(ratio * 100f) + "%";
                        chip.AddToClassList("progress-row-chip--agitated");
                    }
                    break;
                default:
                    chip.text = "CALM";
                    chip.AddToClassList("progress-row-chip--calm");
                    break;
            }
        }

        private static void UnbindProgressRow(VisualElement row, int index)
        {
            VisualElement barFill = row.Q<VisualElement>("BarFill");
            if (barFill != null)
            {
                barFill.RemoveFromClassList("progress-row-bar-fill--hot");
                barFill.RemoveFromClassList("progress-row-bar-fill--flipped");
                barFill.RemoveFromClassList("progress-row-bar-fill--locked");
            }

            Label statusChip = row.Q<Label>("Status");
            if (statusChip != null)
            {
                statusChip.RemoveFromClassList("progress-row-chip--calm");
                statusChip.RemoveFromClassList("progress-row-chip--agitated");
                statusChip.RemoveFromClassList("progress-row-chip--hot");
                statusChip.RemoveFromClassList("progress-row-chip--flipped");
                statusChip.RemoveFromClassList("progress-row-chip--locked");
            }

            VisualElement thumb = row.Q<VisualElement>("Thumb");
            if (thumb != null)
            {
                thumb.RemoveFromClassList("progress-row-thumb--hot");
                thumb.RemoveFromClassList("progress-row-thumb--flipped");
                thumb.RemoveFromClassList("progress-row-thumb--locked");
            }

            row.RemoveFromClassList("progress-row--flipped");
            row.RemoveFromClassList("progress-row--locked");
        }

        // ----------------------------------------------------------------
        // Navigation
        // ----------------------------------------------------------------

        private void HandleOpenProfile(int slotIndex)
        {
            int count = database != null ? database.Count : 0;
            if (count <= 0) return;
            int idx = pageStart + slotIndex;
            if (idx < 0 || idx >= count) return;
            OpenDossier(idx);
        }

        private void HandleFooterPrevious()
        {
            if (ClipboardUIState.ActiveTab != ClipboardTab.Directory) return;
            if (dossierOpen) PreviousProfile();
            else
            {
                pageStart = Mathf.Max(0, pageStart - CardsPerPage);
                RefreshDirectory();
            }
            UpdateFooter(ClipboardUIState.ActiveTab);
        }

        private void HandleFooterNext()
        {
            if (ClipboardUIState.ActiveTab != ClipboardTab.Directory) return;
            if (dossierOpen) NextProfile();
            else
            {
                int count = database != null ? database.Count : 0;
                int maxPageStart = GetMaxPageStart(count);
                pageStart = Mathf.Min(maxPageStart, pageStart + CardsPerPage);
                RefreshDirectory();
            }
            UpdateFooter(ClipboardUIState.ActiveTab);
        }

        private static int GetDirectoryPageCount(int count)
        {
            if (count <= 0) return 1;
            return Mathf.CeilToInt((float)count / CardsPerPage);
        }

        private static int GetMaxPageStart(int count)
        {
            if (count <= 0) return 0;
            return (GetDirectoryPageCount(count) - 1) * CardsPerPage;
        }

        private static int ClampPageStart(int value, int count)
        {
            if (count <= 0) return 0;
            return Mathf.Clamp(value, 0, GetMaxPageStart(count));
        }

        private void NextProfile()
        {
            int count = database != null ? database.Count : 0;
            if (count > 0)
            {
                selectedProfileIndex = (selectedProfileIndex + 1) % count;
                RefreshDetail();
            }
        }

        private void PreviousProfile()
        {
            int count = database != null ? database.Count : 0;
            if (count > 0)
            {
                selectedProfileIndex = (selectedProfileIndex - 1 + count) % count;
                RefreshDetail();
            }
        }

        // ----------------------------------------------------------------
        // Cursor
        // ----------------------------------------------------------------

        private void ApplyCursor(bool clipboardOpen)
        {
            if (clipboardOpen)
            {
                if (!cursorSnapshotCaptured)
                {
                    cachedCursorLockMode = UnityEngine.Cursor.lockState;
                    cachedCursorVisible = UnityEngine.Cursor.visible;
                    cursorSnapshotCaptured = true;
                }
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                return;
            }
            if (cursorSnapshotCaptured) RestoreCursorSnapshot();
        }

        private void RestoreCursorSnapshot()
        {
            UnityEngine.Cursor.lockState = cachedCursorLockMode;
            UnityEngine.Cursor.visible = cachedCursorVisible;
            cursorSnapshotCaptured = false;
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private static void SetBackgroundColor(VisualElement el, Color color)
        {
            if (el != null) el.style.backgroundColor = new StyleColor(color);
        }

        private static void SetTextColor(Label label, Color color)
        {
            if (label != null) label.style.color = new StyleColor(color);
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (el == null) return;
            if (visible) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }

        private static void SetPortraitSprite(VisualElement el, Sprite sprite)
        {
            if (el == null) return;
            if (sprite != null)
            {
                el.style.backgroundImage = new StyleBackground(sprite);
                el.style.backgroundColor = StyleKeyword.None;
            }
            else
            {
                el.style.backgroundImage = StyleKeyword.None;
                el.style.backgroundColor = new StyleColor(new Color32(208, 202, 192, 255));
            }
        }

        private static void ApplyCardPortraitFraming(VisualElement portraitImage, string npcId)
        {
            if (portraitImage == null)
            {
                return;
            }

            ClearCardPortraitFraming(portraitImage);
            switch (npcId)
            {
                case NpcIds.Sandra:
                    portraitImage.AddToClassList("portrait-image--sandra");
                    break;
                case NpcIds.Brutus:
                    portraitImage.AddToClassList("portrait-image--brutus");
                    break;
                case NpcIds.Tommy:
                    portraitImage.AddToClassList("portrait-image--tommy");
                    break;
            }
        }

        private static void ClearCardPortraitFraming(VisualElement portraitImage)
        {
            if (portraitImage == null)
            {
                return;
            }

            portraitImage.RemoveFromClassList("portrait-image--sandra");
            portraitImage.RemoveFromClassList("portrait-image--brutus");
            portraitImage.RemoveFromClassList("portrait-image--tommy");
        }

        private static bool IsDigitPressedThisFrame(int digit)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            switch (digit)
            {
                case 1:
                    return Keyboard.current.digit1Key.wasPressedThisFrame;
                case 2:
                    return Keyboard.current.digit2Key.wasPressedThisFrame;
                case 3:
                    return Keyboard.current.digit3Key.wasPressedThisFrame;
                default:
                    return false;
            }
#else
            return false;
#endif
        }

        private static bool IsKeyPressedThisFrame(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.Tab:
                    return Keyboard.current.tabKey.wasPressedThisFrame;
                case KeyCode.Escape:
                    return Keyboard.current.escapeKey.wasPressedThisFrame;
                default:
                    return false;
            }
#else
            return false;
#endif
        }

    }
}
