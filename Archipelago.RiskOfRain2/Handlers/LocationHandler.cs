using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    class LocationHandler : IHandler
    {
        // NOTE every mention of a "location" refers to the archipelago location checks
        // NOTE every mention of a "environment" refers to the risk of rain 2 scenes that are loaded and played


        // setup all scene indexes as megic numbers
        // scenes from https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
        // main scenes
        public const int ancientloft = 3;       // Aphelian Sanctuary
        public const int blackbeach = 7;        // Distant Roost
        public const int blackbeach2 = 8;       // Distant Roost TODO environment varients should probably be treated the same
        public const int dampcavesimple = 10;   // Abyssal Depths
        public const int foggyswamp = 12;       // Wetland Aspect
        public const int frozenwall = 13;       // Rallypoint Delta
        public const int golemplains = 15;      // Titanic Plains
        public const int golemplains2 = 16;     // Titanic Plains TODO environment varients should probably be treated the same
        public const int goolake = 17;          // Abandoned Aqueduct
        public const int rootjungle = 35;       // Sundered Grove
        public const int shipgraveyard = 37;    // Siren's Call
        public const int skymeadow = 38;        // Sky Meadow
        public const int snowyforest = 39;      // Siphoned Forest
        public const int sulfurpools = 41;      // Sulfur Pools
        public const int wispgraveyard = 47;    // Scorched Acres


        public enum LocationTypes
        {
            chest,
            shrine,
            scavenger,
            radio_scanner,
            newt_altar,
            MAX

        }

        // XXX this should be in an array
        public struct LocationInformationTemplate
        {
            public int chest_count { get; set; }
            public int shrine_count { get; set; }
            public int scavenger_count { get; set; }
            public int radio_scanner_count { get; set; }
            public int newt_alter_count { get; set; }
        }

        public static LocationInformationTemplate buildTemplateFromSlotData(Dictionary<string, object> SlotData)
        {
            LocationInformationTemplate locationtemplate = new LocationInformationTemplate();
            if (SlotData is not null)
            {
                if (SlotData.TryGetValue("chests_per_stage", out var chests_per_stage)) locationtemplate.chest_count = Convert.ToInt32(chests_per_stage);
                if (SlotData.TryGetValue("shrines_per_stage", out var shrines_per_stage)) locationtemplate.shrine_count = Convert.ToInt32(shrines_per_stage);
                if (SlotData.TryGetValue("scavengers_per_stage", out var scavengers_per_stage)) locationtemplate.scavenger_count = Convert.ToInt32(scavengers_per_stage);
                if (SlotData.TryGetValue("scanner_per_stage", out var scanner_per_stage)) locationtemplate.radio_scanner_count = Convert.ToInt32(scanner_per_stage);
                if (SlotData.TryGetValue("altars_per_stage", out var altars_per_stage)) locationtemplate.newt_alter_count = Convert.ToInt32(altars_per_stage);
            }
            return locationtemplate;
        }

        /// <summary>
        /// These values are sourced from the RoR2 Archipelago world code.
        /// These are used to determine the id values of locations.
        /// </summary>
        private readonly struct ArchipelagoLocationOffsets
        {
            // these values come from worlds/ror2/Locations.py in Archipelago
            public const int ror2_locations_start_orderedstage = 38000 + 250;
            // XXX this should be in an array
            public const int offset_ChestsPerEnvironment = 0;
            public const int offset_ShrinesPerEnvironment = 0 + 20;
            public const int offset_ScavengersPerEnvironment = 0 + 20 + 20;
            public const int offset_ScannersPerEnvironment = 0 + 20 + 20 + 1;
            public const int offset_AltarsPerEnvironment = 0 + 20 + 20 + 1 + 1;
            public const int allocation = 0 + 20 + 20 + 1 + 1 + 2;
        }

        private ArchipelagoSession session;
        private LocationInformationTemplate originallocationstemplate;
        private Dictionary<int, LocationInformationTemplate> currentlocations;

        public LocationHandler(ArchipelagoSession session, LocationInformationTemplate locationstemplate)
        {
            Log.LogDebug($"Location handler constructor.");
            this.session = session;
            originallocationstemplate = locationstemplate;
            currentlocations = new Dictionary<int, LocationInformationTemplate>();


            itemShouldNotDrop = new Queue<bool>();

            InitialSetupLocationDict(locationstemplate);

            CatchUpLocationDict();
        }

        /// <summary>
        /// Calling adds the location template to each environment so they can be individually tracked later.
        /// </summary>
        /// <param name="locationstemplate">Template to assign to all relevant environments.</param>
        // TODO this should probably become generic so that environment sets can be passed in (e.g. normal environments, simulacrum environments, etc)
        private void InitialSetupLocationDict(LocationInformationTemplate locationstemplate)
        {
            currentlocations.Add(ancientloft,       locationstemplate); // Aphelian Sanctuary
            currentlocations.Add(blackbeach,        locationstemplate); // Distant Roost
            currentlocations.Add(blackbeach2,       locationstemplate); // Distant Roost
            currentlocations.Add(dampcavesimple,    locationstemplate); // Abyssal Depths
            currentlocations.Add(foggyswamp,        locationstemplate); // Wetland Aspect
            currentlocations.Add(frozenwall,        locationstemplate); // Rallypoint Delta
            currentlocations.Add(golemplains,       locationstemplate); // Titanic Plains
            currentlocations.Add(golemplains2,      locationstemplate); // Titanic Plains
            currentlocations.Add(goolake,           locationstemplate); // Abandoned Aqueduct
            currentlocations.Add(rootjungle,        locationstemplate); // Sundered Grove
            currentlocations.Add(shipgraveyard,     locationstemplate); // Siren's Call
            currentlocations.Add(skymeadow,         locationstemplate); // Sky Meadow
            currentlocations.Add(snowyforest,       locationstemplate); // Siphoned Forest
            currentlocations.Add(sulfurpools,       locationstemplate); // Sulfur Pools
            currentlocations.Add(wispgraveyard,     locationstemplate); // Scorched Acres
            // TODO separate out the DLC locations
        }

        /// <summary>
        /// This is used to have the location handler catch up to the archipelago session.
        /// This is because the player may have completed checks, died, and restarted the session and we do not need to have the player repeat checks.
        /// </summary>
        private void CatchUpLocationDict()
        {
            Log.LogDebug("CatchUpLocationDict"); // XXX
            ReadOnlyCollection<long> completedchecks = session.Locations.AllLocationsChecked;

            // TODO time complexity probably doesn't matter here, but there probably is a more efficient way to do this
            // a probably better way to do it would be iterate over all completed chests, skip numbers which don't relate to ror2, and change the values per number
            // instead of the converse of checking the numbers for every possible check

            // a copy is needed because the one being enumerated over cannot be changed
            Dictionary<int, LocationInformationTemplate> locationscopy = new Dictionary<int, LocationInformationTemplate>(currentlocations);

            foreach (KeyValuePair<int, LocationInformationTemplate> kvp in locationscopy)
            {
                int index = kvp.Key;
                LocationInformationTemplate location = kvp.Value;
                int environment_start_id = index*ArchipelagoLocationOffsets.allocation + ArchipelagoLocationOffsets.ror2_locations_start_orderedstage;
                Log.LogDebug($"index {index}"); // XXX
                Log.LogDebug($"environment_start_id {environment_start_id}"); // XXX

                // XXX this should go into a function probably since it is copy and pasted
                // XXX this code is begging for the template to be an array indexed off of the type enum

                // catch up chests
                for (int n=0; n < originallocationstemplate.chest_count; n++)
                {
                    // check each location if it has been seen
                    if (completedchecks.Contains(n + ArchipelagoLocationOffsets.offset_ChestsPerEnvironment + environment_start_id))
                    {
                        location.chest_count--; // a location completed has been found for this environment
                    }
                    // if we see a location missing, imply the ones that succeed it are also missing
                    else break;
                }
                Log.LogDebug($"caught up to chest {location.chest_count}"); // XXX

                // catch up shrines
                for (int n=0; n < originallocationstemplate.shrine_count; n++)
                {
                    // check each location if it has been seen
                    if (completedchecks.Contains(n + ArchipelagoLocationOffsets.offset_ShrinesPerEnvironment + environment_start_id))
                    {
                        location.shrine_count--; // a location completed has been found for this environment
                    }
                    // if we see a location missing, imply the ones that succeed it are also missing
                    else break;
                }
                Log.LogDebug($"caught up to shrine {location.shrine_count}"); // XXX

                // catch up scavengers
                for (int n=0; n < originallocationstemplate.scavenger_count; n++)
                {
                    // check each location if it has been seen
                    if (completedchecks.Contains(n + ArchipelagoLocationOffsets.offset_ScannersPerEnvironment + environment_start_id))
                    {
                        location.scavenger_count--; // a location completed has been found for this environment
                    }
                    // if we see a location missing, imply the ones that succeed it are also missing
                    else break;
                }
                Log.LogDebug($"caught up to scavenger {location.scavenger_count}"); // XXX

                // catch up scanner
                for (int n=0; n < originallocationstemplate.scavenger_count; n++)
                {
                    // check each location if it has been seen
                    if (completedchecks.Contains(n + ArchipelagoLocationOffsets.offset_ScannersPerEnvironment + environment_start_id))
                    {
                        location.radio_scanner_count--; // a location completed has been found for this environment
                    }
                    // if we see a location missing, imply the ones that succeed it are also missing
                    else break;
                }
                Log.LogDebug($"caught up to scanner {location.radio_scanner_count}"); // XXX

                // catch up altar
                for (int n=0; n < originallocationstemplate.scavenger_count; n++)
                {
                    // check each location if it has been seen
                    if (completedchecks.Contains(n + ArchipelagoLocationOffsets.offset_AltarsPerEnvironment + environment_start_id))
                    {
                        location.radio_scanner_count--; // a location completed has been found for this environment
                    }
                    // if we see a location missing, imply the ones that succeed it are also missing
                    else break;
                }
                Log.LogDebug($"caught up to altar {location.newt_alter_count}"); // XXX

                currentlocations[(int)index] = location;
            }
        }

        public void Hook()
        {
            // Reset
            On.RoR2.Stage.BeginAdvanceStage += Stage_BeginAdvanceStage;
            // Chests
            On.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop_Chest;
            On.RoR2.PickupDropletController.CreatePickupDroplet_PickupIndex_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet;
            // Shrines
            On.RoR2.PortalStatueBehavior.GrantPortalEntry += PortalStatueBehavior_GrantPortalEntry_Gold;
            On.RoR2.ShrineBloodBehavior.AddShrineStack += ShrineBloodBehavior_AddShrineStack;
            On.RoR2.ShrineCombatBehavior.AddShrineStack += ShrineCombatBehavior_AddShrineStack;
            On.RoR2.ShrineRestackBehavior.AddShrineStack += ShrineRestackBehavior_AddShrineStack;
            On.RoR2.BossGroup.DropRewards += BossGroup_DropRewards;
            On.RoR2.ShrineHealingBehavior.AddShrineStack += ShrineHealingBehavior_AddShrineStack;
            // Scavengers
            // Radio Scanners
            On.RoR2.SceneDirector.PopulateScene += SceneDirector_PopulateScene;
            On.RoR2.RadiotowerTerminal.GrantUnlock += RadiotowerTerminal_GrantUnlock;
            // Newt Altars
            On.RoR2.PortalStatueBehavior.GrantPortalEntry += PortalStatueBehavior_GrantPortalEntry_Blue;
        }

        public void UnHook()
        {
            // Reset
            On.RoR2.Stage.BeginAdvanceStage -= Stage_BeginAdvanceStage;
            // Chests
            On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop_Chest;
            On.RoR2.PickupDropletController.CreatePickupDroplet_PickupIndex_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet;
            // Shrines
            On.RoR2.PortalStatueBehavior.GrantPortalEntry -= PortalStatueBehavior_GrantPortalEntry_Gold;
            On.RoR2.ShrineBloodBehavior.AddShrineStack -= ShrineBloodBehavior_AddShrineStack;
            On.RoR2.ShrineChanceBehavior.AddShrineStack -= ShrineChanceBehavior_AddShrineStack;
            On.RoR2.ShrineCombatBehavior.AddShrineStack -= ShrineCombatBehavior_AddShrineStack;
            On.RoR2.ShrineRestackBehavior.AddShrineStack -= ShrineRestackBehavior_AddShrineStack;
            On.RoR2.BossGroup.DropRewards -= BossGroup_DropRewards;
            On.RoR2.ShrineHealingBehavior.AddShrineStack -= ShrineHealingBehavior_AddShrineStack;
            // Scavengers
            // Radio Scanners
            On.RoR2.SceneDirector.PopulateScene -= SceneDirector_PopulateScene;
            On.RoR2.RadiotowerTerminal.GrantUnlock -= RadiotowerTerminal_GrantUnlock;
            // Newt Altars
            On.RoR2.PortalStatueBehavior.GrantPortalEntry -= PortalStatueBehavior_GrantPortalEntry_Blue;
        }

        private uint chestitemsPickedUp = 0; // is used to count the number of items
        private uint shrinesUsed = 0; // is used to count the number of items
        // XXX get this in from the YAML
        private uint itemPickupStep = 2; // is the interval at which archipelago locations are sent from chest-like objects; 1 is every, 2 is every other, etc
        private uint shrineUseStep = 2; // is the interval at which archipelago locations are sent from shrine objects; 1 is every, 2 is every other, etc
        private Queue<bool> itemShouldNotDrop; // used to figure out which items should complete locations

        private void sendLocation(int id)
        {
            LocationChecksPacket packet = new LocationChecksPacket();
            packet.Locations = new List<long> { id }.ToArray();
            Log.LogDebug($"planning to send location {id}"); // XXX
            // why synchronous? that's how Ijwu had done it before, unsure of the specific reasoning:
            // https://github.com/Ijwu/Archipelago.RiskOfRain2/blob/4318f37e7aa3fea258830de0d08a41014b19228b/Archipelago.RiskOfRain2/ArchipelagoItemLogicController.cs#L311
            session.Socket.SendPacket(packet);
        }

        /// <summary>
        /// Checks the remaing checks of a specific type in the current environment.
        /// </summary>
        /// <param name="loctype">The type of location to check.</param>
        /// <returns>Returns the amount of remaining locations.</returns>
        private int checkAvailable(LocationTypes loctype) // TODO make a method to check the nth location

        {
            int currentenvironment = (int)SceneCatalog.mostRecentSceneDef.sceneDefIndex;
            if (!currentlocations.TryGetValue((int)currentenvironment, out var locationsinenvironment))
            // prevent KeyNotFoundException by using TryGetValue
            {
                // if the locations in the environment are not being tracked, there must be 0 locations
                return 0;
            }

            switch (loctype)
            {
                // XXX this code is begging for the template to be an array indexed off of the type enum
                case LocationTypes.chest:
                    return locationsinenvironment.chest_count;
                case LocationTypes.shrine:
                    return locationsinenvironment.shrine_count;
                case LocationTypes.scavenger:
                    return locationsinenvironment.scavenger_count;
                case LocationTypes.radio_scanner:
                    return locationsinenvironment.radio_scanner_count;
                case LocationTypes.newt_altar:
                    return locationsinenvironment.newt_alter_count;
                default:
                    return 0; // TODO maybe thrown an exception?
            }
        }

        /// <summary>
        /// Send the next available location for the current environment of that specified type.
        /// NOTE this does not account for pickup steps.
        /// </summary>
        /// <param name="loctype">The type of location to send.</param>
        /// <returns>
        /// Returns true if a location send attempt was made.
        /// (Sending a location who's item has been collected will still return true.)
        /// </returns>
        private bool sendNextAvailable(LocationTypes loctype) // TODO make a method to send the nth location
        {
            int currentenvironment = (int)SceneCatalog.mostRecentSceneDef.sceneDefIndex;
            if (!currentlocations.TryGetValue((int)currentenvironment, out var locationsinenvironment))
            // prevent KeyNotFoundException by using TryGetValue
            {
                // if the locations in the environment that are not being tracked, then there is no check to send
                return false;
            }

            int environment_start_id = currentenvironment * ArchipelagoLocationOffsets.allocation + ArchipelagoLocationOffsets.ror2_locations_start_orderedstage;

            // check if there is a check to be done
            // if there are none, then return false
            switch (loctype)
            {
                // XXX this code is begging for the template to be an array indexed off of the type enum
                case LocationTypes.chest:
                    if (locationsinenvironment.chest_count == 0) return false;
                    break;
                case LocationTypes.shrine:
                    if (locationsinenvironment.shrine_count == 0) return false;
                    break;
                case LocationTypes.scavenger:
                    if (locationsinenvironment.scavenger_count == 0) return false;
                    break;
                case LocationTypes.radio_scanner:
                    if (locationsinenvironment.radio_scanner_count == 0) return false;
                    break;
                case LocationTypes.newt_altar:
                    if (locationsinenvironment.newt_alter_count == 0) return false;
                    break;
                default:
                    return false; // TODO maybe thrown an exception?
            }

            int next_index;
            int offset_in_allocation;
            switch (loctype)
            {
                // XXX this code is begging for the template to be an array indexed off of the type enum
                case LocationTypes.chest:
                    next_index = originallocationstemplate.chest_count - locationsinenvironment.chest_count;
                    offset_in_allocation = ArchipelagoLocationOffsets.offset_ChestsPerEnvironment;
                    locationsinenvironment.chest_count--;
                    break;
                case LocationTypes.shrine:
                    next_index = originallocationstemplate.shrine_count - locationsinenvironment.shrine_count;
                    offset_in_allocation = ArchipelagoLocationOffsets.offset_ShrinesPerEnvironment;
                    locationsinenvironment.shrine_count--;
                    break;
                case LocationTypes.scavenger:
                    next_index = originallocationstemplate.scavenger_count - locationsinenvironment.scavenger_count;
                    offset_in_allocation = ArchipelagoLocationOffsets.offset_ScavengersPerEnvironment;
                    locationsinenvironment.scavenger_count--;
                    break;
                case LocationTypes.radio_scanner:
                    next_index = originallocationstemplate.radio_scanner_count - locationsinenvironment.radio_scanner_count;
                    offset_in_allocation = ArchipelagoLocationOffsets.offset_ScannersPerEnvironment;
                    locationsinenvironment.radio_scanner_count--;
                    break;
                case LocationTypes.newt_altar:
                    next_index = originallocationstemplate.newt_alter_count - locationsinenvironment.newt_alter_count;
                    offset_in_allocation = ArchipelagoLocationOffsets.offset_AltarsPerEnvironment;
                    locationsinenvironment.newt_alter_count--;
                    break;
                default:
                    return false; // TODO maybe thrown an exception?
            }

            currentlocations[(int)currentenvironment] = locationsinenvironment; // save changes to the count

            sendLocation(next_index + offset_in_allocation + environment_start_id);

            return true; // a location must have been sent
            // (don't care if the item for said location has already be collected)
            // (don't care about duplicates if it happens, though it shouldn't happen if everything is working)

        }

        /// <summary>
        /// Resets all overhead variables that should be reinitialized when entering a new environment.
        /// </summary>
        private void Stage_BeginAdvanceStage(On.RoR2.Stage.orig_BeginAdvanceStage orig, Stage self, SceneDef destinationStage)
        {
            Log.LogDebug("Stage_BeginAdvanceStage"); // XXX
            orig(self, destinationStage);

            // don't reset the counters on moving between stages
            // this could be it absurdly hard to complete checks on very high step sizes
            //chestitemsPickedUp = 0;
            //shrinesUsed = 0;

            itemShouldNotDrop.Clear();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Chest like objects

        // To not have to write IL code, some weird hooks will be used.
        // The idea is to count the number of items that will be spawned and then intercept them as they are spawning
        //  to prevent only consume items we want to use as locations.

        private void ChestBehavior_ItemDrop_Chest(On.RoR2.ChestBehavior.orig_ItemDrop orig, RoR2.ChestBehavior self)
        {
            if(NetworkServer.active && self.dropPickup != PickupIndex.none && self.dropCount == 1)
            // chests only ever drop one item, so if the drop count is one, we know it was truely a chest (and not a scavenger bag)
            {
                if (0 < checkAvailable(LocationTypes.chest))
                {
                    chestitemsPickedUp++;
                    if (true) // TODO maybe items should also go to the player?
                    {
                        // used to enforce don't drop the item within PickupDropletController_CreatePickupDroplet
                        itemShouldNotDrop.Enqueue(0 == chestitemsPickedUp % itemPickupStep);
                    }

                    if (0 == chestitemsPickedUp % itemPickupStep)
                    {
                        sendNextAvailable(LocationTypes.chest);
                    }
                }
            }

            orig(self); // the original will end up calling PickupDropletController_CreatePickupDroplet as well as other things
        }

        private void PickupDropletController_CreatePickupDroplet(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_PickupIndex_Vector3_Vector3 orig, RoR2.PickupIndex pickupIndex, UnityEngine.Vector3 position, UnityEngine.Vector3 velocity)
        {
            // XXX is from the item blacklist Ijwu created is important to impose here?

            // check if the item being dropped is being asked to not drop
            if (itemShouldNotDrop.Count > 0 && itemShouldNotDrop.Dequeue())
            {
                Log.LogDebug($"item {pickupIndex} was used to satisfy a location and thus is consumed");
                return;
            }
            orig(pickupIndex, position, velocity);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Shrine like objects

        // All shrines behave differently and there is no inheritance to a common shrine object
        // Therefore all shrine types will have to be handled differently.

        /// <summary>
        /// Call on beating a shrine. This accounts for the step in shrine uses and submits locations.
        /// </summary>
        /// <returns>Returns true if a location was submitted.</returns>
        private bool shrineBeat()
        {
            if (0 < checkAvailable(LocationTypes.shrine))
            {
                shrinesUsed++;
                if (0 == shrinesUsed % shrineUseStep) return sendNextAvailable(LocationTypes.shrine);
            }
            return false;
        }

        /// <summary>
        /// Beats the gold portal shrine when attempting to grant the portal entry.
        /// </summary>
        private void PortalStatueBehavior_GrantPortalEntry_Gold(On.RoR2.PortalStatueBehavior.orig_GrantPortalEntry orig, PortalStatueBehavior self)
        {
            orig(self);
            // using the gold shrine beats it; it already costs enough to use the shrine, so taking the portal away is just crule
            if (self.portalType == PortalStatueBehavior.PortalType.Goldshores) shrineBeat();
        }


        /// <summary>
        /// Using the blood shrine beats the shrine.
        /// </summary>
        private void ShrineBloodBehavior_AddShrineStack(On.RoR2.ShrineBloodBehavior.orig_AddShrineStack orig, ShrineBloodBehavior self, Interactor interactor)
        {
            orig(self, interactor); // depending on money would be given will determine success of the shrine
            shrineBeat(); // using the blood shrine beats it
        }

        /// <summary>
        /// Beat the chance shrine when a successful purchase happens.
        /// </summary>
        private void ShrineChanceBehavior_AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
        {
            int prev = self.successfulPurchaseCount;
            // XXX make it so shrine does not drop item and a check
            orig(self, activator);
            // chance shrine is only beat if the success count increases
            if (prev < self.successfulPurchaseCount) shrineBeat();
        }

        /// <summary>
        /// Using the shcange shrine beats it.
        /// </summary>
        private void ShrineCombatBehavior_AddShrineStack(On.RoR2.ShrineCombatBehavior.orig_AddShrineStack orig, ShrineCombatBehavior self, Interactor interactor)
        {
            orig(self, interactor);
            // TODO maybe combat shrine shouldn't be an instant reward
            shrineBeat(); // using the combat shrine beats it
        }

        /// <summary>
        /// Using the order shrine beats it
        /// </summary>
        private void ShrineRestackBehavior_AddShrineStack(On.RoR2.ShrineRestackBehavior.orig_AddShrineStack orig, ShrineRestackBehavior self, Interactor interactor)
        {
            orig(self, interactor);
            shrineBeat(); // using the order shrine beats it
        }

        /// <summary>
        /// When the boss group is attempting to drop bonus rewards, the mountain shrines which granted the bonus are beat.
        /// </summary>
        private void BossGroup_DropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
        {
            orig(self);
            for (int n = 0; n < self.bonusRewardCount; n++)
            {
                Log.LogDebug("bonusRewardCount means a mountain shrine was beat");
                // the only way to raise the bonusRewardCount of a boss is via a mountain shrine

                shrineBeat(); // beat the mountain shrine per mountain activated when the teleporter finishes
            }
        }

        /// <summary>
        /// Purchasing the each of the last two upgrades of the woods shrine beats the shrine.
        /// </summary>
        private void ShrineHealingBehavior_AddShrineStack(On.RoR2.ShrineHealingBehavior.orig_AddShrineStack orig, ShrineHealingBehavior self, Interactor activator)
        {
            orig(self, activator);
            // the last two purchases of woods shine are checks
            if (self.purchaseCount > self.maxPurchaseCount - 2) shrineBeat();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Scavenger

        // Scavengers will be counted by the number of bags opened.

        // XXX implement scavbackpak uses ChestBehavior_ItemDrop

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Radio scanner

        // Radio scanners will need to be forcefully spawned even if the player has purchased them
        //  otherwise the check would be impossible to complete.

        private void SceneDirector_PopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            orig(self); // let the director do it's own thing first as to not get in the way

            if (0 < checkAvailable(LocationTypes.radio_scanner))
            // we always want to always spawn a radio scanner if it is a location
            {
                Log.LogDebug("Environment has radio_scanner locations, spawning an iscRadarTower.");

                // the format for spawning is stolen directly from how rusty/lock boxes are spawned
                Xoroshiro128Plus xoroshiro128PlusRadioScanner = new Xoroshiro128Plus(self.rng.nextUlong);
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscRadarTower"), new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random
                }, xoroshiro128PlusRadioScanner));
            }
        }

        private void RadiotowerTerminal_GrantUnlock(On.RoR2.RadiotowerTerminal.orig_GrantUnlock orig, RadiotowerTerminal self, Interactor interactor)
        {
            Log.LogDebug("RadiotowerTerminal_GrantUnlock"); // XXX

            if (0 == checkAvailable(LocationTypes.radio_scanner))
            {
                // there are no checks, treat the scanner as if it were a vanilla scanner
                orig(self, interactor);
                return;
            }

            sendNextAvailable(LocationTypes.radio_scanner);

            // still play the effect for the scanner and lock it from being used again
            EffectManager.SpawnEffect(self.unlockEffect, new EffectData
            {
                origin = self.transform.position
            }, transmit: true);
            self.SetHasBeenPurchased(newHasBeenPurchased: true);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Newt Altars

        private void PortalStatueBehavior_GrantPortalEntry_Blue(On.RoR2.PortalStatueBehavior.orig_GrantPortalEntry orig, PortalStatueBehavior self)
        {
            Log.LogDebug("PortalStatueBehavior_GrantPortalEntry"); // XXX

            if (self.portalType != PortalStatueBehavior.PortalType.Shop)
            {
                orig(self);
                return;
            } // the below code is only applied to blue portal, ie an altar was used


            Log.LogDebug("altar used ie blue portal"); // XXX
            if (0 == checkAvailable(LocationTypes.newt_altar))
            {
                // there are no checks, treat the altar as if it were a vanilla altar
                orig(self);
                return;
            }

            sendNextAvailable(LocationTypes.newt_altar);

            // don't block the other newts, more than one newt in a stage is rare and if also rewards knowing where newts can spawn when you can find and get to two

            // don't run the original as we do not want to spawn the portal
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
