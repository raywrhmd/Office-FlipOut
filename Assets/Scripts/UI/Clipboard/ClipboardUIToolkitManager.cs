using System.Collections.Generic;
using System.Text;
using OfficeFlipOut.Data;
using OfficeFlipOut.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace OfficeFlipOut.UI
{
    [DefaultExecutionOrder(-250)]
    [RequireComponent(typeof(UIDocument))]
    public class ClipboardUIToolkitManager : MonoBehaviour
    {
        private const string SandraNpcId = "npc_1";
        private const string BrutusNpcId = "npc_2";
        private const string TommyNpcId = "npc_3";
        private const int CardsPerPage = 3;
        private const int RuledLineCount = 20;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Runtime")]
        [SerializeField] private bool startOpen;

        [Header("Data")]
        [SerializeField] private EmployeeProfileDatabase database;

        [Header("Main Menu (Toolkit)")]
        [Tooltip("Blaine title art; assign Assets/Art/Sprites/Blaine/title.png")]
        [SerializeField] private Texture2D mainMenuTitleArt;
        [SerializeField] private string mainMenuSubtitleOverride;

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

        private VisualElement mainMenuShell;
        private VisualElement mainMenuBoard;
        private VisualElement mainMenuDimmer;
        private VisualElement mainMenuTitleImage;
        private Label mainMenuTitleFallback;
        private Label mainMenuSubtitleLabel;
        private Button mainMenuStart;
        private Button mainMenuQuit;
        private VisualElement clipboardHint;
        private bool mainMenuWired;

        // Tabs (3: Directory, Progress, Map)
        private VisualElement tabDirectoryRoot, tabProgressRoot, tabMapRoot;

        // Panels
        private VisualElement directoryPanel, progressPanel, mapPanel;

        // Directory: grid + dossier drill-down (P2)
        private VisualElement directoryGridView, dossierView;
        private bool dossierOpen;

        // Footer (P3)
        private Button footerBackDossier;
        private Button footerPrevious, footerNext, footerClose;
        private VisualElement footerCounterWrap;
        private Label pageCounterLabel;
        private VisualElement footerRow;

        // Quit popup (P4)
        private VisualElement quitPopup;

        // Directory state
        private int pageStart;

        // Detail state
        private int selectedProfileIndex;

        // Card refs (P1: added ragePips)
        private struct CardElements
        {
            public VisualElement root;
            public VisualElement colorStrip;
            public Label nameLabel;
            public Label difficultyLabel;
            public Label roleLabel;
            public VisualElement portrait;
            public VisualElement rageFace;
            public VisualElement statusBadge;
            public Label statusText;
            public Label dislikesLabel;
            public Label locationLabel;
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
        private Label detailLocation, detailScheduleHeader, detailSchedule;

        // Progress refs (P0)
        private Label progressOverallLabel;
        private VisualElement progressOverallFill;
        private ScrollView progressNpcList;
        private Label progressObjective, progressNextAction;
        private Label progressSecurityLabel;
        private VisualElement progressSecurityFill;

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

            if (GameRuntimeState.IsMainMenuOpen)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    GameRuntimeState.SetMainMenuOpen(false);
                    return;
                }
                if (Input.GetKeyDown(closeKey) || Input.GetKeyDown(KeyCode.Escape))
                {
                    QuitGame();
                    return;
                }
                return;
            }

            if (Input.GetKeyDown(toggleKey))
                ClipboardUIState.SetOpen(!ClipboardUIState.IsOpen);

            if (!ClipboardUIState.IsOpen) return;

            if (Input.GetKeyDown(closeKey)) { ClipboardUIState.SetOpen(false); return; }
            if (Input.GetKeyDown(KeyCode.Alpha1)) ClipboardUIState.SetTab(ClipboardTab.Directory);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) ClipboardUIState.SetTab(ClipboardTab.Progress);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) ClipboardUIState.SetTab(ClipboardTab.Map);
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
            GenerateMainMenuRuledLines();
            WireEvents();
            WireMainMenuIfNeeded();

            ClipboardUIState.SetTab(ClipboardTab.Directory);
            ClipboardUIState.SetOpen(startOpen);
            initialized = true;

            if (!startOpen)
            {
                shell.AddToClassList("hidden");
                board.AddToClassList("anim-closed");
                dimmer.AddToClassList("anim-hidden");
            }

            ApplyMainMenuTitleArt();
            if (!string.IsNullOrWhiteSpace(mainMenuSubtitleOverride) && mainMenuSubtitleLabel != null)
            {
                mainMenuSubtitleLabel.text = mainMenuSubtitleOverride;
            }

            if (mainMenuShell != null)
            {
                GameRuntimeState.MainMenuOpenChanged += HandleMainMenuOpenChanged;
            }

            ApplyState();
            if (mainMenuShell != null)
            {
                ApplyMainMenuVisibility();
            }
        }

        private void ResolveReferences()
        {
            if (progressTracker == null)
                progressTracker = FindFirst<ProgressTracker>();
            if (progressTracker == null)
                progressTracker = new GameObject("ProgressTracker").AddComponent<ProgressTracker>();

            if (database == null)
            {
                database = Resources.Load<EmployeeProfileDatabase>("EmployeeProfileDatabase");
                if (database == null)
                {
                    database = ScriptableObject.CreateInstance<EmployeeProfileDatabase>();
                    database.hideFlags = HideFlags.DontSave;
                }
#if UNITY_EDITOR
                if (database == null)
                {
                    Debug.LogWarning(
                        "[ClipboardUIToolkitManager] Assign EmployeeProfileDatabase in the inspector or add Assets/Resources/EmployeeProfileDatabase.asset (menu: Office Flip Out / Generate Default Employee Data).",
                        this);
                }
#endif
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

            footerRow = root.Q<VisualElement>("FooterRow");
            footerBackDossier = root.Q<Button>("FooterBackDossier");
            footerPrevious = root.Q<Button>("FooterPrevious");
            footerNext = root.Q<Button>("FooterNext");
            footerClose = root.Q<Button>("FooterClose");
            footerCounterWrap = root.Q<VisualElement>("FooterCounterWrap");
            pageCounterLabel = root.Q<Label>("PageCounter");

            quitPopup = root.Q<VisualElement>("QuitPopup");

            for (int i = 0; i < CardsPerPage; i++)
            {
                string p = "Card" + i;
                cards[i] = new CardElements
                {
                    root = root.Q<VisualElement>(p),
                    colorStrip = root.Q<VisualElement>(p + "ColorStrip"),
                    nameLabel = root.Q<Label>(p + "Name"),
                    difficultyLabel = root.Q<Label>(p + "Difficulty"),
                    roleLabel = root.Q<Label>(p + "Role"),
                    portrait = root.Q<VisualElement>(p + "Portrait"),
                    rageFace = root.Q<VisualElement>(p + "RageFace"),
                    statusBadge = root.Q<VisualElement>(p + "StatusBadge"),
                    statusText = root.Q<Label>(p + "StatusText"),
                    dislikesLabel = root.Q<Label>(p + "Dislikes"),
                    locationLabel = root.Q<Label>(p + "Location"),
                    lockBadge = root.Q<VisualElement>(p + "LockBadge"),
                    lockText = root.Q<Label>(p + "LockText"),
                    openButton = root.Q<Button>("OpenProfile" + i)
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
            detailLocation = root.Q<Label>("DetailLocation");
            detailScheduleHeader = root.Q<Label>("DetailScheduleHeader");
            detailSchedule = root.Q<Label>("DetailSchedule");

            progressOverallLabel = root.Q<Label>("ProgressOverallLabel");
            progressOverallFill = root.Q<VisualElement>("ProgressOverallFill");
            progressNpcList = root.Q<ScrollView>("ProgressNpcList");
            progressObjective = root.Q<Label>("ProgressObjective");
            progressNextAction = root.Q<Label>("ProgressNextAction");
            progressSecurityLabel = root.Q<Label>("ProgressSecurityLabel");
            progressSecurityFill = root.Q<VisualElement>("ProgressSecurityFill");

            mainMenuShell = root.Q<VisualElement>("MainMenuShell");
            mainMenuBoard = root.Q<VisualElement>("MainMenuBoard");
            mainMenuDimmer = root.Q<VisualElement>("MainMenuDimmer");
            mainMenuTitleImage = root.Q<VisualElement>("MainMenuTitleImage");
            mainMenuTitleFallback = root.Q<Label>("MainMenuTitleFallback");
            mainMenuSubtitleLabel = root.Q<Label>("MainMenuSubtitle");
            mainMenuStart = root.Q<Button>("MainMenuStart");
            mainMenuQuit = root.Q<Button>("MainMenuQuit");
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

        private void GenerateMainMenuRuledLines()
        {
            VisualElement container = root.Q<VisualElement>("MainMenuRuledLines");
            if (container == null)
            {
                return;
            }

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

        private void WireMainMenuIfNeeded()
        {
            if (mainMenuWired)
            {
                return;
            }
            if (mainMenuStart != null)
            {
                mainMenuStart.clicked += () => GameRuntimeState.SetMainMenuOpen(false);
            }
            if (mainMenuQuit != null)
            {
                mainMenuQuit.clicked += QuitGame;
            }
            mainMenuWired = true;
        }

        private void HandleMainMenuOpenChanged(bool _)
        {
            ApplyMainMenuVisibility();
        }

        private void ApplyMainMenuVisibility()
        {
            if (!initialized || mainMenuShell == null)
            {
                return;
            }

            bool open = GameRuntimeState.IsMainMenuOpen;
            if (open)
            {
                ClipboardUIState.SetOpen(false);
                mainMenuShell.RemoveFromClassList("hidden");
                if (mainMenuDimmer != null)
                {
                    mainMenuDimmer.RemoveFromClassList("anim-hidden");
                }
                if (mainMenuBoard != null)
                {
                    mainMenuBoard.RemoveFromClassList("anim-closed");
                }
                if (mainMenuStart != null)
                {
                    mainMenuStart.Focus();
                }
            }
            else
            {
                mainMenuShell.AddToClassList("hidden");
                if (mainMenuDimmer != null)
                {
                    mainMenuDimmer.AddToClassList("anim-hidden");
                }
                if (mainMenuBoard != null)
                {
                    mainMenuBoard.AddToClassList("anim-closed");
                }
            }

            ApplyCursor(ClipboardUIState.IsOpen || GameRuntimeState.IsMainMenuOpen);
            UpdateClipboardHintVisibility();
        }

        private void ApplyMainMenuTitleArt()
        {
            if (mainMenuTitleImage == null)
            {
                return;
            }

            if (mainMenuTitleArt != null)
            {
                mainMenuTitleImage.style.backgroundImage = new StyleBackground(mainMenuTitleArt);
                mainMenuTitleImage.RemoveFromClassList("hidden");
                if (mainMenuTitleFallback != null)
                {
                    mainMenuTitleFallback.AddToClassList("hidden-title-fallback");
                }
            }
            else
            {
                mainMenuTitleImage.style.backgroundImage = StyleKeyword.None;
                mainMenuTitleImage.AddToClassList("hidden");
                if (mainMenuTitleFallback != null)
                {
                    mainMenuTitleFallback.RemoveFromClassList("hidden-title-fallback");
                }
            }
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

            // Quit popup (P4)
            root.Q<Button>("QuitCornerBtn").clicked += () => SetVisible(quitPopup, true);
            root.Q<Button>("QuitPopupResume").clicked += () => SetVisible(quitPopup, false);
            root.Q<Button>("QuitPopupQuit").clicked += QuitGame;

            dimmer.RegisterCallback<ClickEvent>(_ => ClipboardUIState.SetOpen(false));
        }

        // ----------------------------------------------------------------
        // State Management
        // ----------------------------------------------------------------

        private void HandleOpenChanged(bool isOpen) => ApplyState();
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
                SetVisible(quitPopup, false);
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

            for (int i = 0; i < CardsPerPage; i++)
            {
                ref CardElements card = ref cards[i];
                if (card.root == null) continue;

                if (count == 0) { ShowCardFallback(ref card); continue; }

                int idx = pageStart + i;
                if (idx < 0 || idx >= count) { ShowCardFallback(ref card); continue; }
                EmployeeProfileData profile = database.GetProfileAt(idx);
                if (profile == null) { ShowCardFallback(ref card); continue; }
                if (IsLocked(profile)) { ShowCardLocked(ref card); continue; }
                ShowCardProfile(ref card, profile);
            }
            UpdateDirectoryPageCounter();
        }

        private void ShowCardProfile(ref CardElements card, EmployeeProfileData profile)
        {
            card.nameLabel.text = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? "Unknown" : profile.DisplayName;
            card.roleLabel.text = profile.Role;
            SetBackgroundColor(card.colorStrip, GetIdentityColor(profile.ColorIdentity));
            card.difficultyLabel.text = GetDifficultyLabel(profile.DifficultyTier);
            SetTextColor(card.difficultyLabel, GetDifficultyColor(profile.DifficultyTier));

            // P5: show up to 2 dislikes with count
            IReadOnlyList<string> dislikes = profile.Dislikes;
            card.dislikesLabel.text = FormatDislikeSummary(dislikes);

            card.locationLabel.text = "@ " + GetCurrentLocation(profile);
            UpdateCardStatusAndRageFace(ref card, profile.NpcId);
            SetVisible(card.lockBadge, false);
            SetPortraitSprite(card.portrait, ResolvePortrait(profile));
            card.openButton.SetEnabled(true);
        }

        private void ShowCardLocked(ref CardElements card)
        {
            card.nameLabel.text = "[ CLASSIFIED ]";
            card.roleLabel.text = "???";
            SetBackgroundColor(card.colorStrip, new Color32(42, 42, 48, 255));
            card.difficultyLabel.text = "BOSS";
            SetTextColor(card.difficultyLabel, GetDifficultyColor(EmployeeDifficultyTier.Final));
            card.dislikesLabel.text = "Hates: ???";
            card.locationLabel.text = "@ ???";
            SetVisible(card.lockBadge, true);
            card.lockText.text = "LOCKED";
            SetVisible(card.statusBadge, false);
            SetVisible(card.rageFace, false);
            SetPortraitSprite(card.portrait, null);
            card.portrait.style.backgroundColor = new StyleColor(new Color32(55, 50, 46, 255));
            card.openButton.SetEnabled(false);
        }

        private void ShowCardFallback(ref CardElements card)
        {
            card.nameLabel.text = "No Staff";
            card.roleLabel.text = "";
            SetBackgroundColor(card.colorStrip, new Color32(208, 202, 192, 255));
            card.difficultyLabel.text = "";
            card.dislikesLabel.text = "";
            card.locationLabel.text = "";
            SetVisible(card.lockBadge, false);
            SetVisible(card.statusBadge, false);
            SetVisible(card.rageFace, false);
            SetPortraitSprite(card.portrait, null);
            card.openButton.SetEnabled(false);
        }

        private void UpdateCardStatusAndRageFace(ref CardElements card, string npcId)
        {
            if (progressTracker == null)
            {
                SetVisible(card.statusBadge, false);
                SetVisible(card.rageFace, false);
                return;
            }

            ProgressTracker.EmployeeProgressSnapshot snap = FindSnapshot(npcId);
            bool flipped = snap != null && snap.isFlippedOut;
            int rage = snap != null ? snap.currentRage : 0;

            SetVisible(card.statusBadge, true);
            card.statusText.text = flipped ? "FLIPPED OUT" : (rage > 0 ? "AGITATED" : "ACTIVE");
            card.statusBadge.RemoveFromClassList("status-active");
            card.statusBadge.RemoveFromClassList("status-flipped");
            card.statusBadge.AddToClassList(flipped ? "status-flipped" : "status-active");

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
            if (IsLocked(profile)) { ShowDetailLocked(); return; }
            ShowDetailFull(profile);
        }

        private void ShowDetailFull(EmployeeProfileData profile)
        {
            Color32 idColor = GetIdentityColor(profile.ColorIdentity);
            SetBackgroundColor(detailColorBanner, idColor);
            SetBackgroundColor(detailFolderTab, idColor);
            SetVisible(detailFolderTab, true);

            detailName.text = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? "Unknown Employee" : profile.DisplayName;
            detailRole.text = profile.Role + "  /  " + profile.ColorIdentity;
            detailDifficulty.text = GetDifficultyLabel(profile.DifficultyTier);
            SetTextColor(detailDifficulty, GetDifficultyColor(profile.DifficultyTier));
            detailPersonality.text = FormatPersonalityQuote(profile.PersonalitySummary);
            SetVisible(detailLockBanner, false);

            UpdateDetailStatusBadge(profile.NpcId);
            SetPortraitSprite(detailPortrait, ResolvePortrait(profile));

            detailLikesHeader.text = "LIKES";
            detailLikes.text = BuildBulletList(profile.Likes);
            detailDislikesHeader.text = "DISLIKES";
            detailDislikes.text = BuildBulletList(profile.Dislikes);
            SetVisible(detailDislikesUnderline,
                profile.Dislikes != null && profile.Dislikes.Count > 0);

            detailHint.text = string.IsNullOrWhiteSpace(profile.SabotageHint)
                ? "Intel: watch their routine. trigger what they hate."
                : "Intel: " + profile.SabotageHint;

            detailLocation.text = "Currently @ " + GetCurrentLocation(profile);
            detailScheduleHeader.text = "DAILY ROUTINE";
            detailSchedule.text = BuildScheduleText(profile.ScheduleBlocks);
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

            detailName.text = "[ CLASSIFIED ]";
            detailRole.text = "Final Obstacle";
            detailDifficulty.text = "BOSS";
            SetTextColor(detailDifficulty, GetDifficultyColor(EmployeeDifficultyTier.Final));
            detailPersonality.text = "\u201COnly destabilizes after everyone else has flipped.\u201D";
            SetVisible(detailLockBanner, true);
            detailLockText.text = "DOSSIER LOCKED - Flip all coworkers to gain access";
            SetVisible(detailStatusRow, false);
            SetPortraitSprite(detailPortrait, null);

            detailLikesHeader.text = "LIKES";
            detailLikes.text = "  ???";
            detailDislikesHeader.text = "DISLIKES";
            detailDislikes.text = "  ???";
            SetVisible(detailDislikesUnderline, false);
            detailHint.text = "Intel: complete all coworker FLIP OUTs to unlock.";
            detailLocation.text = "Currently @ ???";
            detailScheduleHeader.text = "DAILY ROUTINE";
            detailSchedule.text = "  Schedule classified.";
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
            detailLocation.text = "";
            detailScheduleHeader.text = "";
            detailSchedule.text = "";
        }

        private void UpdateDetailStatusBadge(string npcId)
        {
            if (progressTracker == null) { SetVisible(detailStatusRow, false); return; }

            SetVisible(detailStatusRow, true);
            bool flipped = IsNpcFlippedOut(npcId);
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

            progressNpcList.Clear();

            for (int i = 0; i < snaps.Count; i++)
            {
                ProgressTracker.EmployeeProgressSnapshot snap = snaps[i];
                EmployeeProfileData profile = database != null
                    ? database.GetProfileByNpcId(snap.npcId)
                    : null;
                bool locked = profile != null && IsLocked(profile);

                VisualElement row = new VisualElement();
                row.AddToClassList("npc-progress-row");

                // Header: name + status badge
                VisualElement header = new VisualElement();
                header.AddToClassList("npc-progress-header");

                Label nameLabel = new Label(snap.displayName);
                nameLabel.AddToClassList("npc-progress-name");
                header.Add(nameLabel);

                Label statusLabel = new Label(
                    locked ? "LOCKED" :
                    snap.isFlippedOut ? "FLIPPED OUT" :
                    snap.currentRage > 0 ? "AGITATED" : "CALM");
                statusLabel.AddToClassList("npc-progress-status");
                if (snap.isFlippedOut) statusLabel.AddToClassList("npc-flipped");
                else if (locked) statusLabel.AddToClassList("npc-locked");

                if (!locked)
                {
                    VisualElement moodGroup = new VisualElement();
                    moodGroup.AddToClassList("npc-mood-group");

                    VisualElement barTrack = new VisualElement();
                    barTrack.AddToClassList("npc-rage-bar-track");
                    VisualElement barFill = new VisualElement();
                    barFill.AddToClassList("npc-rage-bar-fill");
                    int req = snap.requiredSignals > 0 ? snap.requiredSignals : 3;
                    float ragePct = Mathf.Clamp01((float)snap.currentRage / req) * 100f;
                    barFill.style.width = new StyleLength(new Length(ragePct, LengthUnit.Percent));
                    if (snap.currentRage >= req - 1) barFill.AddToClassList("rage-high");
                    barTrack.Add(barFill);
                    moodGroup.Add(barTrack);
                    moodGroup.Add(statusLabel);
                    header.Add(moodGroup);
                }
                else
                {
                    header.Add(statusLabel);
                }

                row.Add(header);

                // Task checkmarks (skip for locked NPCs)
                if (!locked)
                {
                    VisualElement taskRow = new VisualElement();
                    taskRow.AddToClassList("npc-task-row");

                    int taskCount = Mathf.Clamp(snap.requiredSignals, 1, snap.npcId == BrutusNpcId ? 2 : 3);
                    if (snap.npcId == SandraNpcId)
                    {
                        if (taskCount >= 1)
                        {
                            AddTaskItem(taskRow, "Spill drink near desk", snap.spilledDrink);
                        }

                        if (taskCount >= 2)
                        {
                            AddTaskItem(taskRow, "Microwave fish nearby", snap.microwavedFish);
                        }

                        if (taskCount >= 3)
                        {
                            AddTaskItem(taskRow, "Steal desk prop", snap.tookStapler);
                        }
                    }
                    else if (snap.npcId == BrutusNpcId)
                    {
                        if (taskCount >= 1)
                        {
                            AddTaskItem(taskRow, "Bring birthday cake near", snap.microwavedFish);
                        }

                        if (taskCount >= 2)
                        {
                            AddTaskItem(taskRow, "Knock over filing cabinet", snap.tookStapler);
                        }
                    }
                    else if (snap.npcId == TommyNpcId)
                    {
                        if (taskCount >= 1)
                        {
                            AddTaskItem(taskRow, "Hit with projectile", snap.spilledDrink);
                        }

                        if (taskCount >= 2)
                        {
                            AddTaskItem(taskRow, "Make loud noise nearby", snap.microwavedFish);
                        }

                        if (taskCount >= 3)
                        {
                            AddTaskItem(taskRow, "Unplug device", snap.tookStapler);
                        }
                    }
                    else
                    {
                        if (taskCount >= 1)
                        {
                            AddTaskItem(taskRow, "Spill drink", snap.spilledDrink);
                        }

                        if (taskCount >= 2)
                        {
                            AddTaskItem(taskRow, "Microwave fish", snap.microwavedFish);
                        }

                        if (taskCount >= 3)
                        {
                            AddTaskItem(taskRow, "Steal object", snap.tookStapler);
                        }
                    }
                    row.Add(taskRow);
                }

                progressNpcList.Add(row);
            }

            // Objective + next action
            string nextAction;
            string objectiveText = progressTracker.GetObjectiveText(null, out nextAction);
            progressObjective.text = objectiveText;
            progressNextAction.text = !string.IsNullOrWhiteSpace(nextAction)
                ? "Next: " + nextAction : "";

            // Job security
            float security = progressTracker.GetJobSecurity01();
            progressSecurityLabel.text = string.Format("Job Security: {0}%", Mathf.RoundToInt(security * 100f));
            progressSecurityFill.style.width = new StyleLength(new Length(security * 100f, LengthUnit.Percent));
        }

        private static void AddTaskItem(VisualElement parent, string taskName, bool done)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("task-item");

            Label check = new Label(done ? "\u2713" : "\u2717");
            check.AddToClassList("task-check");
            check.AddToClassList(done ? "task-done" : "task-pending");
            item.Add(check);

            Label label = new Label(taskName);
            label.AddToClassList("task-label");
            if (done) label.AddToClassList("task-label-done");
            item.Add(label);

            parent.Add(item);
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

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private ProgressTracker.EmployeeProgressSnapshot FindSnapshot(string npcId)
        {
            if (progressTracker == null) return null;
            IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps = progressTracker.GetSnapshots();
            for (int i = 0; i < snaps.Count; i++)
            {
                if (snaps[i].npcId == npcId)
                    return snaps[i];
            }
            return null;
        }

        private bool IsNpcFlippedOut(string npcId)
        {
            ProgressTracker.EmployeeProgressSnapshot snap = FindSnapshot(npcId);
            return snap != null && snap.isFlippedOut;
        }

        private bool IsLocked(EmployeeProfileData profile)
        {
            if (profile == null) return false;
            bool locked = profile.StartsLocked;
            if (locked && profile.RequiresAllCoworkersFlipped && progressTracker != null)
                locked = !progressTracker.AreAllCoworkersFlipped(profile.NpcId);
            return locked;
        }

        // P5: format dislikes summary for directory cards
        private static string FormatPersonalityQuote(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                return "\u201CNo intel on personality yet.\u201D";
            }

            return "\u201C" + summary + "\u201D";
        }

        private static string FormatDislikeSummary(IReadOnlyList<string> dislikes)
        {
            if (dislikes == null || dislikes.Count == 0)
                return "Hates: nothing obvious";
            if (dislikes.Count == 1)
                return "Hates: " + dislikes[0];
            if (dislikes.Count == 2)
                return "Hates: " + dislikes[0] + ", " + dislikes[1];
            return "Hates: " + dislikes[0] + ", " + dislikes[1] + " (+" + (dislikes.Count - 2) + " more)";
        }

        private static string GetCurrentLocation(EmployeeProfileData profile)
        {
            if (profile == null || profile.ScheduleBlocks == null || profile.ScheduleBlocks.Count == 0)
                return "Unknown";

            float nowHour = (Time.time / 60f) % 24f;
            for (int i = 0; i < profile.ScheduleBlocks.Count; i++)
            {
                EmployeeScheduleBlock b = profile.ScheduleBlocks[i];
                if (b != null && b.StartHour <= nowHour && nowHour < b.EndHour)
                    return string.IsNullOrWhiteSpace(b.Location) ? "Unknown" : b.Location;
            }
            EmployeeScheduleBlock fb = profile.ScheduleBlocks[0];
            return fb != null && !string.IsNullOrWhiteSpace(fb.Location)
                ? fb.Location : "Unknown";
        }

        private static string BuildBulletList(IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0)
                return "  None listed";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                sb.Append("  \u2022 ");
                sb.AppendLine(items[i]);
            }
            return sb.ToString();
        }

        private static string BuildScheduleText(IReadOnlyList<EmployeeScheduleBlock> blocks)
        {
            if (blocks == null || blocks.Count == 0)
                return "  No schedule data.";

            float nowHour = (Time.time / 60f) % 24f;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < blocks.Count; i++)
            {
                EmployeeScheduleBlock b = blocks[i];
                if (b == null) continue;
                bool isCurrent = b.StartHour <= nowHour && nowHour < b.EndHour;
                sb.Append(isCurrent ? " >> " : "    ");
                sb.AppendFormat("{0:00}:00-{1:00}:00", b.StartHour, b.EndHour);
                sb.Append("  ");
                sb.Append(b.Label);
                if (!string.IsNullOrWhiteSpace(b.Location))
                {
                    sb.Append(" @ ");
                    sb.Append(b.Location);
                }
                if (isCurrent) sb.Append("  [NOW]");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0) return 0;
            int wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static Color32 GetIdentityColor(string colorIdentity)
        {
            if (string.IsNullOrWhiteSpace(colorIdentity))
                return new Color32(140, 140, 140, 255);
            string key = colorIdentity.Trim().ToLowerInvariant();
            if (key.Contains("purple")) return new Color32(128, 70, 160, 255);
            if (key.Contains("red")) return new Color32(195, 60, 50, 255);
            if (key.Contains("green") || key.Contains("blue")) return new Color32(55, 140, 130, 255);
            if (key.Contains("black") || key.Contains("executive")) return new Color32(42, 42, 48, 255);
            if (key.Contains("orange")) return new Color32(210, 130, 50, 255);
            return new Color32(140, 140, 140, 255);
        }

        private static string GetDifficultyLabel(EmployeeDifficultyTier tier)
        {
            switch (tier)
            {
                case EmployeeDifficultyTier.Intro: return "EASY";
                case EmployeeDifficultyTier.Mid: return "MEDIUM";
                case EmployeeDifficultyTier.Advanced: return "HARD";
                case EmployeeDifficultyTier.Final: return "BOSS";
                default: return "";
            }
        }

        private static Color32 GetDifficultyColor(EmployeeDifficultyTier tier)
        {
            switch (tier)
            {
                case EmployeeDifficultyTier.Intro: return new Color32(60, 130, 65, 255);
                case EmployeeDifficultyTier.Mid: return new Color32(180, 155, 40, 255);
                case EmployeeDifficultyTier.Advanced: return new Color32(200, 100, 35, 255);
                case EmployeeDifficultyTier.Final: return new Color32(165, 35, 35, 255);
                default: return new Color32(148, 132, 112, 255);
            }
        }

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

        private static T FindFirst<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
#else
            return FindObjectOfType<T>();
#endif
        }
    }
}
