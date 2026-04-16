using System;
using System.Collections.Generic;
using UnityEngine;

namespace OfficeFlipOut.Data
{
    public enum EmployeeDifficultyTier
    {
        Intro = 0,
        Mid = 1,
        Advanced = 2,
        Final = 3
    }

    [Serializable]
    public class EmployeeScheduleBlock
    {
        [SerializeField] private string label;
        [SerializeField] private string location;
        [SerializeField, Range(0f, 24f)] private float startHour;
        [SerializeField, Range(0f, 24f)] private float endHour = 1f;

        public string Label => label;
        public string Location => location;
        public float StartHour => startHour;
        public float EndHour => endHour;

        public EmployeeScheduleBlock(string blockLabel, string blockLocation, float blockStartHour, float blockEndHour)
        {
            label = blockLabel;
            location = blockLocation;
            startHour = Mathf.Clamp(blockStartHour, 0f, 24f);
            endHour = Mathf.Clamp(blockEndHour, 0f, 24f);
            if (endHour <= startHour)
            {
                endHour = Mathf.Min(24f, startHour + 1f);
            }
        }

        public EmployeeScheduleBlock()
        {
        }
    }

    [Serializable]
    public class EmployeeScheduleSeed
    {
        public string Label;
        public string Location;
        public float StartHour;
        public float EndHour;
    }

    [Serializable]
    public class EmployeeProfileSeed
    {
        public string NpcId;
        public string DisplayName;
        public string Role;
        public string ColorIdentity;
        public string PersonalitySummary;
        public string SabotageHint;
        public List<string> Likes = new List<string>();
        public List<string> Dislikes = new List<string>();
        public EmployeeDifficultyTier DifficultyTier;
        public bool StartsLocked;
        public bool RequiresAllCoworkersFlipped;
        public List<EmployeeScheduleSeed> Schedule = new List<EmployeeScheduleSeed>();
    }

    [CreateAssetMenu(menuName = "Office Flip Out/Employee Profile", fileName = "EmployeeProfile")]
    public class EmployeeProfileData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;
        [SerializeField] private string role;
        [SerializeField] private string colorIdentity;
        [SerializeField] private Sprite portrait;

        [Header("Personality")]
        [SerializeField, TextArea(2, 4)] private string personalitySummary;
        [SerializeField, TextArea(2, 4)] private string sabotageHint;

        [Header("Puzzle Data")]
        [SerializeField] private List<string> likes = new List<string>();
        [SerializeField] private List<string> dislikes = new List<string>();
        [SerializeField] private EmployeeDifficultyTier difficultyTier;

        [Header("Progression")]
        [SerializeField] private bool startsLocked;
        [SerializeField] private bool requiresAllCoworkersFlipped;

        [Header("Schedule")]
        [SerializeField] private List<EmployeeScheduleBlock> scheduleBlocks = new List<EmployeeScheduleBlock>();

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public string Role => role;
        public string ColorIdentity => colorIdentity;
        public Sprite Portrait => portrait;
        public string PersonalitySummary => personalitySummary;
        public string SabotageHint => sabotageHint;
        public IReadOnlyList<string> Likes => likes;
        public IReadOnlyList<string> Dislikes => dislikes;
        public EmployeeDifficultyTier DifficultyTier => difficultyTier;
        public bool StartsLocked => startsLocked;
        public bool RequiresAllCoworkersFlipped => requiresAllCoworkersFlipped;
        public IReadOnlyList<EmployeeScheduleBlock> ScheduleBlocks => scheduleBlocks;

        public void ApplySeed(EmployeeProfileSeed seed)
        {
            if (seed == null)
            {
                return;
            }

            npcId = seed.NpcId;
            displayName = seed.DisplayName;
            role = seed.Role;
            colorIdentity = seed.ColorIdentity;
            portrait = null;
            personalitySummary = seed.PersonalitySummary;
            sabotageHint = seed.SabotageHint;
            difficultyTier = seed.DifficultyTier;
            startsLocked = seed.StartsLocked;
            requiresAllCoworkersFlipped = seed.RequiresAllCoworkersFlipped;

            likes = seed.Likes != null ? new List<string>(seed.Likes) : new List<string>();
            dislikes = seed.Dislikes != null ? new List<string>(seed.Dislikes) : new List<string>();

            scheduleBlocks = new List<EmployeeScheduleBlock>();
            if (seed.Schedule != null)
            {
                for (int i = 0; i < seed.Schedule.Count; i++)
                {
                    EmployeeScheduleSeed scheduleSeed = seed.Schedule[i];
                    if (scheduleSeed == null)
                    {
                        continue;
                    }

                    scheduleBlocks.Add(new EmployeeScheduleBlock(
                        scheduleSeed.Label,
                        scheduleSeed.Location,
                        scheduleSeed.StartHour,
                        scheduleSeed.EndHour));
                }
            }
        }
    }
}
