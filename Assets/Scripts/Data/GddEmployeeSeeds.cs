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
                    PersonalitySummary = "Style-obsessed and image-conscious. She cracks when her desk gets messy or the office smells off.",
                    SabotageHint = "Spill a drink near her desk, microwave fish in her path, or steal one of her desk props.",
                    DifficultyTier = EmployeeDifficultyTier.Mid,
                    Likes = new List<string> { "Clean aesthetics", "Fresh office scent", "Orderly desk space" },
                    Dislikes = new List<string> { "Spilled drinks", "Microwave fish", "Desk props going missing" },
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
                    PersonalitySummary = "Short fuse, strong opinions, and a desk setup he expects to stay perfect.",
                    SabotageHint = "Bring birthday cake near him, then knock over his filing cabinet.",
                    DifficultyTier = EmployeeDifficultyTier.Intro,
                    Likes = new List<string> { "Quiet workspace", "Perfectly organized filing cabinet", "Everything in its place" },
                    Dislikes = new List<string> { "Birthday cake", "Filing cabinet disorder", "People touching his desk" },
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
                    PersonalitySummary = "Laid back, hard to annoy, and happiest when his workspace stays quiet and powered.",
                    SabotageHint = "Hit him with any thrown object, blast loud noise near his chill zone, and unplug his comfort gear.",
                    DifficultyTier = EmployeeDifficultyTier.Advanced,
                    Likes = new List<string> { "Low stress", "Clean air", "Quiet corners", "Charged devices" },
                    Dislikes = new List<string> { "Projectiles", "Loud noise", "Unplugged fan, monitor, or lava lamp" },
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
                    PersonalitySummary = "Locked behind the rest of the office. The final obstacle once every coworker has flipped out.",
                    SabotageHint = "Flip every coworker first, then the boss dossier unlocks.",
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
