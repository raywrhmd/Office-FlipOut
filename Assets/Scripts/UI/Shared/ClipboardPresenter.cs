using System.Collections.Generic;
using System.Text;
using OfficeFlipOut.Data;
using OfficeFlipOut.Systems;
using UnityEngine;

namespace OfficeFlipOut.UI.Shared
{
    /// <summary>
    /// Single source of formatting / lock-state truth for the clipboard.
    /// Views (UI Toolkit binders) should call into this rather than re-deriving
    /// these strings or rules locally. Intentionally stateless and dependency-injected.
    /// </summary>
    public static class ClipboardPresenter
    {
        public const string LockedNameFallback = "[ CLASSIFIED ]";
        public const string LockedRoleFallback = "Final Obstacle";
        public const string LockedDifficultyFallback = "BOSS";
        public const string LockedDislikesFallback = "Hates: ???";
        public const string LockedLocationFallback = "@ ???";
        public const string LockedScheduleFallback = "  Schedule classified.";
        public const string LockedDossierBanner = "DOSSIER LOCKED - Flip all coworkers to gain access";
        public const string LockedHintFallback = "Intel: complete all coworker FLIP OUTs to unlock.";
        public const string EmptyDislikesText = "Hates: nothing obvious";
        public const string DefaultPersonalityQuote = "\u201CMaintains a very normal office presence.\u201D";
        public const string LockedPersonalityQuote = "\u201COnly destabilizes after everyone else has flipped.\u201D";

        // ----------------------------------------------------------------
        // Lock state
        // ----------------------------------------------------------------

        public static bool IsLocked(EmployeeProfileData profile, ProgressTracker progressTracker)
        {
            if (profile == null) return false;
            bool locked = profile.StartsLocked;
            if (locked && profile.RequiresAllCoworkersFlipped && progressTracker != null)
            {
                locked = !progressTracker.AreAllCoworkersFlipped(profile.NpcId);
            }
            return locked;
        }

        // ----------------------------------------------------------------
        // Schedule / location
        // ----------------------------------------------------------------

        public static string GetCurrentLocation(EmployeeProfileData profile)
        {
            if (profile == null || profile.ScheduleBlocks == null || profile.ScheduleBlocks.Count == 0)
            {
                return "Unknown";
            }

            float nowHour = (Time.time / 60f) % 24f;
            for (int i = 0; i < profile.ScheduleBlocks.Count; i++)
            {
                EmployeeScheduleBlock b = profile.ScheduleBlocks[i];
                if (b != null && b.StartHour <= nowHour && nowHour < b.EndHour)
                {
                    return string.IsNullOrWhiteSpace(b.Location) ? "Unknown" : b.Location;
                }
            }

            EmployeeScheduleBlock fallback = profile.ScheduleBlocks[0];
            return fallback != null && !string.IsNullOrWhiteSpace(fallback.Location)
                ? fallback.Location : "Unknown";
        }

        public static string BuildScheduleText(IReadOnlyList<EmployeeScheduleBlock> blocks)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return "  No schedule data.";
            }

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

        // ----------------------------------------------------------------
        // Text formatting
        // ----------------------------------------------------------------

        public static string FormatDislikeSummary(IReadOnlyList<string> dislikes)
        {
            if (dislikes == null || dislikes.Count == 0) return EmptyDislikesText;
            if (dislikes.Count == 1) return "Hates: " + dislikes[0];
            if (dislikes.Count == 2) return "Hates: " + dislikes[0] + ", " + dislikes[1];
            return "Hates: " + dislikes[0] + ", " + dislikes[1] + " (+" + (dislikes.Count - 2) + " more)";
        }

