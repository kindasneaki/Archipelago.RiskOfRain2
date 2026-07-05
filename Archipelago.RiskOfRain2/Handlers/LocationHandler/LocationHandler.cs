using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.UI;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.Console;
using RoR2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Networking;
using R2API.Utils;
using R2API.Networking;
using R2API.Networking.Interfaces;
using Archipelago.RiskOfRain2.Lookup;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class LocationHandler : IHandler
    {
        // NOTE every mention of a "location" refers to the archipelago location checks
        // NOTE every mention of a "environment" refers to the risk of rain 2 scenes that are loaded and played

        private ArchipelagoSession session;
        private LocationInformationTemplate originallocationstemplate;
        private Dictionary<int, LocationInformationTemplate> currentlocations;

        public LocationHandler(ArchipelagoSession session, LocationInformationTemplate locationstemplate)
        {
            Log.LogDebug($"Location handler constructor.");
            this.session = session;
            originallocationstemplate = locationstemplate.copy();
            currentlocations = new Dictionary<int, LocationInformationTemplate>();


            InitialSetupLocationDict(locationstemplate);
        }

        /// <summary>
        /// Calling adds the location template to each environment so they can be individually tracked later.
        /// </summary>
        /// <param name="locationstemplate">Template to assign to all relevant environments.</param>
        // TODO this should probably become generic so that environment sets can be passed in (e.g. normal environments, simulacrum environments, etc)
        private void InitialSetupLocationDict(LocationInformationTemplate locationstemplate)
        {
            currentlocations.Add(LocationNames.ancientloft,       locationstemplate); // Aphelian Sanctuary
            currentlocations.Add(LocationNames.blackbeach,        locationstemplate); // Distant Roost
            currentlocations.Add(LocationNames.blackbeach2,       locationstemplate); // Distant Roost
            currentlocations.Add(LocationNames.lakes,             locationstemplate); // Verdant Falls
            currentlocations.Add(LocationNames.dampcavesimple,    locationstemplate); // Abyssal Depths
            currentlocations.Add(LocationNames.foggyswamp,        locationstemplate); // Wetland Aspect
            currentlocations.Add(LocationNames.frozenwall,        locationstemplate); // Rallypoint Delta
            currentlocations.Add(LocationNames.golemplains,       locationstemplate); // Titanic Plains
            currentlocations.Add(LocationNames.golemplains2,      locationstemplate); // Titanic Plains
            currentlocations.Add(LocationNames.goolake,           locationstemplate); // Abandoned Aqueduct
            currentlocations.Add(LocationNames.rootjungle,        locationstemplate); // Sundered Grove
            currentlocations.Add(LocationNames.shipgraveyard,     locationstemplate); // Siren's Call
            currentlocations.Add(LocationNames.skymeadow,         locationstemplate); // Sky Meadow
            currentlocations.Add(LocationNames.snowyforest,       locationstemplate); // Siphoned Forest
            currentlocations.Add(LocationNames.sulfurpools,       locationstemplate); // Sulfur Pools
            currentlocations.Add(LocationNames.wispgraveyard,     locationstemplate); // Scorched Acres
            // Seekers of the Storm
            currentlocations.Add(LocationNames.lakesnight,        locationstemplate); // Viscous Falls
            currentlocations.Add(LocationNames.village,           locationstemplate); // Shattered Abodes
            currentlocations.Add(LocationNames.villagenight,      locationstemplate); // Disturbed Impact
            currentlocations.Add(LocationNames.lemuriantemple,    locationstemplate); // Reformed Altar
            currentlocations.Add(LocationNames.habitat,           locationstemplate); // Treeborn Colony
            currentlocations.Add(LocationNames.habitatfall,       locationstemplate); // Golden Dieback
            currentlocations.Add(LocationNames.helminthroost,    locationstemplate);  // Helminth Hatchery
            // TODO separate out the DLC locations
        }

        /// <summary>
        /// This is used to have the location handler catch up to the archipelago session.
        /// This is because the player may have completed checks, died, and restarted the session and we do not need to have the player repeat checks.
        /// </summary>
        public void CatchUpSceneLocations(string sceneName)
        {
            int index = GetSceneIndex(sceneName);
            Dictionary<int, LocationInformationTemplate> locationscopy = currentlocations.ToDictionary(k => k.Key, k => k.Value.copy());
            if (!locationscopy.TryGetValue(index, out LocationInformationTemplate location)) {
                return;
            }

            ReadOnlyCollection<long> completedchecks = session.Locations.AllLocationsChecked;
            int environment_start_id = index * ArchipelagoLocationOffsets.allocation + ArchipelagoLocationOffsets.ror2_locations_start_orderedstage;

            Log.LogDebug($"Doing catch up on environment: index {index}, stage name {sceneName}");
            Log.LogDebug($"environment_start_id {environment_start_id}");
            for (int type = 0; type < (int)LocationTypes.MAX; type++)
            {
                for (int n = originallocationstemplate[type] - location[type]; n < originallocationstemplate[type]; n++)
                {
                    // check each location if it has been seen
                    if (completedchecks.Contains(n + ArchipelagoLocationOffsets.offset[type] + environment_start_id))
                    {
                        location[type]--; // a location completed has been found for this environment
                    }
                    // if we see a location missing, imply the ones that succeed it are also missing
                    else break;
                }
                Log.LogDebug($"caught up to {LocationTypesShortName[type]} {location[type]}");
            }

            currentlocations[index] = location;
        }
        public void Hook()
        {
            // Etc
            On.RoR2.SceneCatalog.OnActiveSceneChanged += SceneCatalog_OnActiveSceneChanged;
            On.RoR2.SceneExitController.OnDestroy += SceneExitController_OnDestroy;
            On.RoR2.SceneInfo.Awake += SceneInfo_Awake;
            On.RoR2.SceneCollection.AddToWeightedSelection += SceneCollection_AddToWeightedSelection;
            // Chests
            On.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath += SacrificeArtifactManager_OnServerCharacterDeath;
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_ChestDrop;
            // Shrines
            On.RoR2.PortalStatueBehavior.GrantPortalEntry += PortalStatueBehavior_GrantPortalEntry_Gold;
            On.RoR2.ShrineBloodBehavior.AddShrineStack += ShrineBloodBehavior_AddShrineStack;
            On.RoR2.CharacterMaster.GiveMoney += CharacterMaster_GiveMoney;
            On.RoR2.ShrineChanceBehavior.AddShrineStack += ShrineChanceBehavior_AddShrineStack;
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_ChanceShrine;
            On.RoR2.ShrineCombatBehavior.AddShrineStack += ShrineCombatBehavior_AddShrineStack;
            On.RoR2.ShrineRestackBehavior.AddShrineStack += ShrineRestackBehavior_AddShrineStack;
            On.RoR2.BossGroup.DropRewards += BossGroup_DropRewards;
            On.RoR2.ShrineHealingBehavior.AddShrineStack += ShrineHealingBehavior_AddShrineStack;
            On.RoR2.ShrineColossusAccessBehavior.OnInteraction += ShrineColossusAccessBehavior_OnInteraction;
            // Scavengers
            On.EntityStates.ScavBackpack.Opening.OnEnter += Opening_OnEnter;
            On.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop_Scavenger;
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_Scavenger;
            // Void Triple Chest
           /* On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
            On.RoR2.OptionChestBehavior.ItemDrop += OptionChestBehavior_ItemDrop;
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3;*/
            // Radio Scanners
            On.RoR2.SceneDirector.PopulateScene += SceneDirector_PopulateScene;
            On.RoR2.RadiotowerTerminal.GrantUnlock += RadiotowerTerminal_GrantUnlock;
            ArchipelagoConsoleCommand.OnArchipelagoHighlightSatelliteCommandCalled += ArchipelagoConsoleCommand_OnArchipelagoHighlightSatelliteCommandCalled;
            ArchipelagoConsoleCommand_OnArchipelagoHighlightSatelliteCommandCalled(ArchipelagoPlugin.SatelliteEntry.Value);
            // Newt Altars
            On.RoR2.PortalStatueBehavior.GrantPortalEntry += PortalStatueBehavior_GrantPortalEntry_Blue;
            // Highlight Satellite
            
        }



        /*        private void PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig, GenericPickupController.CreatePickupInfo pickupInfo, UnityEngine.Vector3 position, UnityEngine.Vector3 velocity)
                {
                    throw new NotImplementedException();
                }

                private void OptionChestBehavior_ItemDrop(On.RoR2.OptionChestBehavior.orig_ItemDrop orig, OptionChestBehavior self)
                {
                    if (blockVoidTriple)
                    {
                        Log.LogDebug("Blocked triple spawn");
                        return;
                    }
                    orig(self);
                }*/

        /*        private void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
                {
                    Log.LogDebug($"Purchase Interaction {self.name} activator {activator.name}");
                    if (self.name == "VoidTriple(Clone)") blockVoidTriple = true;
                    orig(self, activator);
                }*/

        public void UnHook()
        {
            // Etc
            On.RoR2.SceneCatalog.OnActiveSceneChanged -= SceneCatalog_OnActiveSceneChanged;
            On.RoR2.SceneExitController.OnDestroy -= SceneExitController_OnDestroy;
            On.RoR2.SceneInfo.Awake -= SceneInfo_Awake;
            On.RoR2.SceneCollection.AddToWeightedSelection -= SceneCollection_AddToWeightedSelection;
            // Chests
            On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop_Chest;
            On.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath -= SacrificeArtifactManager_OnServerCharacterDeath;
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_ChestDrop;
            // Shrines
            On.RoR2.PortalStatueBehavior.GrantPortalEntry -= PortalStatueBehavior_GrantPortalEntry_Gold;
            On.RoR2.ShrineBloodBehavior.AddShrineStack -= ShrineBloodBehavior_AddShrineStack;
            On.RoR2.CharacterMaster.GiveMoney -= CharacterMaster_GiveMoney;
            On.RoR2.ShrineChanceBehavior.AddShrineStack -= ShrineChanceBehavior_AddShrineStack;
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_ChanceShrine;
            On.RoR2.ShrineCombatBehavior.AddShrineStack -= ShrineCombatBehavior_AddShrineStack;
            On.RoR2.ShrineRestackBehavior.AddShrineStack -= ShrineRestackBehavior_AddShrineStack;
            On.RoR2.BossGroup.DropRewards -= BossGroup_DropRewards;
            On.RoR2.ShrineHealingBehavior.AddShrineStack -= ShrineHealingBehavior_AddShrineStack;
            On.RoR2.ShrineColossusAccessBehavior.OnInteraction -= ShrineColossusAccessBehavior_OnInteraction;
            // Scavengers
            On.EntityStates.ScavBackpack.Opening.OnEnter -= Opening_OnEnter;
            On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop_Scavenger;
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_Scavenger;
            // Radio Scanners
            On.RoR2.SceneDirector.PopulateScene -= SceneDirector_PopulateScene;
            On.RoR2.RadiotowerTerminal.GrantUnlock -= RadiotowerTerminal_GrantUnlock;
            ArchipelagoConsoleCommand.OnArchipelagoHighlightSatelliteCommandCalled -= ArchipelagoConsoleCommand_OnArchipelagoHighlightSatelliteCommandCalled;
            // Newt Altars
            On.RoR2.PortalStatueBehavior.GrantPortalEntry -= PortalStatueBehavior_GrantPortalEntry_Blue;
            
        }

        public ArchipelagoLocationCheckProgressBarUI itemBar = null;
        public ArchipelagoLocationCheckProgressBarUI shrineBar = null;

        // NOTE the counters are not used to store the actual count, they used for detecting when to send locations
        private uint chestitemsPickedUp = 0; // is used to count the number of items
        private uint shrinesUsed = 0; // is used to count the number of items

        public uint itemPickupStep = 3; // is the interval at which archipelago locations are sent from chest-like objects; 1 is every, 2 is every other, etc
        public uint shrineUseStep = 3; // is the interval at which archipelago locations are sent from shrine objects; 1 is every, 2 is every other, etc

        private bool chestblockitem = false; // used to keep track of when the chest's item(s) are blocked as a location check
        private bool sacrificeitem = false; // used to keep track of when an item is being dropped by the sacrifice artifiact
        private bool chanceshrineblockitem = false; // used to keep track of when the blood shrine is attempting to give gold so the gold can be blocked
        private bool chanceshrinebeat = false; // used to keep track of if the chance shrine intended on rewarding a check
        private bool bloodshrineblockgold = false; // used to keep track of when the blood shrine is attempting to give gold so the gold can be blocked
        private int scavbackpackHash = 0; // used to keep track of which chest is the scavenger backpack
        private bool scavbackpackWasLocation = false; // used to track if the scavenger backpack that was opened was used as a location
        private bool scavbackpackblockitem = false; // used to keep track of when the scavenger backpack's items are blocked from a location check
        // private bool blockVoidTriple = false;
        public const int testing = 3;
        private bool highlightOn = false;
        public static SceneDef sceneDef { get; private set; } //used for the currect scene loaded

        private void SceneInfo_Awake(On.RoR2.SceneInfo.orig_Awake orig, SceneInfo self)
        {
            orig(self);
            sceneDef = self.sceneDef;
            GetCurrentSceneIndex();
            Log.LogDebug($"Scene Index is {sceneIndex}");
        }

        public static SceneDef GetLocationScene()
        {
            return sceneDef;
        }
        public void GetCurrentSceneIndex()
        {
            foreach (var scene in LocationNames.cachedLocationsNames)
            {
                if (scene.Value == sceneDef.cachedName)
                {
                    sceneIndex = scene.Key;
                    return;
                }
            }
            sceneIndex = 100;
        }
        public int GetSceneIndex(string sceneName)
        {
            foreach (var scene in LocationNames.cachedLocationsNames)
            {
                if (scene.Value == sceneName)
                {
                    return scene.Key;
                }
            }
            return 0;
        }
        private void updateBar(LocationTypes loctype)
        {
            ArchipelagoLocationCheckProgressBarUI bar = null;
            int amount = 0;
            int step = 1;
            switch (loctype)
            {
                case LocationTypes.chest:
                    bar = itemBar;
                    amount = (int) chestitemsPickedUp;
                    step = (int) itemPickupStep;
                    new SyncLocationCheckProgress(amount % step, step).Send(NetworkDestination.Clients);
                    break;
                case LocationTypes.shrine:
                    bar = shrineBar;
                    amount = (int) shrinesUsed;
                    step = (int) shrineUseStep;
                    new SyncShrineCheckProgress(amount % step, step).Send(NetworkDestination.Clients);
                    break;
            }

            if (null != bar)
            {
                bar.UpdateCheckProgress(amount % step, step);
                // use the default color with checks, use the alt color when out of checks
                bar.ChangeBarColor(0 < checkAvailable(loctype) ? ArchipelagoLocationCheckProgressBarUI.defaultColor : ArchipelagoLocationCheckProgressBarUI.altColor);
            }
        }

        private void sendLocation(int id)
        {
            LocationChecksPacket packet = new LocationChecksPacket();
            packet.Locations = new List<long> { id }.ToArray();
            Log.LogDebug($"planning to send location {id}"); // XXX
            // Changed to Async.. lets see if it breaks something else
            session.Socket.SendPacketAsync(packet);
            
        }

        /// <summary>
        /// Checks the remaing checks of a specific type in the current environment. <br/>
        /// If the type given is LocationTypes.MAX, the total of all locations remaining will be returned.
        /// </summary>
        /// <param name="loctype">The type of location to check.</param>
        /// <returns>Returns the amount of remaining locations.</returns>
        private int checkAvailable(LocationTypes loctype) // TODO make a method to check the nth location
        {
            int index = GetSceneIndex(sceneDef.cachedName);
            if (!currentlocations.TryGetValue(index, out var locationsinenvironment))
            // prevent KeyNotFoundException by using TryGetValue
            {
                // if the locations in the environment are not being tracked, there must be 0 locations
                return 0;
            }

            if (LocationTypes.MAX == loctype)
            {
                return locationsinenvironment.total();
            }
            return locationsinenvironment[loctype];
        }

        /// <summary>
        /// Send the next available location for the current environment of that specified type.
        /// </summary>
        /// <remarks>
        /// NOTE this does not account for pickup steps.
        /// </remarks>
        /// <param name="loctype">The type of location to send.</param>
        /// <returns>
        /// Returns true if a location send attempt was made.
        /// (Sending a location who's item has been collected will still return true.)
        /// </returns>
        private bool sendNextAvailable(LocationTypes loctype) // TODO make a method to send the nth location
        {
            if (LocationTypes.MAX == loctype) throw new ArgumentException("MAX is not a sendable location type.");

            if (!currentlocations.TryGetValue(sceneIndex, out var locationsinenvironment))
            // prevent KeyNotFoundException by using TryGetValue
            {
                // if the locations in the environment that are not being tracked, then there is no check to send
                return false;
            }

            int environment_start_id = sceneIndex * ArchipelagoLocationOffsets.allocation + ArchipelagoLocationOffsets.ror2_locations_start_orderedstage;

            // check if there is a check to be done
            // if there are none, then return false
            if (locationsinenvironment[loctype] == 0) return false;

            int next_index = originallocationstemplate[loctype] - locationsinenvironment[loctype];
            int offset_in_allocation = ArchipelagoLocationOffsets.offset[(int)loctype];
            locationsinenvironment[loctype]--;
            ArchipelagoLocationsInEnvironmentController.count[loctype] = locationsinenvironment[loctype];

            // update UI to the results of sending the location
            ArchipelagoTotalChecksObjectiveController.CurrentChecks++;
            int CurrentChecks = ArchipelagoTotalChecksObjectiveController.CurrentChecks++;
            int TotalChecks = ArchipelagoTotalChecksObjectiveController.TotalChecks;
            new SyncTotalCheckProgress(CurrentChecks, TotalChecks).Send(NetworkDestination.Clients);
            if (0 == ArchipelagoLocationsInEnvironmentController.count.total())
            {
                new AllChecksCompleteInStage().Send(NetworkDestination.Clients);
                ArchipelagoLocationsInEnvironmentController.RemoveObjective();
            }
            else
            {
                new NextStageObjectives().Send(NetworkDestination.Clients);
                ArchipelagoLocationsInEnvironmentController.AddObjective();
                UpdateClientsUI();
            }

            currentlocations[sceneIndex] = locationsinenvironment; // save changes to the count
            
            sendLocation(next_index + offset_in_allocation + environment_start_id);

            return true; // a location must have been sent
            // (don't care if the item for said location has already be collected)
            // (don't care if the location has been sent before, though it shouldn't happen if everything is working)

        }
        private bool UpdateClientsUI()
        {
            if (!currentlocations.TryGetValue(sceneIndex, out var locationsinenvironment))
            // prevent KeyNotFoundException by using TryGetValue
            {
                // if the locations in the environment that are not being tracked, then there is no check to send
                return false;
            }
            ArchipelagoLocationsInEnvironmentController.CurrentScene = locationsinenvironment.scene();
            ArchipelagoLocationsInEnvironmentController.CurrentChests = locationsinenvironment[LocationTypes.chest];
            ArchipelagoLocationsInEnvironmentController.CurrentShrines = locationsinenvironment[LocationTypes.shrine];
            ArchipelagoLocationsInEnvironmentController.CurrentScavangers = locationsinenvironment[LocationTypes.scavenger];
            ArchipelagoLocationsInEnvironmentController.CurrentScanners = locationsinenvironment[LocationTypes.radio_scanner];
            ArchipelagoLocationsInEnvironmentController.CurrentNewts = locationsinenvironment[LocationTypes.newt_altar];
            new SyncCurrentEnvironmentCheckProgress(locationsinenvironment.scene(), locationsinenvironment[LocationTypes.chest], locationsinenvironment[LocationTypes.shrine],
                locationsinenvironment[LocationTypes.scavenger], locationsinenvironment[LocationTypes.radio_scanner], locationsinenvironment[LocationTypes.newt_altar]).Send(NetworkDestination.Clients);
            return true;
        }

        /// <summary>
        /// Resets all overhead variables that should be reinitialized when entering a new environment.
        /// </summary>
        private void SceneCatalog_OnActiveSceneChanged(On.RoR2.SceneCatalog.orig_OnActiveSceneChanged orig, UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
        {
            orig(oldScene, newScene);
            LoadItemPickupHooks();
        }

        public void LoadItemPickupHooks()
        {
            // We want to hook directly to SceneCatalog_OnActiveSceneChanged rather than delegate
            //  to SceneCatalog_OnActiveSceneChanged so that we can take advantage of the changed mostRecentSceneDef.
            CatchUpSceneLocations(sceneDef.cachedName);

            // don't reset the counters on moving between stages
            // this could make it absurdly hard to complete checks on very high step sizes
            //chestitemsPickedUp = 0;
            //shrinesUsed = 0;

            // reset the values in case the shrine was somehow busy when the stage changed
            chestblockitem = false;
            sacrificeitem = false;
            chanceshrineblockitem = false;
            chanceshrinebeat = false;
            bloodshrineblockgold = false;
            scavbackpackHash = 0;
            scavbackpackWasLocation = false;
            scavbackpackblockitem = false;

            // update the bars for the new scene
            updateBar(LocationTypes.chest);
            updateBar(LocationTypes.shrine);
            if (0 < checkAvailable(LocationTypes.chest))
            {
                On.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop_Chest;
                On.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath += SacrificeArtifactManager_OnServerCharacterDeath;
                On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_ChestDrop;
            }
            // update the UI to match the new environment
            for (int type = 0; type < (int)LocationTypes.MAX; type++)
            {
                ArchipelagoLocationsInEnvironmentController.count[type] = checkAvailable((LocationTypes)type);
            }
            UpdateClientsUI();
            if (0 == ArchipelagoLocationsInEnvironmentController.count.total())
            {
                new AllChecksCompleteInStage().Send(NetworkDestination.Clients);
                ArchipelagoLocationsInEnvironmentController.RemoveObjective();
            }
            else
            {
                new NextStageObjectives().Send(NetworkDestination.Clients);
                ArchipelagoLocationsInEnvironmentController.AddObjective();
            }

            // TODO maybe the make sure the ArchipelagoTotalChecksObjectiveController.CurrentChecks gets synced here (since sending a location increments it and could possibly desync it?)

        }
        private void SceneExitController_OnDestroy(On.RoR2.SceneExitController.orig_OnDestroy orig, SceneExitController self)
        {
            On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop_Chest;
            On.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath -= SacrificeArtifactManager_OnServerCharacterDeath;
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_ChestDrop;
            orig(self);
        }
        private void SceneCollection_AddToWeightedSelection(On.RoR2.SceneCollection.orig_AddToWeightedSelection orig, SceneCollection self, WeightedSelection<SceneDef> dest, Func<SceneDef, bool> canAdd)
        {
            // In explore mode we will give help the player a little by adjusting the RNG to favor locations where checks need to still be performed.
            // This should help the player not get stuck in an RNG hell where they simply cannot roll into the stages they need to go to to complte things.

            orig(self, dest, canAdd);
            if (null == dest) return; // prevent NRE
            for (int i=0; i < dest.Count; i++)
            {
                // add 5 weight to per location left in an environment
                string stageName = dest.choices[i].value.cachedName;
                int environment_index = GetSceneIndex(stageName);
                CatchUpSceneLocations(stageName);
                Log.LogDebug($"Environment {environment_index} with weight {dest.choices[i].weight} has stage name {stageName}.");
                if (currentlocations.TryGetValue(environment_index, out var locations))
                { 
                    int addweight = locations.total() * 5;
                    Log.LogDebug($"Environment {environment_index} with weight {dest.choices[i].weight} has {addweight / 5} locations, adjusting weight.");
                    dest.ModifyChoiceWeight(i, dest.choices[i].weight + addweight);
                    Log.LogDebug($"Adjusted weight to {dest.choices[i].weight}.");
                    if (dest.choices[i].weight <= 0)
                    {
                        Log.LogDebug($"Environment {environment_index} weight adjusted to 1 to prevent zero or negative weight.");
                        dest.ModifyChoiceWeight(i, 1);
                    }
                }
                else Log.LogDebug($"Environment {environment_index} with weight {dest.choices[i].weight} does not have locations.");
            }

        }
    }

    // TODO it may be interesting if Baazar seers could allow the player to travel to environments earlier in the loop (ie to give more control over where the player goes)
}
