using Archipelago.RiskOfRain2.Lookup;
using System.Collections.Generic;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class StageBlockerHandler
    {
        public LocationNames locationsNames = new LocationNames();

        // Stage Progression system
        public static Dictionary<string, bool> stageUnlocks = new()
        {
            { "Stage 1", false },
            { "Stage 2", false },
            { "Stage 3", false },
            { "Stage 4", false },

        };
        public static int amountOfStages = 0;

        /**
         * Stage progression is static, so it outlives both the handler and the run.
         * The server re-sends every stage item on connect, so this must be called once per connect
         * before those items are applied.
         */
        public static void ResetStageProgression()
        {
            stageUnlocks = new Dictionary<string, bool>
            {
                { "Stage 1", false },
                { "Stage 2", false },
                { "Stage 3", false },
                { "Stage 4", false },
            };
            amountOfStages = 0;
        }

        public readonly Dictionary<string, int> stageLookup = new()
        {
            { "ancientloft", 1 },
            { "dampcavesimple", 3 },
            { "foggyswamp", 1 },
            { "frozenwall", 2 },
            { "goolake", 1 },
            { "rootjungle", 3 },
            { "shipgraveyard", 3 },
            { "skymeadow", 4 },
            { "sulfurpools", 2 },
            { "wispgraveyard", 2 },
            { "lemuriantemple", 1 },
            { "habitat", 2 },
            { "habitatfall", 2 },
            { "helminthroost", 4 },
            { "meridian", 3 },
            { "nest", 1 },
            { "ironalluvium", 2 },
            { "ironalluvium2", 2 },
            { "conduitcanyon", 3 },
            { "repurposedcrater", 3 },
        };

        // Used to display the full location names in chat when a stage is needed to progress
        public readonly Dictionary<string, string> locationNames = new()
        {
            { "ancientloft", "Aphelian Sanctuary" },
            { "dampcavesimple", "Abyssal Depths" },
            { "foggyswamp", "Wetland Aspect" },
            { "frozenwall", "Rallypoint Delta" },
            { "goolake", "Abandoned Aqueduct" },
            { "rootjungle", "Sundered Grove" },
            { "shipgraveyard", "Siren's Call" },
            { "skymeadow", "Sky Meadow" },
            { "sulfurpools", "Sulfur Pools" },
            { "wispgraveyard", "Scorched Acres" },
            { "lemuriantemple", "Reformed Altar" },
            { "habitat", "Treeborn Colony" },
            { "habitatfall", "Golden Dieback" },
            { "helminthroost", "Helminhe Hatchery" },
            { "meridian", "Prime Meridian" },
            { "nest", "Pretender's Precipice" },
            { "ironalluvium", "Iron Alluvium" },
            { "ironalluvium2", "Iron Auroras" },
            { "conduitcanyon", "Conduit Canyon" },
            { "repurposedcrater", "Repurposed Crater" },
        };

        public readonly Dictionary <string, string> dlcLookup = new()
        {
            { "snowyforest", "sotv" },
            { "ancientloft", "sotv" },
            { "sulfurpools", "sotv" },
            { "village", "sots" },
            { "villagenight", "sots" },
            { "lakesnight", "sots" },
            { "lemuriantemple", "sots" },
            { "habitat", "sots" },
            { "habitatfall", "sots" },
            { "helminthroost", "sots" },
            { "nest", "alloyed" },
            { "ironalluvium", "alloyed" },
            { "ironalluvium2", "alloyed" },
            { "conduitcanyon", "alloyed" },
            { "repurposedcrater", "alloyed" },
        };

        public static bool progressivesStages = false;
        public static bool showSeerPortals = false;
        public static string revertToBeginningMessage = "";
    }
}
