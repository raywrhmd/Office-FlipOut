using System.Collections.Generic;

namespace OfficeFlipOut.Data
{
    public static class GddEmployeeSeeds
    {
        public static List<EmployeeProfileSeed> CreateDefaultSeeds()
        {
            return new List<EmployeeProfileSeed>
            {
                new EmployeeProfileSeed
                {
                    NpcId = "npc_1",
                    DisplayName = "Sandra Cain",
                    Role = "Coworker",
                    ColorIdentity = "Purple",
                    PersonalitySummary = "Chic and style-first.",
                    SabotageHint = "Hates the smell of fish.",
                    DifficultyTier = EmployeeDifficultyTier.Mid,
                    Likes = new List<string> { "Clean aesthetics", "Fresh office scent" },
                    Dislikes = new List<string> { "Microwave fish", "Messy desk props", "Loud disruptions" },
                    Schedule = new List<EmployeeScheduleSeed>
                    {
                        new EmployeeScheduleSeed { Label = "Planning", Location = "Desk Row B", StartHour = 8f, EndHour = 10f },
                        new EmployeeScheduleSeed { Label = "Kitchen Pass", Location = "Microwave Zone", StartHour = 10f, EndHour = 11f },
                        new EmployeeScheduleSeed { Label = "Client Prep", Location = "Meeting Area", StartHour = 11f, EndHour = 15f },
                        new EmployeeScheduleSeed { Label = "Desk Wrap", Location = "Desk Row B", StartHour = 15f, EndHour = 17f }
                    }
                },
                new EmployeeProfileSeed
                {
                    NpcId = "npc_2",
                    DisplayName = "Brutus Stragenoff",
                    Role = "Coworker",
                    ColorIdentity = "Red",
                    PersonalitySummary = "Temperamental and easy to provoke.",
                    SabotageHint = "Hates office birthdays.",
                    DifficultyTier = EmployeeDifficultyTier.Intro,
                    Likes = new List<string> { "Quiet workspace", "Perfectly organized filing cabinet" },
                    Dislikes = new List<string> { "Birthday cake", "Filing cabinet disorder" },
                    Schedule = new List<EmployeeScheduleSeed>
                    {
                        new EmployeeScheduleSeed { Label = "Desk Grind", Location = "Desk Row A", StartHour = 8f, EndHour = 11f },
                        new EmployeeScheduleSeed { Label = "Coffee Run", Location = "Break Room", StartHour = 11f, EndHour = 12f },
                        new EmployeeScheduleSeed { Label = "Desk Grind", Location = "Desk Row A", StartHour = 12f, EndHour = 17f }
                    }
                },
                new EmployeeProfileSeed
                {
                    NpcId = "npc_3",
                    DisplayName = "Tom T. Thomson",
                    Role = "Coworker",
                    ColorIdentity = "Green/Blue",
                    PersonalitySummary = "Chill and hard to rattle.",
                    SabotageHint = "Do what you can",
                    DifficultyTier = EmployeeDifficultyTier.Advanced,
                    Likes = new List<string> { "Low stress", "Clean air", "Quiet corners" },
                    Dislikes = new List<string> { "Cigarette smoke", "Persistent pranks", "Desk tampering" },
                    Schedule = new List<EmployeeScheduleSeed>
                    {
                        new EmployeeScheduleSeed { Label = "Deep Work", Location = "Back Desk", StartHour = 8f, EndHour = 12f },
                        new EmployeeScheduleSeed { Label = "Smoke Patrol", Location = "Hallway", StartHour = 12f, EndHour = 13f },
                        new EmployeeScheduleSeed { Label = "Deep Work", Location = "Back Desk", StartHour = 13f, EndHour = 17f }
                    }
                },
                new EmployeeProfileSeed
                {
                    NpcId = "boss_1",
                    DisplayName = "Da Boss",
                    Role = "Final Obstacle",
                    ColorIdentity = "Executive Black",
                    PersonalitySummary = "Mysterious and all-seeing. Only destabilizes after everyone else has flipped.",
                    SabotageHint = "Complete all coworker FLIP OUTs to unlock final pressure sequence.",
                    DifficultyTier = EmployeeDifficultyTier.Final,
                    StartsLocked = true,
                    RequiresAllCoworkersFlipped = true,
                    Likes = new List<string> { "Order", "Control", "Quiet office floor" },
                    Dislikes = new List<string> { "Open chaos", "Visible insubordination", "Total disruption" },
                    Schedule = new List<EmployeeScheduleSeed>
                    {
                        new EmployeeScheduleSeed { Label = "Executive Round", Location = "Boss Office", StartHour = 9f, EndHour = 12f },
                        new EmployeeScheduleSeed { Label = "Floor Sweep", Location = "Main Office", StartHour = 13f, EndHour = 15f },
                        new EmployeeScheduleSeed { Label = "Lockdown", Location = "Boss Office", StartHour = 15f, EndHour = 18f }
                    }
                }
            };
        }
    }
}
