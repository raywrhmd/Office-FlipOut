using System;
using System.Collections.Generic;
using UnityEngine;

namespace OfficeFlipOut.Data
{
    /// <summary>
    /// Authoring container for all NPC profiles. Public access always
    /// returns a deterministic order sorted by difficulty tier (then by
    /// display name as a tiebreak), so the clipboard directory, HUD
    /// roster, and "next objective" lookup all walk staff in the same
    /// easy-to-hard sequence regardless of how the asset was authored or
    /// what order Unity returned scene NPCs in.
    /// </summary>
    [CreateAssetMenu(menuName = "Office Flip Out/Employee Database", fileName = "EmployeeProfileDatabase")]
    public class EmployeeProfileDatabase : ScriptableObject
    {
        [SerializeField] private List<EmployeeProfileData> profiles = new List<EmployeeProfileData>();
        [SerializeField] private bool useBuiltInDefaultsWhenEmpty = true;

        private readonly List<EmployeeProfileData> runtimeProfiles = new List<EmployeeProfileData>();
        // Reusable scratch buffer for the sorted view. We don't cache the
        // sort across calls so the database stays self-healing in the face
        // of late asset deserialization or runtime SetProfiles edits.
        // With <=10 NPCs the cost is microseconds.
        private readonly List<EmployeeProfileData> sortedScratch = new List<EmployeeProfileData>();
        private static readonly List<EmployeeProfileData> EmptyProfiles = new List<EmployeeProfileData>();

        public int Count => GetProfiles().Count;

        public EmployeeProfileData GetProfileAt(int index)
        {
            IReadOnlyList<EmployeeProfileData> activeProfiles = GetProfiles();
            if (activeProfiles == null || activeProfiles.Count == 0)
            {
                return null;
            }

            if (index < 0)
            {
                index = activeProfiles.Count - 1;
            }
            else if (index >= activeProfiles.Count)
            {
                index = 0;
            }

            return activeProfiles[index];
        }

        public EmployeeProfileData GetProfileByNpcId(string npcId)
        {
            IReadOnlyList<EmployeeProfileData> activeProfiles = GetProfiles();
            if (activeProfiles == null || string.IsNullOrWhiteSpace(npcId))
            {
                return null;
            }

            for (int i = 0; i < activeProfiles.Count; i++)
            {
                EmployeeProfileData profile = activeProfiles[i];
                if (profile != null && profile.NpcId == npcId)
                {
                    return profile;
                }
            }

            return null;
        }

        public void SetProfiles(IList<EmployeeProfileData> newProfiles)
        {
            profiles = newProfiles != null ? new List<EmployeeProfileData>(newProfiles) : new List<EmployeeProfileData>();
            runtimeProfiles.Clear();
        }

        /// <summary>
        /// Returns the active profile list in stable (DifficultyTier, DisplayName)
        /// order. Falls back to seeding from <see cref="GddEmployeeSeeds"/>
        /// when no profiles are authored (and the toggle allows it). Computed
        /// fresh on every call so late-arriving deserialization or runtime
        /// SetProfiles edits never leave stale data on screen.
        /// </summary>
        public IReadOnlyList<EmployeeProfileData> GetProfiles()
        {
            IReadOnlyList<EmployeeProfileData> source = ResolveSource();
            return BuildSortedView(source);
        }

        private IReadOnlyList<EmployeeProfileData> ResolveSource()
        {
            if (profiles != null && profiles.Count > 0)
            {
                return profiles;
            }

            if (!useBuiltInDefaultsWhenEmpty)
            {
                return EmptyProfiles;
            }

            if (runtimeProfiles.Count == 0)
            {
                List<EmployeeProfileSeed> seeds = GddEmployeeSeeds.CreateDefaultSeeds();
                for (int i = 0; i < seeds.Count; i++)
                {
                    EmployeeProfileData profile = ScriptableObject.CreateInstance<EmployeeProfileData>();
                    profile.hideFlags = HideFlags.DontSave;
                    profile.ApplySeed(seeds[i]);
                    runtimeProfiles.Add(profile);
                }
            }

            return runtimeProfiles;
        }

        private List<EmployeeProfileData> BuildSortedView(IReadOnlyList<EmployeeProfileData> source)
        {
            sortedScratch.Clear();
            if (source == null || source.Count == 0)
            {
                return sortedScratch;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null) sortedScratch.Add(source[i]);
            }
            sortedScratch.Sort(CompareByDifficultyThenName);
            return sortedScratch;
        }

        private static int CompareByDifficultyThenName(EmployeeProfileData a, EmployeeProfileData b)
        {
            // null-safety: nulls (filtered out above) sink to the end.
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int tierDiff = (int)a.DifficultyTier - (int)b.DifficultyTier;
            if (tierDiff != 0) return tierDiff;

            string nameA = a.DisplayName ?? string.Empty;
            string nameB = b.DisplayName ?? string.Empty;
            return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
        }
    }
}
