using System;
using System.Collections.Generic;
using System.Text;
using OfficeFlipOut.Data;
using OfficeFlipOut.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace OfficeFlipOut.UI
{
    public class EmployeeDetailView : MonoBehaviour
    {
        public event Action BackRequested;

        private EmployeeProfileDatabase database;
        private ProgressTracker progressTracker;
        private int selectedIndex;
        private bool built;

        public int CurrentIndex => selectedIndex;

        // Header
        private Image colorBanner;
        private Image folderTab;
        private Text nameLabel;
        private Text roleLabel;
        private Text difficultyTag;
        private Image portrait;
        private Image portraitBorder;
        private Text personalityLabel;

        // Lock
        private GameObject lockBanner;
        private Text lockText;

        // Intel
        private Text likesHeader;
        private Text likesLabel;
        private Text dislikesHeader;
        private Text dislikesLabel;
        private Image dislikesUnderline;
        private Text hintLabel;

        // Schedule
        private Text locationLabel;
        private Text scheduleHeader;
        private Text scheduleLabel;

        // Status
        private GameObject statusRow;
        private Image statusBadgeBg;
        private Text statusBadgeText;

        private Button backButton;

        public void Configure(EmployeeProfileDatabase db, ProgressTracker tracker)
        {
            database = db;
            progressTracker = tracker;
            BuildIfNeeded();
        }

        public void ShowProfile(int profileIndex)
        {
            selectedIndex = profileIndex;
            Refresh();
        }

        public void NextProfile()
        {
            int count = database != null ? database.Count : 0;
            if (count > 0) { selectedIndex = (selectedIndex + 1) % count; Refresh(); }
        }

        public void PreviousProfile()
        {
            int count = database != null ? database.Count : 0;
            if (count > 0) { selectedIndex = (selectedIndex - 1 + count) % count; Refresh(); }
        }

        public void Refresh()
        {
            BuildIfNeeded();
            EmployeeProfileData profile = database != null ? database.GetProfileAt(selectedIndex) : null;
            if (profile == null) { ShowEmpty(); return; }
            if (IsLocked(profile)) { ShowLocked(); return; }
            ShowFull(profile);
        }

        // ---- Build ----

        private void BuildIfNeeded()
        {
            if (built) return;
            built = true;

            UIFactory.VerticalGroup(gameObject, TextAnchor.UpperLeft, 6,
                new RectOffset(0, 0, 0, 8));

            // Color identity banner
            colorBanner = UIFactory.ColorStrip("ColorBanner", transform,
                UIFactory.PortraitPlaceholder, 6f);

            // Folder tab sticking out on the right
            folderTab = UIFactory.FilledImage("FolderTab", transform,
                UIFactory.PortraitPlaceholder,
                anchorMin: new Vector2(0.82f, 1f),
                anchorMax: new Vector2(0.98f, 1f));
            folderTab.rectTransform.sizeDelta = new Vector2(0, 20);
            folderTab.rectTransform.anchoredPosition = new Vector2(0, 10);
            folderTab.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            // Paper clip holding the dossier together
            UIFactory.PaperClip(transform, new Vector2(0.92f, 0.98f), -12f, 1.3f);

            // Main body
            GameObject body = UIFactory.Rect("Body", transform);
            UIFactory.VerticalGroup(body, TextAnchor.UpperLeft, 8,
                new RectOffset(14, 14, 8, 0));
            body.AddComponent<LayoutElement>().flexibleHeight = 1;

            // Header row: portrait + identity
            GameObject headerRow = UIFactory.Rect("HeaderRow", body.transform);
            UIFactory.HorizontalGroup(headerRow, TextAnchor.UpperLeft, 16, expandHeight: false);

            // Portrait with polaroid border
            GameObject portraitWrap = UIFactory.Rect("PortraitWrap", headerRow.transform);
            portraitBorder = portraitWrap.AddComponent<Image>();
            portraitBorder.color = UIFactory.CardWhite;
            UIFactory.VerticalGroup(portraitWrap, padding: new RectOffset(5, 5, 5, 16));
            LayoutElement pwLE = portraitWrap.AddComponent<LayoutElement>();
            pwLE.preferredWidth = 120;
            pwLE.preferredHeight = 145;
            pwLE.flexibleWidth = 0;

            Image pShadow = UIFactory.FilledImage("PortraitShadow", portraitWrap.transform,
                new Color(0, 0, 0, 0.08f),
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(2, -3), offsetMax: new Vector2(4, -1));
            pShadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            GameObject portraitGo = UIFactory.Rect("Portrait", portraitWrap.transform);
            portrait = portraitGo.AddComponent<Image>();
            portrait.color = UIFactory.PortraitPlaceholder;
            portrait.preserveAspect = true;

            // Tape holding the portrait photo (image is ~square diagonal)
            UIFactory.TapeStrip(portraitWrap.transform, new Vector2(0.5f, 0.95f), 50, 46, -40f);

            // Identity column
            GameObject identityCol = UIFactory.Rect("Identity", headerRow.transform);
            UIFactory.VerticalGroup(identityCol, TextAnchor.UpperLeft, 3);
            identityCol.AddComponent<LayoutElement>().flexibleWidth = 1;

            nameLabel = UIFactory.Label("Name", identityCol.transform, "",
                22, FontStyle.Bold, UIFactory.TextDark);

            GameObject roleRow = UIFactory.Rect("RoleRow", identityCol.transform);
            UIFactory.HorizontalGroup(roleRow, TextAnchor.MiddleLeft, 10, expandHeight: false);

            roleLabel = UIFactory.Label("Role", roleRow.transform, "",
                14, FontStyle.Normal, UIFactory.TextSubtle);
            roleLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            difficultyTag = UIFactory.Label("Difficulty", roleRow.transform, "",
                12, FontStyle.Bold, UIFactory.TextMuted, TextAnchor.MiddleRight);

            personalityLabel = UIFactory.Label("Personality", identityCol.transform, "",
                14, FontStyle.Italic, UIFactory.TextHandwritten);
            personalityLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            personalityLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // Status badge
            statusRow = UIFactory.Rect("StatusRow", identityCol.transform);
            UIFactory.HorizontalGroup(statusRow, TextAnchor.MiddleLeft, 0,
                expandHeight: false, expandWidth: false);
            statusRow.AddComponent<LayoutElement>().preferredHeight = 22;

            GameObject badge = UIFactory.Rect("Badge", statusRow.transform);
            statusBadgeBg = badge.AddComponent<Image>();
            statusBadgeBg.color = UIFactory.StatusActive;
            LayoutElement bLE = badge.AddComponent<LayoutElement>();
            bLE.minWidth = 100;
            bLE.preferredHeight = 20;

            statusBadgeText = UIFactory.Label("BadgeText", badge.transform, "ACTIVE",
                12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform btRt = statusBadgeText.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = new Vector2(6, 0);
            btRt.offsetMax = new Vector2(-6, 0);

            // Lock banner
            lockBanner = UIFactory.Rect("LockBanner", identityCol.transform);
            lockBanner.AddComponent<Image>().color = UIFactory.LockBadge;
            lockBanner.AddComponent<LayoutElement>().minHeight = 30;
            lockText = UIFactory.Label("LockText", lockBanner.transform,
                "LOCKED - Flip all coworkers to unlock",
                13, FontStyle.Bold, UIFactory.LockBadgeText, TextAnchor.MiddleCenter);
            RectTransform ltRt = lockText.GetComponent<RectTransform>();
            ltRt.anchorMin = Vector2.zero;
            ltRt.anchorMax = Vector2.one;
            ltRt.offsetMin = new Vector2(8, 2);
            ltRt.offsetMax = new Vector2(-8, -2);
            lockBanner.SetActive(false);

            UIFactory.DashedLine("Sep1", body.transform);

            // Info section: two columns
            GameObject infoRow = UIFactory.Rect("InfoRow", body.transform);
            UIFactory.HorizontalGroup(infoRow, TextAnchor.UpperLeft, 20, expandHeight: true);
            infoRow.AddComponent<LayoutElement>().flexibleHeight = 1;

            // Left column: likes, dislikes, hint
            GameObject leftCol = UIFactory.Rect("LeftCol", infoRow.transform);
            UIFactory.VerticalGroup(leftCol, TextAnchor.UpperLeft, 6);
            leftCol.AddComponent<LayoutElement>().flexibleWidth = 1;

            likesHeader = UIFactory.Label("LikesH", leftCol.transform, "LIKES",
                13, FontStyle.Bold, UIFactory.TextSubtle);
            likesLabel = UIFactory.Label("Likes", leftCol.transform, "",
                15, FontStyle.Normal, UIFactory.TextMedium);
            likesLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            likesLabel.verticalOverflow = VerticalWrapMode.Overflow;

            UIFactory.DashedLine("LikesSep", leftCol.transform);

            dislikesHeader = UIFactory.Label("DislikesH", leftCol.transform, "DISLIKES",
                13, FontStyle.Bold, UIFactory.RedPen);
            dislikesLabel = UIFactory.Label("Dislikes", leftCol.transform, "",
                15, FontStyle.Normal, UIFactory.TextMedium);
            dislikesLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            dislikesLabel.verticalOverflow = VerticalWrapMode.Overflow;

            dislikesUnderline = UIFactory.ColorStrip("RedUnderline", leftCol.transform,
                UIFactory.RedPen, 2f);

            UIFactory.DashedLine("HintSep", leftCol.transform);

            hintLabel = UIFactory.Label("Hint", leftCol.transform, "",
                14, FontStyle.Italic, new Color32(120, 92, 48, 255));
            hintLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            hintLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // Right column: location + schedule
            GameObject rightCol = UIFactory.Rect("RightCol", infoRow.transform);
            UIFactory.VerticalGroup(rightCol, TextAnchor.UpperLeft, 6);
            rightCol.AddComponent<LayoutElement>().flexibleWidth = 1;

            locationLabel = UIFactory.Label("Location", rightCol.transform, "",
                15, FontStyle.Bold, UIFactory.TextDark);

            UIFactory.DashedLine("SchedSep", rightCol.transform);

            scheduleHeader = UIFactory.Label("SchedH", rightCol.transform, "DAILY ROUTINE",
                13, FontStyle.Bold, UIFactory.TextSubtle);
            scheduleLabel = UIFactory.Label("Schedule", rightCol.transform, "",
                14, FontStyle.Normal, UIFactory.TextMedium);
            scheduleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            scheduleLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // Back button
            UIFactory.DashedLine("BottomSep", body.transform);
            backButton = UIFactory.Button("BackBtn", body.transform,
                "< Back to Directory", UIFactory.ButtonBrown, UIFactory.ButtonBrownText, 15);
            backButton.onClick.AddListener(() => BackRequested?.Invoke());
        }

        // ---- Populate ----

        private void ShowFull(EmployeeProfileData profile)
        {
            Color32 idColor = UIFactory.GetIdentityColor(profile.ColorIdentity);
            colorBanner.color = idColor;
            folderTab.color = idColor;
            folderTab.gameObject.SetActive(true);

            nameLabel.text = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? "Unknown Employee" : profile.DisplayName;
            roleLabel.text = profile.Role + "  /  " + profile.ColorIdentity;
            difficultyTag.text = UIFactory.GetDifficultyLabel(profile.DifficultyTier);
            difficultyTag.color = UIFactory.GetDifficultyColor(profile.DifficultyTier);
            personalityLabel.text = "\u201CPlaceholder\u201D";
            lockBanner.SetActive(false);

            UpdateStatusBadge(profile.NpcId);
            SetPortrait(profile.Portrait);

            likesHeader.text = "LIKES";
            likesLabel.text = BuildBulletList(profile.Likes);
            dislikesHeader.text = "DISLIKES";
            dislikesLabel.text = BuildBulletList(profile.Dislikes);
            dislikesUnderline.gameObject.SetActive(
                profile.Dislikes != null && profile.Dislikes.Count > 0);

            hintLabel.text = string.IsNullOrWhiteSpace(profile.SabotageHint)
                ? "Intel: watch their routine. trigger what they hate."
                : "Intel: " + profile.SabotageHint;

            locationLabel.text = "Currently @ " + GetCurrentLocation(profile);
            scheduleHeader.text = "DAILY ROUTINE";
            scheduleLabel.text = BuildScheduleText(profile.ScheduleBlocks);
            backButton.gameObject.SetActive(true);
        }

        private void ShowLocked()
        {
            colorBanner.color = new Color32(42, 42, 48, 255);
            folderTab.color = new Color32(42, 42, 48, 255);
            folderTab.gameObject.SetActive(true);

            nameLabel.text = "[ CLASSIFIED ]";
            roleLabel.text = "Final Obstacle";
            difficultyTag.text = "BOSS";
            difficultyTag.color = UIFactory.GetDifficultyColor(EmployeeDifficultyTier.Final);
            personalityLabel.text = "\u201COnly destabilizes after everyone else has flipped.\u201D";
            lockBanner.SetActive(true);
            lockText.text = "DOSSIER LOCKED - Flip all coworkers to gain access";
            statusRow.SetActive(false);
            SetPortrait(null);

            likesHeader.text = "LIKES";
            likesLabel.text = "  ???";
            dislikesHeader.text = "DISLIKES";
            dislikesLabel.text = "  ???";
            dislikesUnderline.gameObject.SetActive(false);
            hintLabel.text = "Intel: complete all coworker FLIP OUTs to unlock.";

            locationLabel.text = "Currently @ ???";
            scheduleHeader.text = "DAILY ROUTINE";
            scheduleLabel.text = "  Schedule classified.";
            backButton.gameObject.SetActive(true);
        }

        private void ShowEmpty()
        {
            colorBanner.color = UIFactory.PortraitPlaceholder;
            folderTab.gameObject.SetActive(false);
            nameLabel.text = "No Profile";
            roleLabel.text = "";
            difficultyTag.text = "";
            personalityLabel.text = "";
            lockBanner.SetActive(false);
            statusRow.SetActive(false);
            SetPortrait(null);
            likesHeader.text = "";
            likesLabel.text = "";
            dislikesHeader.text = "";
            dislikesLabel.text = "";
            dislikesUnderline.gameObject.SetActive(false);
            hintLabel.text = "";
            locationLabel.text = "";
            scheduleHeader.text = "";
            scheduleLabel.text = "";
            backButton.gameObject.SetActive(true);
        }

        private void UpdateStatusBadge(string npcId)
        {
            if (progressTracker == null) { statusRow.SetActive(false); return; }

            statusRow.SetActive(true);
            IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps = progressTracker.GetSnapshots();
            for (int i = 0; i < snaps.Count; i++)
            {
                if (snaps[i].npcId == npcId)
                {
                    bool flipped = snaps[i].isFlippedOut;
                    statusBadgeText.text = flipped ? "FLIPPED OUT" : "ACTIVE";
                    statusBadgeBg.color = flipped ? UIFactory.StatusFlipped : UIFactory.StatusActive;
                    return;
                }
            }
            statusBadgeText.text = "ACTIVE";
            statusBadgeBg.color = UIFactory.StatusActive;
        }

        private void SetPortrait(Sprite sprite)
        {
            portrait.sprite = sprite;
            portrait.color = sprite != null ? Color.white : UIFactory.PortraitPlaceholder;
        }

        // ---- Helpers ----

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
    }
}
