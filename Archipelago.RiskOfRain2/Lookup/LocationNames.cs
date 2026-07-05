using System.Collections.Generic;

namespace Archipelago.RiskOfRain2.Lookup
{
    public class LocationNames
    {
        // Single source of truth for scene indexes.
        // scenes from https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
        // main scenes
        public const int arena = 4;             // Void Fields
        public const int lakes = 28;            // Verdant Falls
        public const int blackbeach = 7;        // Distant Roost
        public const int blackbeach2 = 8;       // Distant Roost (2)
        public const int dampcavesimple = 10;   // Abyssal Depths
        public const int foggyswamp = 12;       // Wetland Aspect
        public const int frozenwall = 13;       // Rallypoint Delta
        public const int golemplains = 15;      // Titanic Plains
        public const int golemplains2 = 16;     // Titanic Plains (2)
        public const int goolake = 17;          // Abandoned Aqueduct
        public const int moon2 = 32;            // Commencement
        public const int rootjungle = 35;       // Sundered Grove
        public const int shipgraveyard = 37;    // Siren's Call
        public const int skymeadow = 38;        // Sky Meadow
        public const int wispgraveyard = 47;    // Scorched Acres
        // Survivors of the Void
        public const int snowyforest = 39;      // Siphoned Forest
        public const int ancientloft = 3;       // Aphelian Sanctuary
        public const int sulfurpools = 41;      // Sulfur Pools
        public const int voidstage = 46;        // Void Locus
        public const int voidraid = 45;         // The Planetarium
        // Seekers of the Storm
        public const int lakesnight = 34;       // Viscous Falls - Alternate stage to Verdant Falls
        public const int village = 54;          // Shattered Abodes
        public const int villagenight = 55;     // Disturbed Impact - Alternate stage to Shattered Abodes
        public const int lemuriantemple = 36;   // Reformed Altar
        public const int habitat = 21;          // Treeborn Colony
        public const int habitatfall = 22;      // Golden Dieback - Alternate stage to Treeborn Colony
        public const int helminthroost = 23;    // Helminth Hatchery
        public const int meridian = 40;         // Prime Meridian
        // hidden realms
        public const int artifactworld = 5;     // Hidden Realm: Bulwark's Ambry
        public const int bazaar = 6;            // Hidden Realm: Bazaar Between Time
        public const int goldshores = 14;       // Hidden Realm: Gilded Coast
        public const int limbo = 27;            // Hidden Realm: A Moment, Whole
        public const int mysteryspace = 33;     // Hidden Realm: A Moment, Fractured

        public static readonly Dictionary<int, string> locationsNames = new()
        {
            { ancientloft, "Aphelian Sanctuary" },
            { blackbeach, "Distant Roost" },
            { blackbeach2, "Distant Roost (2)" },
            { lakes, "Verdant Falls"},
            { dampcavesimple, "Abyssal Depths" },
            { foggyswamp, "Wetland Aspect" },
            { frozenwall, "Rallypoint Delta" },
            { golemplains, "Titanic Plains" },
            { golemplains2, "Titanic Plains (2)" },
            { goolake, "Abandoned Aqueduct" },
            { rootjungle, "Sundered Grove" },
            { shipgraveyard, "Siren's Call" },
            { skymeadow, "Sky Meadow" },
            { snowyforest, "Siphoned Forest" },
            { sulfurpools, "Sulfur Pools" },
            { wispgraveyard, "Scorched Acres" },
            { moon2, "Commencement" },
            { arena, "Void Fields" },
            { voidstage, "Void Locus" },
            { voidraid, "The Planetarium" },
            { artifactworld, "Hidden Realm: Bulwark's Ambry"},
            { bazaar, "Hidden Realm: Bazaar Between Time"},
            { goldshores, "Hidden Realm: Gilded Coast" },
            { limbo, "Hidden Realm: A Moment, Whole"},
            { mysteryspace, "Hidden Realm: A Moment, Fractured" },
            { lakesnight, "Viscous Falls" },
            { village, "Shattered Abodes" },
            { villagenight, "Disturbed Impact" },
            { lemuriantemple, "Reformed Altar" },
            { habitat, "Treeborn Colony" },
            { habitatfall, "Golden Dieback" },
            { helminthroost, "Helminth Hatchery" },
            { meridian, "Prime Meridian" }
        };

        public static readonly Dictionary<int, string> cachedLocationsNames = new()
        {
            { ancientloft, "ancientloft" },
            { arena, "arena" },
            { artifactworld, "artifactworld" },
            { bazaar, "bazaar" },
            { blackbeach, "blackbeach" },
            { blackbeach2, "blackbeach2" },
            { dampcavesimple, "dampcavesimple" },
            { foggyswamp, "foggyswamp" },
            { frozenwall, "frozenwall" },
            { goldshores, "goldshores" },
            { golemplains, "golemplains" },
            { golemplains2, "golemplains2" },
            { goolake, "goolake" },
            { limbo, "limbo" },
            { lakes, "lakes"},
            { moon2, "moon2" },
            { mysteryspace, "mysteryspace" },
            { rootjungle, "rootjungle" },
            { shipgraveyard, "shipgraveyard" },
            { skymeadow, "skymeadow" },
            { snowyforest, "snowyforest" },
            { sulfurpools, "sulfurpools" },
            { voidraid, "voidraid" },
            { voidstage, "voidstage" },
            { wispgraveyard, "wispgraveyard" },
            { lakesnight, "lakesnight" },
            { village, "village" },
            { villagenight, "villagenight" },
            { lemuriantemple, "lemuriantemple" },
            { habitat, "habitat" },
            { habitatfall, "habitatfall" },
            { helminthroost, "helminthroost" },
            { meridian, "meridian" },
        };

        public string GetLocationName(string cachedName)
        {
            int sceneIndex = GetSceneIndex(cachedName);
            if (locationsNames.TryGetValue(sceneIndex, out string locationName))
            {
                return locationName;
            }
            return "";
        }

        public string GetLocationNameByIndex(int index)
        {
            if (locationsNames.TryGetValue(index, out string locationName))
            {
                return locationName;
            }
            return "";
        }
        public string GetCachedLocationNameByIndex(int index)
        {
            if (cachedLocationsNames.TryGetValue(index, out string cachedName))
            {
                return cachedName;
            }
            return "";
        }

        public bool LocationNamesContains(string sceneName)
        {
            return locationsNames.ContainsValue(sceneName);
        }

        public bool CachedLocationNamesContains(string cachedName)
        {
            return cachedLocationsNames.ContainsValue(cachedName);
        }

        public int GetSceneIndex(string cachedName)
        {
            foreach (var scene in cachedLocationsNames)
            {
                if (scene.Value == cachedName)
                {
                    return scene.Key;
                }
            }
            return 0;
        }

    }
}
