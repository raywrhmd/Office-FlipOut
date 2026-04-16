using System;
using System.Collections.Generic;
using OfficeFlipOut.Data;
using OfficeFlipOut.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace OfficeFlipOut.UI
{
    public class EmployeeDirectoryView : MonoBehaviour
    {
        private const int CardsPerPage = 3;

        private static readonly float[] CardRotations = { 0f, 0f, 0f };

        public event Action<int> OpenProfileRequested;
        public event Action PageChanged;

        private EmployeeProfileDatabase database;
        private ProgressTracker progressTracker;
        private int pageStart;
        private bool built;

        private readonly StaffCard[] cards = new StaffCard[CardsPerPage];

        public int CurrentPage
        {
            get
            {
                int count = database != null ? database.Count : 0;
                if (count <= 0) return 1;
                return (WrapIndex(pageStart, count) / CardsPerPage) + 1;
            }
        }

        public int TotalPages
        {
            get
            {
                int count = database != null ? database.Count : 0;
                if (count <= 0) return 1;
                return Mathf.CeilToInt((float)count / CardsPerPage);
            }
        }

        private sealed class StaffCard
        {
            public GameObject root;
            public Image cardBg;
            public Image colorStrip;
            public Text nameLabel;
            public Text roleLabel;
            public Image portrait;
            public Text dislikesLabel;
            public Text locationLabel;
            public Text difficultyLabel;
            public GameObject rageFaceRoot;
            public Image rageFaceImage;
            public GameObject statusBadge;
            public Text statusLabel;
            public GameObject lockRoot;
            public Text lockLabel;
            public Button openButton;
        }

        public void Configure(EmployeeProfileDatabase db, ProgressTracker tracker)
        {
            database = db;
            progressTracker = tracker;
            BuildIfNeeded();
        }

        public void Refresh()
        {
            BuildIfNeeded();
            int count = database != null ? database.Count : 0;

            for (int i = 0; i < CardsPerPage; i++)
            {
                StaffCard card = cards[i];
                if (card == null) continue;

                if (count == 0) { ShowFallback(card); continue; }

                int idx = WrapIndex(pageStart + i, count);
                EmployeeProfileData profile = database.GetProfileAt(idx);
                if (profile == null) { ShowFallback(card); continue; }
                if (IsLocked(profile)) { ShowLocked(card); continue; }

                ShowProfile(card, profile, idx);
            }
            PageChanged?.Invoke();
        }

        public void NextPage() { pageStart += CardsPerPage; Refresh(); }
        public void PreviousPage() { pageStart -= CardsPerPage; Refresh(); }

        // ---- Build ----

        private void BuildIfNeeded()
        {
            if (built) return;
            built = true;

            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);

            UIFactory.VerticalGroup(gameObject, TextAnchor.UpperLeft, 8,
                new RectOffset(0, 0, 0, 0), expandWidth: true);

            // Section header
            GameObject titleRow = UIFactory.Rect("TitleRow", transform);
            UIFactory.HorizontalGroup(titleRow, TextAnchor.MiddleLeft, 10, expandHeight: false);
            LayoutElement titleRowLE = titleRow.AddComponent<LayoutElement>();
            titleRowLE.preferredHeight = 24;

            UIFactory.Label("PanelTitle", titleRow.transform, "EMPLOYEE DIRECTORY",
                20, FontStyle.Bold, UIFactory.TextDark);

            Text note = UIFactory.Label("Note", titleRow.transform,
                "- observe before acting",
                13, FontStyle.Italic, UIFactory.TextHandwritten);
            note.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Card grid
            GameObject grid = UIFactory.Rect("StaffGrid", transform);
            UIFactory.HorizontalGroup(grid, TextAnchor.UpperCenter, 14, expandHeight: true);
            grid.AddComponent<LayoutElement>().flexibleHeight = 1;

            for (int i = 0; i < CardsPerPage; i++)
                cards[i] = BuildCard(grid.transform, i);
        }

        private StaffCard BuildCard(Transform gridParent, int slotIndex)
        {
            StaffCard card = new StaffCard();

            card.root = UIFactory.Rect("Card" + slotIndex, gridParent);
            RectTransform rootRt = card.root.GetComponent<RectTransform>();
            rootRt.localRotation = Quaternion.Euler(0f, 0f, CardRotations[slotIndex]);

            // Card shadow (excluded from layout)
            Image cardShadow = UIFactory.FilledImage("CardShadow", card.root.transform, UIFactory.CardShadow,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(3, -4), offsetMax: new Vector2(5, -1));
            cardShadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            card.cardBg = card.root.AddComponent<Image>();
            card.cardBg.color = UIFactory.CardWhite;

            VerticalLayoutGroup layout = UIFactory.VerticalGroup(card.root,
                TextAnchor.UpperLeft, 3, new RectOffset(0, 0, 0, 4));
            layout.childForceExpandHeight = false;
            card.root.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Color identity strip at top
            card.colorStrip = UIFactory.ColorStrip("ColorStrip", card.root.transform,
                UIFactory.PortraitPlaceholder, 6f);

            // Inner padding
            GameObject inner = UIFactory.Rect("Inner", card.root.transform);
            UIFactory.VerticalGroup(inner, TextAnchor.UpperLeft, 3,
                new RectOffset(10, 10, 4, 0));
            LayoutElement innerLE = inner.AddComponent<LayoutElement>();
            innerLE.flexibleWidth = 1;
            innerLE.flexibleHeight = 1;

            // Name row
            GameObject nameRow = UIFactory.Rect("NameRow", inner.transform);
            UIFactory.HorizontalGroup(nameRow, TextAnchor.MiddleLeft, 6, expandHeight: false);

            card.nameLabel = UIFactory.Label("Name", nameRow.transform, "",
                15, FontStyle.Bold, UIFactory.TextDark);
            card.nameLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            card.difficultyLabel = UIFactory.Label("Diff", nameRow.transform, "",
                12, FontStyle.Bold, UIFactory.TextMuted, TextAnchor.MiddleRight);
            card.difficultyLabel.gameObject.AddComponent<LayoutElement>().minWidth = 60;

            card.roleLabel = UIFactory.Label("Role", inner.transform, "",
                13, FontStyle.Italic, UIFactory.TextSubtle);

            // Portrait — capped flex so items below it (status, dislikes) get room
            GameObject portraitGo = UIFactory.Rect("Portrait", inner.transform);
            card.portrait = portraitGo.AddComponent<Image>();
            card.portrait.color = UIFactory.PortraitPlaceholder;
            card.portrait.preserveAspect = true;
            LayoutElement pLE = portraitGo.AddComponent<LayoutElement>();
            pLE.minHeight = 50;
            pLE.preferredHeight = 80;
            pLE.flexibleHeight = 0.5f;

            // Rage face badge — overlaid on the portrait's bottom-right corner
            GameObject rageFaceGo = UIFactory.Rect("RageFace", portraitGo.transform,
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 0f));
            RectTransform rfRt = rageFaceGo.GetComponent<RectTransform>();
            rfRt.pivot = new Vector2(1f, 0f);
            rfRt.sizeDelta = new Vector2(34, 34);
            rfRt.anchoredPosition = new Vector2(4f, -4f);
            rageFaceGo.AddComponent<LayoutElement>().ignoreLayout = true;

            Image rfBg = rageFaceGo.AddComponent<Image>();
            rfBg.color = new Color32(255, 255, 255, 220);
            rfBg.raycastTarget = false;

            GameObject rfIcon = UIFactory.Rect("RageFaceIcon", rageFaceGo.transform,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(2, 2), offsetMax: new Vector2(-2, -2));
            card.rageFaceImage = rfIcon.AddComponent<Image>();
            card.rageFaceImage.preserveAspect = true;
            card.rageFaceImage.color = Color.white;
            card.rageFaceImage.raycastTarget = false;
            card.rageFaceRoot = rageFaceGo;

            // Status badge
            card.statusBadge = UIFactory.Rect("StatusBadge", inner.transform);
            Image statusBg = card.statusBadge.AddComponent<Image>();
            statusBg.color = UIFactory.StatusActive;
            LayoutElement statusLE = card.statusBadge.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 20;
            statusLE.minHeight = 18;

            card.statusLabel = UIFactory.Label("StatusText", card.statusBadge.transform,
                "ACTIVE", 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform srt = card.statusLabel.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            UIFactory.DashedLine("CardSep", inner.transform);

            card.dislikesLabel = UIFactory.Label("Dislikes", inner.transform,
                "", 13, FontStyle.Normal, UIFactory.TextMedium);
            card.dislikesLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.dislikesLabel.gameObject.AddComponent<LayoutElement>().minHeight = 16;

            card.locationLabel = UIFactory.Label("Location", inner.transform,
                "", 12, FontStyle.Italic, UIFactory.TextHandwritten);
            card.locationLabel.gameObject.AddComponent<LayoutElement>().minHeight = 15;

            // Lock overlay
            GameObject lockBadge = UIFactory.Rect("LockBadge", inner.transform);
            lockBadge.AddComponent<Image>().color = UIFactory.LockBadge;
            lockBadge.AddComponent<LayoutElement>().minHeight = 24;
            card.lockRoot = lockBadge;
            lockBadge.SetActive(false);

            card.lockLabel = UIFactory.Label("LockLabel", lockBadge.transform, "",
                12, FontStyle.Bold, UIFactory.LockBadgeText, TextAnchor.MiddleCenter);
            RectTransform lrt = card.lockLabel.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6, 2);
            lrt.offsetMax = new Vector2(-6, -2);

            // Open Profile button
            GameObject btnWrap = UIFactory.Rect("BtnWrap", card.root.transform);
            UIFactory.VerticalGroup(btnWrap, padding: new RectOffset(8, 8, 0, 0));
            btnWrap.AddComponent<LayoutElement>().preferredHeight = 32;

            card.openButton = UIFactory.Button("OpenProfile", btnWrap.transform,
                "Open Dossier", UIFactory.ButtonBrown, UIFactory.ButtonBrownText, 14);

            int capturedSlot = slotIndex;
            card.openButton.onClick.AddListener(() =>
            {
                int count = database != null ? database.Count : 0;
                if (count <= 0) return;
                OpenProfileRequested?.Invoke(WrapIndex(pageStart + capturedSlot, count));
            });

            // Push pin at the card's top edge, on the color strip (no text there)
            float pinX = slotIndex == 1 ? 0.5f : (slotIndex == 0 ? 0.2f : 0.8f);
            UIFactory.PushPin(card.root.transform, new Vector2(pinX, 1.0f), 28f,
                slotIndex * 8f - 8f);

            return card;
        }

        // ---- Populate ----

        private void ShowProfile(StaffCard card, EmployeeProfileData profile, int dbIndex)
        {
            card.nameLabel.text = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? "Unknown" : profile.DisplayName;
            card.roleLabel.text = profile.Role;

            card.colorStrip.color = UIFactory.GetIdentityColor(profile.ColorIdentity);
            card.difficultyLabel.text = UIFactory.GetDifficultyLabel(profile.DifficultyTier);
            card.difficultyLabel.color = UIFactory.GetDifficultyColor(profile.DifficultyTier);

            card.dislikesLabel.text = FormatDislikeSummary(profile.Dislikes);

            card.locationLabel.text = "@ " + GetCurrentLocation(profile);
            UpdateStatusAndRageFace(card, profile.NpcId);
            card.lockRoot.SetActive(false);

            if (profile.Portrait != null)
            {
                card.portrait.sprite = profile.Portrait;
                card.portrait.color = Color.white;
            }
            else
            {
                card.portrait.sprite = null;
                card.portrait.color = UIFactory.PortraitPlaceholder;
            }
            card.openButton.interactable = true;
        }

        private void UpdateStatusAndRageFace(StaffCard card, string npcId)
        {
            if (progressTracker == null)
            {
                card.statusBadge.SetActive(false);
                card.rageFaceRoot.SetActive(false);
                return;
            }

            ProgressTracker.EmployeeProgressSnapshot snap = null;
            IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps = progressTracker.GetSnapshots();
            for (int i = 0; i < snaps.Count; i++)
            {
                if (snaps[i].npcId == npcId) { snap = snaps[i]; break; }
            }

            bool flipped = snap != null && snap.isFlippedOut;
            int rage = snap != null ? snap.currentRage : 0;

            card.statusBadge.SetActive(true);
            card.statusLabel.text = flipped ? "FLIPPED OUT" : (rage > 0 ? "AGITATED" : "ACTIVE");
            card.statusBadge.GetComponent<Image>().color =
                flipped ? UIFactory.StatusFlipped : UIFactory.StatusActive;

            Sprite face = snap != null ? snap.rageFaceSprite : null;
            card.rageFaceRoot.SetActive(face != null);
            card.rageFaceImage.sprite = face;
        }

        private void ShowLocked(StaffCard card)
        {
            card.nameLabel.text = "[ CLASSIFIED ]";
            card.roleLabel.text = "???";
            card.colorStrip.color = new Color32(42, 42, 48, 255);
            card.difficultyLabel.text = "BOSS";
            card.difficultyLabel.color = UIFactory.GetDifficultyColor(EmployeeDifficultyTier.Final);
            card.dislikesLabel.text = "Hates: ???";
            card.locationLabel.text = "@ ???";
            card.lockRoot.SetActive(true);
            card.lockLabel.text = "LOCKED";
            card.statusBadge.SetActive(false);
            card.rageFaceRoot.SetActive(false);
            card.portrait.sprite = null;
            card.portrait.color = new Color32(55, 50, 46, 255);
            card.openButton.interactable = false;
        }

        private void ShowFallback(StaffCard card)
        {
            card.nameLabel.text = "No Staff";
            card.roleLabel.text = "";
            card.colorStrip.color = UIFactory.PortraitPlaceholder;
            card.difficultyLabel.text = "";
            card.dislikesLabel.text = "";
            card.locationLabel.text = "";
            card.lockRoot.SetActive(false);
            card.statusBadge.SetActive(false);
            card.rageFaceRoot.SetActive(false);
            card.portrait.sprite = null;
            card.portrait.color = UIFactory.PortraitPlaceholder;
            card.openButton.interactable = false;
        }

        // ---- Helpers ----

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

        private bool IsLocked(EmployeeProfileData profile)
        {
            if (profile == null) return false;
            bool locked = profile.StartsLocked;
            if (locked && profile.RequiresAllCoworkersFlipped && progressTracker != null)
                locked = !progressTracker.AreAllCoworkersFlipped(profile.NpcId);
            return locked;
        }

        private static string GetCurrentLocation(EmployeeProfileData profile)
        {
            if (profile == null || profile.ScheduleBlocks == null || profile.ScheduleBlocks.Count == 0)
                return "Unknown";

            float nowHour = (Time.time / 60f) % 24f;
            for (int i = 0; i < profile.ScheduleBlocks.Count; i++)
            {
                EmployeeScheduleBlock block = profile.ScheduleBlocks[i];
                if (block != null && block.StartHour <= nowHour && nowHour < block.EndHour)
                    return string.IsNullOrWhiteSpace(block.Location) ? "Unknown" : block.Location;
            }

            EmployeeScheduleBlock fallback = profile.ScheduleBlocks[0];
            return fallback != null && !string.IsNullOrWhiteSpace(fallback.Location)
                ? fallback.Location : "Unknown";
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0) return 0;
            int wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }
    }
}
