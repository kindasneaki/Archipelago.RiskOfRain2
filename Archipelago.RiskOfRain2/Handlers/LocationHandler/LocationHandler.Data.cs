using Archipelago.RiskOfRain2.Lookup;
using Archipelago.RiskOfRain2.UI;
using RoR2;
using System;
using System.Collections.Generic;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class LocationHandler
    {
        // setup all scene indexes as magic numbers
        // scenes from https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
        // scene id's will be incorrect when extra maps are included so make sure to call int index = GetSceneIndex(sceneName); when using sceneIndex
        // main scenes
        public const int ancientloft = 3;       // Aphelian Sanctuary
        public const int blackbeach = 7;        // Distant Roost
        public const int blackbeach2 = 8;       // Distant Roost (2)
        public const int lakes = 28;            // Verdant Falls
        public const int dampcavesimple = 10;   // Abyssal Depths
        public const int foggyswamp = 12;       // Wetland Aspect
        public const int frozenwall = 13;       // Rallypoint Delta
        public const int golemplains = 15;      // Titanic Plains
        public const int golemplains2 = 16;     // Titanic Plains (2)
        public const int goolake = 17;          // Abandoned Aqueduct
        public const int rootjungle = 35;       // Sundered Grove
        public const int shipgraveyard = 37;    // Siren's Call
        public const int skymeadow = 38;        // Sky Meadow
        public const int snowyforest = 39;      // Siphoned Forest
        public const int sulfurpools = 41;      // Sulfur Pools
        public const int wispgraveyard = 47;    // Scorched Acres
        // Seekers of the Storm
        public const int lakesnight = 34;       // Viscous Falls - Alternate stage to Verdant Falls
        public const int village = 54;          // Shattered Abodes
        public const int villagenight = 55;     // Disturbed Impact - Alternate stage to Shattered Abodes
        public const int lemuriantemple = 36;   // Reformed Altar
        public const int habitat = 21;          // Treeborn Colony
        public const int habitatfall = 22;      // Golden Dieback - Alternate stage to Treeborn Colony
        public const int helminthroost = 23;    // Helminth Hatchery
        public const int meridian = 40;         // Prime Meridian

        public static int sceneIndex = 0;
        public enum LocationTypes
        {
            chest,
            shrine,
            scavenger,
            radio_scanner,
            newt_altar,
            // NOTE add additional location types above this comment
            MAX // used to sent the length of LocationInformationTemplates
        }

        public static readonly string[] LocationTypesSlotName = new string[(int)LocationTypes.MAX] // use max to enforce correct amount of names
        {
            // These names should match those in the slot data
            "chestsPerStage",
            "shrinesPerStage",
            "scavengersPerStage",
            "scannerPerStage",
            "altarsPerStage"
        };

        public static readonly string[] LocationTypesShortName = new string[(int)LocationTypes.MAX] // use max to enforce correct amount of names
        {
            // These names are used for debug
            "chests",
            "shrines",
            "scavengers",
            "scanner",
            "altars"
        };

        /// <summary>
        /// These values are sourced from the RoR2 Archipelago world code.
        /// These are used to determine the id values of locations.
        /// </summary>
        private class ArchipelagoLocationOffsets
        {
            // these values come from worlds/ror2/Locations.py in Archipelago
            public const int ror2_locations_start_orderedstage = 38000 + 250;
            public static readonly int[] offset = new int[(int)LocationTypes.MAX + 1] // use max+1 to enforce correct amount of offsets
            {
                0,
                0 + 20,
                0 + 20 + 20,
                0 + 20 + 20 + 1,
                0 + 20 + 20 + 1 + 1,
                0 + 20 + 20 + 1 + 1 + 2
            };
            // NOTE offset[(int)LocationTypes.MAX] will give the size allocated to locations in each environment
            public static readonly int allocation = offset[(int)LocationTypes.MAX];
        }

        // Create a class to interface the template information.
        // This is so readabilty and the ability to index the template with the LocationTypes enum.
        public class LocationInformationTemplate
        {

            private int[] data = new int[(int)LocationTypes.MAX];

            public int this[int i]
            {
                get => data[i];
                set => data[i] = value; 
            }

            public int this[LocationTypes type]
            {
                get => data[(int)type];
                set => data[(int)type] = value;
            }

            /// <returns>The sum of all locations in the template.</returns>
            public int total()
            {
                int sum = 0;
                for (int type = 0; type < (int)LocationTypes.MAX; type++) sum += data[type];
                return sum;
            }
            public string scene()
            {
                SceneDef scene = LocationHandler.GetLocationScene();
                /*                Log.LogDebug($"{scene.sceneDefIndex} scene this");*/
                if (LocationNames.locationsNames.ContainsKey(sceneIndex))
                {
                    ArchipelagoLocationsInEnvironmentController.CurrentScene = $"{LocationNames.locationsNames[sceneIndex]}";
                    return $"{LocationNames.locationsNames[sceneIndex]}";
                }
                ArchipelagoLocationsInEnvironmentController.CurrentScene = $"Environment Location";
                return $"Environment Location";

                
            }

            public LocationInformationTemplate copy()
            {
                LocationInformationTemplate copy = new LocationInformationTemplate();
                for (int type = 0; type < (int)LocationTypes.MAX; type++) copy[type] = data[type];
                return copy;
            }
        }


        public static LocationInformationTemplate buildTemplateFromSlotData(Dictionary<string, object> SlotData)
        {
            LocationInformationTemplate locationtemplate = new LocationInformationTemplate();
            if (SlotData is not null)
            {
                // construct the find the amount of each type of location dictated by the slot data
                for (int type = 0; type < (int)LocationTypes.MAX; type++)
                {
                    // only set the value if the slot has the amoutn of locations for that type
                    if (SlotData.TryGetValue(LocationTypesSlotName[type], out var type_per_stage)) locationtemplate[type] = Convert.ToInt32(type_per_stage);
                }
            }
            return locationtemplate;
        }
    }
}
