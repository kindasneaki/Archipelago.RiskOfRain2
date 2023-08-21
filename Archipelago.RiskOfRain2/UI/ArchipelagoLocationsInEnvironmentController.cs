using Archipelago.RiskOfRain2.Handlers;
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
                return $"{count.scene()}: {count[LocationHandler.LocationTypes.chest]}/{count[LocationHandler.LocationTypes.shrine]}/{count[LocationHandler.LocationTypes.scavenger]}/{count[LocationHandler.LocationTypes.radio_scanner]}/{count[LocationHandler.LocationTypes.newt_altar]}";
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

        internal static LocationHandler.LocationInformationTemplate count = new LocationHandler.LocationInformationTemplate();

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
