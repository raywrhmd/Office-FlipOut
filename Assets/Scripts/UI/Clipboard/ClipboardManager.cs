using OfficeFlipOut.Data;
using OfficeFlipOut.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace OfficeFlipOut.UI
{
    [DefaultExecutionOrder(-250)]
    public class ClipboardManager : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Runtime")]
        [SerializeField] private bool startOpen;

        [Header("Data")]
        [SerializeField] private EmployeeProfileDatabase database;

        private bool initialized;
        private bool shellVisible;
        private bool dossierOpen;

        private CursorLockMode cachedCursorLockMode = CursorLockMode.Locked;
        private bool cachedCursorVisible;
        private bool cursorSnapshotCaptured;

        private GameObject shell;
        private GameObject directoryPanel;
        private GameObject dossierRoot;
        private GameObject progressPanel;
        private GameObject mapPanel;

        private struct StickyTabData
        {
            public Button button;
            public Image bg;
            public GameObject root;
            public Color32 baseColor;
            public float baseRotation;
        }

        private StickyTabData tabDirectory;
        private StickyTabData tabProgress;
        private StickyTabData tabMap;

        private Button footerPrevious;
        private Button footerNext;
        private Button footerClose;
        private Text pageCounterLabel;
        private Text footerPreviousLabel;
        private Text footerNextLabel;

        private EmployeeDirectoryView directoryView;
        private EmployeeDetailView detailView;
        private ProgressTracker progressTracker;
        private ClipboardAnimator animator;

        private void Awake()
        {
            ResolveReferences();
            TryInitialize();
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
            if (cursorSnapshotCaptured) RestoreCursorSnapshot();
        }

        private void Update()
        {
            if (!initialized) { TryInitialize(); if (!initialized) return; }
            if (animator != null && animator.IsAnimating) return;

            if (Input.GetKeyDown(toggleKey))
                ClipboardUIState.SetOpen(!ClipboardUIState.IsOpen);

            if (!ClipboardUIState.IsOpen) return;

            if (Input.GetKeyDown(closeKey)) { ClipboardUIState.SetOpen(false); return; }
            if (Input.GetKeyDown(KeyCode.Alpha1)) ClipboardUIState.SetTab(ClipboardTab.Directory);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) ClipboardUIState.SetTab(ClipboardTab.Progress);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) ClipboardUIState.SetTab(ClipboardTab.Map);
        }

        private void TryInitialize()
        {
            if (initialized) return;
            ResolveReferences();
            EnsureEventSystem();
            BuildUI();
            if (shell == null) return;
            WireEvents();
            ClipboardUIState.SetTab(ClipboardTab.Directory);
            ClipboardUIState.SetOpen(startOpen);
            initialized = true;
            ApplyState();
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
            }
        }

        // ---- Build ----

        private void BuildUI()
        {
            if (shell != null) return;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
            }
            CanvasScaler existingScaler = GetComponent<CanvasScaler>();
            CanvasScaler scaler = existingScaler != null ? existingScaler : gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.pixelPerfect = false;
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            shell = UIFactory.Rect("ClipboardShell", transform,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: Vector2.zero, offsetMax: Vector2.zero);

            Vector2 refRes = scaler.referenceResolution;
            int boardLeftPx = Mathf.RoundToInt(0.115f * refRes.x);
            int boardRightPx = Mathf.RoundToInt(0.885f * refRes.x);
            int boardBottomPx = Mathf.RoundToInt(0.055f * refRes.y);
            int boardTopPx = Mathf.RoundToInt(0.945f * refRes.y);
            Vector2 boardSizePx = new Vector2(boardRightPx - boardLeftPx, boardTopPx - boardBottomPx);

            float boardCenterDeltaX = (boardLeftPx + boardRightPx) * 0.5f - refRes.x * 0.5f;
            float boardCenterDeltaYFromBottom = (boardBottomPx + boardTopPx) * 0.5f - refRes.y * 0.5f;

            GameObject dimmerGo = UIFactory.Rect("Dimmer", shell.transform,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: Vector2.zero, offsetMax: Vector2.zero);
            dimmerGo.AddComponent<Image>().color = UIFactory.Dimmer;
            CanvasGroup dimmerCG = dimmerGo.AddComponent<CanvasGroup>();

            Image boardShadow = UIFactory.FilledImage("BoardShadow", shell.transform,
                UIFactory.Shadow,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f));
            RectTransform boardShadowRt = boardShadow.rectTransform;
            boardShadowRt.sizeDelta = boardSizePx;
            boardShadowRt.anchoredPosition = new Vector2(
                38f + boardCenterDeltaX,
                -10f + boardCenterDeltaYFromBottom);

            GameObject board = UIFactory.Rect("Board", shell.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f));
            RectTransform boardRt = board.GetComponent<RectTransform>();
            boardRt.sizeDelta = boardSizePx;
            boardRt.anchoredPosition = new Vector2(
                30f + boardCenterDeltaX,
                0f + boardCenterDeltaYFromBottom);

            board.AddComponent<Image>().color = UIFactory.BoardEdge;
            UIFactory.FilledImage("BoardFace", board.transform, UIFactory.BoardBrown,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(4, 4), offsetMax: new Vector2(-4, -4));

            CanvasGroup boardCG = board.AddComponent<CanvasGroup>();
            BuildClip(board.transform);

            GameObject paper = UIFactory.Rect("Paper", board.transform,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(16, 14), offsetMax: new Vector2(-16, -14));
            paper.AddComponent<Image>().color = UIFactory.PaperCream;

            UIFactory.RuledLines(paper.transform, 20);
            UIFactory.TapeStrip(paper.transform, new Vector2(0.06f, 0.95f), 65, 60, -35f);
            UIFactory.TapeStrip(paper.transform, new Vector2(0.94f, 0.05f), 60, 55, 30f);
            UIFactory.CoffeeRingStain(paper.transform, new Vector2(0.88f, 0.25f), 140f, 14f);
            UIFactory.PaperClip(board.transform, new Vector2(0.97f, 0.88f), -15f, 1.4f);

            // Position paper children with explicit anchors rather than a VLG.
            // VLG chains fail to propagate heights reliably through nested
            // anchor-stretched rects when the shell first becomes active.
            //
            // Layout constants (px from paper edge):
            //   pad=18 lr / 8 tb, gap=4
            //   top:    tabs(8..48) header(52..92) rule(96..97)
            //   bottom: footer(8..42) rule(46..47)
            //   content fills 101..paper-51
            const float pad = 18f;
            const float topPad = 8f;
            const float gap = 4f;
            float tabsTop = topPad;                         // 8
            float tabsBot = tabsTop + 40f;                  // 48
            float headerTop = tabsBot + gap;                // 52
            float headerBot = headerTop + 40f;              // 92
            float topRuleTop = headerBot + gap;             // 96
            float topRuleBot = topRuleTop + 1f;             // 97
            float contentTop = topRuleBot + gap;            // 101
            float footerH = 34f;
            float botPad = 8f;
            float footerBot = botPad;                       // 8
            float footerTop = footerBot + footerH;          // 42
            float botRuleBot = footerTop + gap;             // 46
            float botRuleTop = botRuleBot + 1f;             // 47
            float contentBot = botRuleTop + gap;            // 51

            BuildStickyTabs(paper.transform, pad, tabsTop, tabsBot);
            BuildHeader(paper.transform, pad, headerTop, headerBot);
            BuildRule(paper.transform, "TopRule", pad, topRuleTop);
            BuildContent(paper.transform, pad, contentTop, contentBot);
            BuildRule(paper.transform, "BottomRule", pad, botRuleBot);
            BuildFooter(paper.transform, pad, footerBot, footerTop);

            board.transform.Find("TopClip").SetAsLastSibling();

            animator = shell.AddComponent<ClipboardAnimator>();
            animator.Configure(boardRt, boardCG, dimmerCG);
            shell.SetActive(false);
        }

        private void BuildClip(Transform boardParent)
        {
            GameObject clip = UIFactory.Rect("TopClip", boardParent,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f));
            RectTransform clipRt = clip.GetComponent<RectTransform>();
            clipRt.pivot = new Vector2(0.5f, 0.5f);
            clipRt.localRotation = Quaternion.identity;
            clipRt.sizeDelta = new Vector2(108f, 160f);
            clipRt.anchoredPosition = new Vector2(0f, 58f);

            Image img = clip.AddComponent<Image>();
            Sprite sprite = UIFactory.ClipboardClipSprite;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color32(120, 125, 130, 255);
            }
            img.raycastTarget = false;
        }

        private void BuildStickyTabs(Transform paper, float pad, float top, float bot)
        {
            GameObject tabRow = PinTopStrip(paper, "StickyTabs", pad, top, bot);
            UIFactory.HorizontalGroup(tabRow, TextAnchor.LowerCenter, 6, expandHeight: true);

            tabDirectory = MakeTab("TabDirectory", tabRow.transform, "Directory", UIFactory.TabYellow, 0f);
            tabProgress = MakeTab("TabProgress", tabRow.transform, "Progress", UIFactory.TabGreen, 0f);
            tabMap = MakeTab("TabMap", tabRow.transform, "Map", UIFactory.TabBlue, 0f);
        }

        private StickyTabData MakeTab(string name, Transform parent, string label, Color32 color, float rot)
        {
            StickyTabData data;
            data.baseColor = color;
            data.baseRotation = rot;
            data.button = UIFactory.StickyTab(name, parent, label, color, rot,
                out data.bg, out data.root);
            return data;
        }

        private void BuildHeader(Transform paper, float pad, float top, float bot)
        {
            GameObject header = PinTopStrip(paper, "Header", pad, top, bot);
            UIFactory.VerticalGroup(header, TextAnchor.UpperCenter, 1, expandWidth: true, expandHeight: true);

            UIFactory.Label("Title", header.transform, "HR CLIPBOARD",
                22, FontStyle.Bold, UIFactory.TextDark, TextAnchor.MiddleCenter);
            UIFactory.Label("Subtitle", header.transform,
                "Employee Observation & Incident Log",
                11, FontStyle.Italic, UIFactory.TextHandwritten, TextAnchor.MiddleCenter);
        }

        private void BuildContent(Transform paper, float pad, float topInset, float botInset)
        {
            // Content area stretches between the rules — its height is fully
            // determined by anchor math (no VLG in the parent chain).
            GameObject content = UIFactory.Rect("Content", paper,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(pad, botInset),
                offsetMax: new Vector2(-pad, -topInset));

            // Each panel stretches to fill Content via anchors.
            // Only one is active at a time, so they overlay without conflict.
            directoryPanel = StretchFill("DirectoryPanel", content.transform);

            directoryView = directoryPanel.AddComponent<EmployeeDirectoryView>();
            directoryView.Configure(database, progressTracker);
            directoryView.OpenProfileRequested += HandleOpenProfile;
            directoryView.PageChanged += UpdatePageCounter;

            // Dossier overlay stretches to fill directoryPanel, excluded from its VLG
            dossierRoot = StretchFill("DossierRoot", directoryPanel.transform);
            dossierRoot.AddComponent<LayoutElement>().ignoreLayout = true;

            detailView = dossierRoot.AddComponent<EmployeeDetailView>();
            detailView.Configure(database, progressTracker);
            detailView.BackRequested += HandleBackToDirectory;
            dossierRoot.SetActive(false);

            progressPanel = StretchFill("ProgressPanel", content.transform);
            BuildProgressPanel(progressPanel.transform);

            mapPanel = StretchFill("MapPanel", content.transform);
            BuildMapPanel(mapPanel.transform);
        }

        private void BuildProgressPanel(Transform parent)
        {
            UIFactory.VerticalGroup(parent.gameObject, spacing: 8,
                padding: new RectOffset(16, 16, 12, 12));

            UIFactory.Label("Title", parent, "OPERATION PROGRESS",
                22, FontStyle.Bold, UIFactory.TextDark);
            UIFactory.DashedLine("Sep", parent);

            Text summary = UIFactory.Label("Summary", parent,
                "Open the Progress tab in UI Toolkit mode for full tracking.\nThis view shows basic status.",
                14, FontStyle.Italic, UIFactory.TextMedium);
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;

            UIFactory.PostItNote("Tip", parent,
                "TIP: The UI Toolkit version has\nfull task checklists and rage bars.", -2.2f);
        }

        private void BuildMapPanel(Transform parent)
        {
            UIFactory.VerticalGroup(parent.gameObject, spacing: 16,
                padding: new RectOffset(16, 16, 16, 16));
            UIFactory.Label("Title", parent, "OFFICE MAP", 22, FontStyle.Bold, UIFactory.TextDark);
            UIFactory.DashedLine("Sep", parent);
            Text msg = UIFactory.Label("Message", parent,
                "Map intel is being compiled.\nCheck back after recon is complete.",
                15, FontStyle.Italic, UIFactory.TextMedium);
            msg.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.PostItNote("Tip", parent,
                "NOTE: Use the Directory tab\nto check employee locations.", -2.2f);
        }

        private void BuildFooter(Transform paper, float pad, float bot, float top)
        {
            GameObject footer = PinBotStrip(paper, "Footer", pad, bot, top);
            UIFactory.HorizontalGroup(footer, spacing: 8, expandHeight: true);

            footerPrevious = UIFactory.Button("Previous", footer.transform, "< Prev Page",
                UIFactory.FooterBg, UIFactory.FooterText, 14);
            footerPrevious.GetComponent<LayoutElement>().flexibleWidth = 0.7f;
            footerPreviousLabel = footerPrevious.GetComponentInChildren<Text>();

            GameObject counterWrap = UIFactory.Rect("CounterWrap", footer.transform);
            LayoutElement cle = counterWrap.AddComponent<LayoutElement>();
            cle.flexibleWidth = 1.4f;
            cle.minHeight = 36;
            pageCounterLabel = UIFactory.PageCounter("PageCounter", counterWrap.transform);
            RectTransform pcRt = pageCounterLabel.GetComponent<RectTransform>();
            pcRt.anchorMin = Vector2.zero;
            pcRt.anchorMax = Vector2.one;
            pcRt.offsetMin = Vector2.zero;
            pcRt.offsetMax = Vector2.zero;

            footerClose = UIFactory.Button("Close", footer.transform, "Close [Tab]",
                UIFactory.FooterBg, UIFactory.FooterText, 14);
            footerClose.GetComponent<LayoutElement>().flexibleWidth = 0.9f;

            footerNext = UIFactory.Button("Next", footer.transform, "Next Page >",
                UIFactory.FooterBg, UIFactory.FooterText, 14);
            footerNext.GetComponent<LayoutElement>().flexibleWidth = 0.7f;
            footerNextLabel = footerNext.GetComponentInChildren<Text>();
        }

        // ---- Anchor helpers (no parent VLG needed) ----

        /// <summary>Top-anchored strip at a fixed pixel offset from the paper top.</summary>
        private static GameObject PinTopStrip(Transform paper, string name, float pad, float top, float bot)
        {
            return UIFactory.Rect(name, paper,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(pad, -(bot)),
                offsetMax: new Vector2(-pad, -(top)));
        }

        /// <summary>Bottom-anchored strip at a fixed pixel offset from the paper bottom.</summary>
        private static GameObject PinBotStrip(Transform paper, string name, float pad, float bot, float top)
        {
            return UIFactory.Rect(name, paper,
                anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
                offsetMin: new Vector2(pad, bot),
                offsetMax: new Vector2(-pad, top));
        }

        private static void BuildRule(Transform paper, string name, float pad, float top)
        {
            GameObject rule = UIFactory.Rect(name, paper,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(pad, -(top + 1f)),
                offsetMax: new Vector2(-pad, -(top)));
            rule.AddComponent<Image>().color = UIFactory.Separator;
        }

        private static GameObject StretchFill(string name, Transform parent)
        {
            return UIFactory.Rect(name, parent,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: Vector2.zero, offsetMax: Vector2.zero);
        }

        // ---- Wire ----

        private void WireEvents()
        {
            tabDirectory.button.onClick.AddListener(() => { CloseDossier(); ClipboardUIState.SetTab(ClipboardTab.Directory); });
            tabProgress.button.onClick.AddListener(() => { CloseDossier(); ClipboardUIState.SetTab(ClipboardTab.Progress); });
            tabMap.button.onClick.AddListener(() => { CloseDossier(); ClipboardUIState.SetTab(ClipboardTab.Map); });

            footerPrevious.onClick.AddListener(HandleFooterPrevious);
            footerNext.onClick.AddListener(HandleFooterNext);
            footerClose.onClick.AddListener(() => ClipboardUIState.SetOpen(false));
        }

        private void HandleOpenChanged(bool isOpen) => ApplyState();
        private void HandleTabChanged(ClipboardTab tab) => ApplyState();

        private void ApplyState()
        {
            if (!initialized) return;

            bool isOpen = ClipboardUIState.IsOpen;
            ClipboardTab tab = ClipboardUIState.ActiveTab;

            if (isOpen && !shellVisible)
            {
                shell.SetActive(true);
                shellVisible = true;
                if (animator != null) animator.PlayOpen();
            }
            else if (!isOpen && shellVisible)
            {
                shellVisible = false;
                if (animator != null)
                    animator.PlayClose(() => { if (shell != null) shell.SetActive(false); });
                else
                    shell.SetActive(false);
            }

            SetActive(directoryPanel, isOpen && tab == ClipboardTab.Directory);
            SetActive(progressPanel, isOpen && tab == ClipboardTab.Progress);
            SetActive(mapPanel, isOpen && tab == ClipboardTab.Map);

            UpdateStickyTabs(tab);
            UpdateFooter(tab);
            ApplyCursor(isOpen);

            if (isOpen)
            {
                if (tab == ClipboardTab.Directory)
                {
                    if (dossierOpen && detailView != null) detailView.Refresh();
                    else if (directoryView != null) directoryView.Refresh();
                }
            }
        }

        private void UpdateStickyTabs(ClipboardTab active)
        {
            SetStickyTabState(ref tabDirectory, active == ClipboardTab.Directory);
            SetStickyTabState(ref tabProgress, active == ClipboardTab.Progress);
            SetStickyTabState(ref tabMap, active == ClipboardTab.Map);
        }

        private void SetStickyTabState(ref StickyTabData tab, bool isActive)
        {
            if (tab.root == null) return;

            RectTransform rt = tab.root.GetComponent<RectTransform>();
            if (isActive)
            {
                rt.localRotation = Quaternion.Euler(0f, 0f, 0f);
                tab.bg.color = new Color32(
                    (byte)Mathf.Min(tab.baseColor.r + 15, 255),
                    (byte)Mathf.Min(tab.baseColor.g + 15, 255),
                    (byte)Mathf.Min(tab.baseColor.b + 15, 255), 255);
                tab.root.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
            }
            else
            {
                rt.localRotation = Quaternion.Euler(0f, 0f, tab.baseRotation);
                tab.bg.color = tab.baseColor;
                tab.root.transform.localScale = Vector3.one;
            }
        }

        // ---- Footer (P3) ----

        private void UpdateFooter(ClipboardTab tab)
        {
            if (tab == ClipboardTab.Directory)
            {
                footerPrevious.gameObject.SetActive(true);
                footerNext.gameObject.SetActive(true);

                if (dossierOpen)
                {
                    if (footerPreviousLabel != null) footerPreviousLabel.text = "< Prev File";
                    if (footerNextLabel != null) footerNextLabel.text = "Next File >";
                    int count = database != null ? database.Count : 0;
                    pageCounterLabel.text = count > 0
                        ? string.Format("file {0} of {1}", detailView.CurrentIndex + 1, count) : "";
                }
                else
                {
                    if (footerPreviousLabel != null) footerPreviousLabel.text = "< Prev Page";
                    if (footerNextLabel != null) footerNextLabel.text = "Next Page >";
                    UpdatePageCounter();
                }
            }
            else
            {
                footerPrevious.gameObject.SetActive(false);
                footerNext.gameObject.SetActive(false);
                pageCounterLabel.text = "";
            }
        }

        private void UpdatePageCounter()
        {
            if (pageCounterLabel == null || directoryView == null) return;
            int page = directoryView.CurrentPage;
            int total = directoryView.TotalPages;
            pageCounterLabel.text = total > 0
                ? string.Format("pg. {0} / {1}", page, total) : "";
        }

        // ---- Dossier drill-down (P2) ----

        private void OpenDossier(int profileIndex)
        {
            dossierOpen = true;
            if (detailView != null) detailView.ShowProfile(profileIndex);
            SetActive(dossierRoot, true);
            UpdateFooter(ClipboardUIState.ActiveTab);
        }

        private void CloseDossier()
        {
            if (!dossierOpen) return;
            dossierOpen = false;
            SetActive(dossierRoot, false);
            if (directoryView != null) directoryView.Refresh();
            UpdateFooter(ClipboardUIState.ActiveTab);
        }

        // ---- Navigation ----

        private void HandleOpenProfile(int profileIndex)
        {
            OpenDossier(profileIndex);
        }

        private void HandleBackToDirectory()
        {
            CloseDossier();
        }

        private void HandleFooterPrevious()
        {
            if (ClipboardUIState.ActiveTab != ClipboardTab.Directory) return;
            if (dossierOpen && detailView != null) detailView.PreviousProfile();
            else if (directoryView != null) directoryView.PreviousPage();
            UpdateFooter(ClipboardUIState.ActiveTab);
        }

        private void HandleFooterNext()
        {
            if (ClipboardUIState.ActiveTab != ClipboardTab.Directory) return;
            if (dossierOpen && detailView != null) detailView.NextProfile();
            else if (directoryView != null) directoryView.NextPage();
            UpdateFooter(ClipboardUIState.ActiveTab);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---- Cursor ----

        private void ApplyCursor(bool clipboardOpen)
        {
            if (clipboardOpen)
            {
                if (!cursorSnapshotCaptured)
                {
                    cachedCursorLockMode = Cursor.lockState;
                    cachedCursorVisible = Cursor.visible;
                    cursorSnapshotCaptured = true;
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            if (cursorSnapshotCaptured) RestoreCursorSnapshot();
        }

        private void RestoreCursorSnapshot()
        {
            Cursor.lockState = cachedCursorLockMode;
            Cursor.visible = cachedCursorVisible;
            cursorSnapshotCaptured = false;
        }

        // ---- Helpers ----

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private void EnsureEventSystem()
        {
            if (FindFirst<EventSystem>() != null) return;
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esGo.AddComponent<InputSystemUIInputModule>();
#else
            esGo.AddComponent<StandaloneInputModule>();
#endif
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