        public static string FormatPersonalityQuote(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary)) return "\u201CNo intel on personality yet.\u201D";
            return "\u201C" + summary + "\u201D";
        }

        public static string BuildBulletList(IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0) return "  None listed";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                sb.Append("  \u2022 ");
                sb.AppendLine(items[i]);
            }
            return sb.ToString();
        }

        public static string FormatSabotageHint(string hint)
        {
            return string.IsNullOrWhiteSpace(hint)
                ? "Intel: watch their routine. trigger what they hate."
                : "Intel: " + hint;
        }

        // ----------------------------------------------------------------
        // Difficulty / identity color tokens
        // ----------------------------------------------------------------

        public static string GetDifficultyLabel(EmployeeDifficultyTier tier)
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

        public static Color32 GetDifficultyColor(EmployeeDifficultyTier tier)
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

        public static Color32 GetIdentityColor(string colorIdentity)
        {
            if (string.IsNullOrWhiteSpace(colorIdentity)) return new Color32(140, 140, 140, 255);
            string key = colorIdentity.Trim().ToLowerInvariant();
            if (key.Contains("purple")) return new Color32(128, 70, 160, 255);
            if (key.Contains("red")) return new Color32(195, 60, 50, 255);
            if (key.Contains("green") || key.Contains("blue")) return new Color32(55, 140, 130, 255);
            if (key.Contains("black") || key.Contains("executive")) return new Color32(42, 42, 48, 255);
            if (key.Contains("orange")) return new Color32(210, 130, 50, 255);
            return new Color32(140, 140, 140, 255);
        }

        // ----------------------------------------------------------------
        // Status helpers
        // ----------------------------------------------------------------

        public enum NpcStatus
        {
            Calm,
            Agitated,
            FlippedOut,
            Locked
        }

        public static NpcStatus DeriveStatus(ProgressTracker.EmployeeProgressSnapshot snap, bool locked)
        {
            if (locked) return NpcStatus.Locked;
            if (snap == null) return NpcStatus.Calm;
            if (snap.isFlippedOut) return NpcStatus.FlippedOut;
            return snap.currentRage > 0 ? NpcStatus.Agitated : NpcStatus.Calm;
        }

        public static string StatusLabel(NpcStatus status)
        {
            switch (status)
            {
                case NpcStatus.FlippedOut: return "FLIPPED OUT";
                case NpcStatus.Agitated: return "AGITATED";
                case NpcStatus.Locked: return "LOCKED";
                default: return "ACTIVE";
            }
        }

        public static string StatusUssClass(NpcStatus status)
        {
            switch (status)
            {
                case NpcStatus.FlippedOut: return "status-flipped";
                case NpcStatus.Locked: return "npc-locked";
                default: return "status-active";
            }
        }

        // ----------------------------------------------------------------
        // Snapshot lookup
        // ----------------------------------------------------------------

        public static ProgressTracker.EmployeeProgressSnapshot FindSnapshot(
            ProgressTracker tracker, string npcId)
        {
            if (tracker == null || string.IsNullOrWhiteSpace(npcId)) return null;
            IReadOnlyList<ProgressTracker.EmployeeProgressSnapshot> snaps = tracker.GetSnapshots();
            for (int i = 0; i < snaps.Count; i++)
            {
                if (snaps[i].npcId == npcId) return snaps[i];
            }
            return null;
        }

        // ----------------------------------------------------------------
        // Per-NPC task labels (used by Progress panel rows)
        // ----------------------------------------------------------------

        /// <summary>
        /// Stable string keys identifying the kind of sabotage trigger a row
        /// represents. The presenter intentionally keeps these as opaque
        /// strings so it does not need to depend on Sprite or Resources.Load -
        /// the view layer maps the key to whichever icon (if any) it has on
        /// hand. Use <see cref="TriggerIconKeys.None"/> for label-only chips.
        /// </summary>
        public static class TriggerIconKeys
        {
            public const string None = "";
            public const string SpillDrink = "spill_drink";
            public const string MicrowaveFish = "fish_smell";
            public const string StealObject = "steal_object";
            public const string BringObjectNear = "bring_near";
            public const string KnockOver = "knock_over";
            public const string ThrowProjectile = "throw_projectile";
            public const string MakeLoudNoise = "loud_noise";
            public const string UnplugDevice = "unplug_device";
        }

        public readonly struct TaskRow
        {
            public readonly string Label;
            public readonly bool Done;
            public readonly string IconKey;

            public TaskRow(string label, bool done)
                : this(label, done, TriggerIconKeys.None) { }

            public TaskRow(string label, bool done, string iconKey)
            {
                Label = label;
                Done = done;
                IconKey = iconKey ?? TriggerIconKeys.None;
            }
        }

        public static List<TaskRow> BuildTaskRows(ProgressTracker.EmployeeProgressSnapshot snap)
        {
            List<TaskRow> rows = new List<TaskRow>();
            if (snap == null) return rows;

            int taskCount = Mathf.Clamp(snap.requiredSignals, 1, snap.npcId == NpcIds.Brutus ? 2 : 3);

            if (snap.npcId == NpcIds.Sandra)
            {
                if (taskCount >= 1) rows.Add(new TaskRow("Spill drink near desk", snap.spilledDrink, TriggerIconKeys.SpillDrink));
                if (taskCount >= 2) rows.Add(new TaskRow("Microwave fish nearby", snap.microwavedFish, TriggerIconKeys.MicrowaveFish));
                if (taskCount >= 3) rows.Add(new TaskRow("Steal desk prop", snap.tookStapler, TriggerIconKeys.StealObject));
            }
            else if (snap.npcId == NpcIds.Brutus)
            {
                if (taskCount >= 1) rows.Add(new TaskRow("Bring birthday cake near", snap.microwavedFish, TriggerIconKeys.BringObjectNear));
                if (taskCount >= 2) rows.Add(new TaskRow("Knock over filing cabinet", snap.tookStapler, TriggerIconKeys.KnockOver));
            }
            else if (snap.npcId == NpcIds.Tommy)
            {
                if (taskCount >= 1) rows.Add(new TaskRow("Throw something at him", snap.spilledDrink, TriggerIconKeys.ThrowProjectile));
                if (taskCount >= 2) rows.Add(new TaskRow("Make loud noise nearby", snap.microwavedFish, TriggerIconKeys.MakeLoudNoise));
                if (taskCount >= 3) rows.Add(new TaskRow("Unplug device", snap.tookStapler, TriggerIconKeys.UnplugDevice));
            }
            else
            {
                if (taskCount >= 1) rows.Add(new TaskRow("Spill drink", snap.spilledDrink, TriggerIconKeys.SpillDrink));
                if (taskCount >= 2) rows.Add(new TaskRow("Microwave fish", snap.microwavedFish, TriggerIconKeys.MicrowaveFish));
                if (taskCount >= 3) rows.Add(new TaskRow("Steal object", snap.tookStapler, TriggerIconKeys.StealObject));
            }

            return rows;
        }
    }
}
