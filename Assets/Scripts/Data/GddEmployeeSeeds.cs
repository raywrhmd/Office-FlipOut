using System.Collections.Generic;

namespace OfficeFlipOut.Data
{
    /// <summary>
    /// Default GDD-derived seed data for every NPC in the game.
    /// Order here is the canonical authoring order: easiest tier first,
    /// then mid, advanced, and the final boss. Runtime consumers
    /// (clipboard directory, HUD roster, objective lookup) all funnel
    /// through <see cref="EmployeeProfileDatabase.GetProfiles"/>, which
    /// re-sorts by (DifficultyTier, DisplayName) so this list also
    /// dictates the editor-inspector and re-generator output order.
    /// Each NPC's <c>SabotageHint</c> / <c>Likes</c> / <c>Dislikes</c>
    /// must mirror the signals consumed by <see cref="Systems.ProgressTracker"/>
    /// for that NpcId; please update both sides when adding triggers.
    /// </summary>
    public static class GddEmployeeSeeds
    {
        public static List<EmployeeProfileSeed> CreateDefaultSeeds()
        {
            return new List<EmployeeProfileSeed>
            {
                // --- Intro tier --------------------------------------------------
                // Brutus: only 2 triggers, both spatially obvious - cake near
                // him and the filing cabinet on his route. Used as the tutorial
                // coworker.
                new EmployeeProfileSeed
                {
                    NpcId = NpcIds.Brutus,
                    DisplayName = "Brutus Stragenoff",
                    Role = "Coworker",
                    ColorIdentity = "Red",
                    PersonalitySummary = "Short fuse, strong opinions, and a desk setup he expects to stay perfect.",
                    SabotageHint = "Bring birthday cake near him, then knock over his filing cabinet.",
                    DifficultyTier = EmployeeDifficultyTier.Intro,
                    Likes = new List<string>
                    {
                        "Quiet workspace",
                        "Perfectly organized filing cabinet",
                        "Everything in its place"
                    },
                    Dislikes = new List<string>
                    {
                        "Birthday cake near him",
                        "Filing cabinet disorder",
                        "People touching his desk"
                    }
                },

                // --- Mid tier ----------------------------------------------------
                // Sandra: 3 desk-area sabotages (spill / fish / steal). Teaches
                // the player to combine kitchen + desk routes.
                new EmployeeProfileSeed
                {
                    NpcId = NpcIds.Sandra,
                    DisplayName = "Sandra Cain",
                    Role = "Coworker",
                    ColorIdentity = "Purple",
                    PersonalitySummary = "Style-obsessed and image-conscious. She cracks when her desk gets messy or the office smells off.",
                    SabotageHint = "Spill a drink near her desk, microwave fish in her path, and steal one of her desk props.",
                    DifficultyTier = EmployeeDifficultyTier.Mid,
                    Likes = new List<string>
                    {
                        "Clean aesthetics",
                        "Fresh office scent",
                        "Orderly desk space"
                    },
                    Dislikes = new List<string>
                    {
                        "Spilled drinks near her desk",
                        "Microwaved fish",
                        "Desk props going missing"
                    }
                },

                // --- Advanced tier -----------------------------------------------
                // Tommy: 3 environmental sabotages spread across the office
                // (projectile / loud noise / unplugged gadgets). Required
                // signals: HitNpcWithProjectile, MakeLoudNoise, UnplugDevice.
                new EmployeeProfileSeed
                {
                    NpcId = NpcIds.Tommy,
                    DisplayName = "Tom T. Thomson",
                    Role = "Coworker",
                    ColorIdentity = "Green/Blue",
                    PersonalitySummary = "Laid back until something hits him, the office gets loud, or his comfort gear stops working. Then he loses his cool fast.",
                    SabotageHint = "Throw something at him, make loud noise on his route, and unplug his comfort gear.",
                    DifficultyTier = EmployeeDifficultyTier.Advanced,
                    Likes = new List<string>
                    {
                        "Low stress",
                        "Quiet corners",
                        "Charged devices"
                    },
                    Dislikes = new List<string>
                    {
                        "Things thrown at him",
                        "Loud noise on his route",
                        "Unplugged fan, monitor, or lava lamp"
                    }
                },

                // --- Final tier --------------------------------------------------
                // Da Boss: starts locked, unlocks once every coworker has
                // flipped. Single objective: trigger him after the office is
                // already in chaos.
                new EmployeeProfileSeed
                {
                    NpcId = NpcIds.Boss,
                    DisplayName = "Da Boss",
                    Role = "Final Obstacle",
                    ColorIdentity = "Executive Black",
                    PersonalitySummary = "Locked behind the rest of the office. The final obstacle once every coworker has flipped out.",
                    SabotageHint = "Flip every coworker first, then the boss dossier unlocks.",
                    DifficultyTier = EmployeeDifficultyTier.Final,
                    StartsLocked = true,
                    RequiresAllCoworkersFlipped = true,
                    Likes = new List<string>
                    {
                        "Order",
                        "Control",
                        "A quiet office floor"
                    },
                    Dislikes = new List<string>
                    {
                        "Open chaos",
                        "Visible insubordination",
                        "Total office disruption"
                    }
                }
            };
        }
    }
}
