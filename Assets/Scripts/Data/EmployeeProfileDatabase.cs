using System.Collections.Generic;
using UnityEngine;

namespace OfficeFlipOut.Data
{
    [CreateAssetMenu(menuName = "Office Flip Out/Employee Database", fileName = "EmployeeProfileDatabase")]
    public class EmployeeProfileDatabase : ScriptableObject
    {
        [SerializeField] private List<EmployeeProfileData> profiles = new List<EmployeeProfileData>();
        [SerializeField] private bool useBuiltInDefaultsWhenEmpty = true;

        private readonly List<EmployeeProfileData> runtimeProfiles = new List<EmployeeProfileData>();
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

        public IReadOnlyList<EmployeeProfileData> GetProfiles()
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
    }
}
