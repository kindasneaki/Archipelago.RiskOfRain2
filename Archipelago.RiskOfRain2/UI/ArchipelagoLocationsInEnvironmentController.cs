using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using R2API.Utils;
using RoR2.UI;
using static RoR2.UI.ObjectivePanelController;

namespace Archipelago.RiskOfRain2.UI
{
    public class ArchipelagoLocationsInEnvironmentController
    {
        public class ChecksInEnvironment : ObjectiveTracker
        {
            public override string GenerateString()
            {
                return $"Environment locations: {chest_count}/{shrine_count}/{scavenger_count}/{radio_scanner_count}/{newt_alter_count}";
            }

            public override bool IsDirty()
            {
                return true;
            }
        }

        static ArchipelagoLocationsInEnvironmentController()
        {
            ObjectivePanelController.collectObjectiveSources += ObjectivePanelController_collectObjectiveSources;
        }

        public static void disable()
        {
            ObjectivePanelController.collectObjectiveSources -= ObjectivePanelController_collectObjectiveSources;
        }

        private static void ObjectivePanelController_collectObjectiveSources(RoR2.CharacterMaster arg1, List<ObjectiveSourceDescriptor> arg2)
        {
            if (addObjective)
            {
                arg2.Add(new ObjectiveSourceDescriptor()
                {
                    master = arg1,
                    objectiveType = typeof(ChecksInEnvironment),
                    source = null
                });
            }
        }

        public static int chest_count { get; set; }
        public static int shrine_count { get; set; }
        public static int scavenger_count { get; set; }
        public static int radio_scanner_count { get; set; }
        public static int newt_alter_count { get; set; }

        private static bool addObjective;

        public static void AddObjective()
        {
            addObjective = true;
        }

        public static void RemoveObjective()
        {
            addObjective = false;
        }
    }
}
