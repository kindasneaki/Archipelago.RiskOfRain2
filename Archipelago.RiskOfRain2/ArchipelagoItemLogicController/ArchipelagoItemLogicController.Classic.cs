using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using System.Collections.ObjectModel;

namespace Archipelago.RiskOfRain2
{
    public partial class ArchipelagoItemLogicController
    {
        public int PickedUpItemCount { get; set; }
        public long ItemStartId { get; private set; }

        public long[] ChecksTogether { get; set; }
        public long[] MissingChecks { get; set; }

        public delegate void ItemDropProcessedHandler(int pickedUpCount);
        public event ItemDropProcessedHandler OnItemDropProcessed;

        private bool finishedAllChecks = false;
        private UniquePickup[] skippedItems;

        /**
         * Builds the list of pickups that should never be treated as a classic-mode location check.
         */
        private void InitializeClassic()
        {
            skippedItems = [
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixBlue.equipmentIndex)),
                //new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixEcho.equipmentIndex)), // Causes NRE... Not sure why.
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixHaunted.equipmentIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixLunar.equipmentIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixPoison.equipmentIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixRed.equipmentIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixWhite.equipmentIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.MiscPickups.LunarCoin.miscPickupIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.ArtifactKey.itemIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Bomb.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Command.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.EliteOnly.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Enigma.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.FriendlyFire.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Glass.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.MixEnemy.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.MonsterTeamGainsItems.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.RandomSurvivorOnRespawn.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Sacrifice.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.ShadowClone.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.SingleMonsterType.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Swarms.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.TeamDeath.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.WeakAssKnees.artifactIndex)),
                new UniquePickup(PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.WispOnDeath.artifactIndex)),
            ];
            Log.LogDebug("Ok, finished browsing catalog.");
        }

        private void DisposeClassicHooks()
        {
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_CreatePickupInfo;
            On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop;
        }

        private void HandleClassicConnected(ConnectedPacket connectedPacket)
        {
            // hook the classic location handler if not using EnvironmentsAsItems
            bool classic;
            if (connectedPacket.SlotData.TryGetValue("goal", out var classicmodeobject))
            {
                classic = !Convert.ToBoolean(classicmodeobject);
            }
            else classic = true;

            Log.LogDebug($"Detected classic_mode from ArchipelagoItemLogicController? {classic}");

            // TODO maybe this should be moved into a hook method with the other hooks from the constructor
            if (classic)
            {

                On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_CreatePickupInfo;
                On.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop;

                session.Locations.CheckedLocationsUpdated += Check_Locations;
            }
            else
            {
                On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_CreatePickupInfo;
                On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop;

                session.Locations.CheckedLocationsUpdated -= Check_Locations;
            }


            // Add 1 because the user's YAML will contain a value equal to "number of pickups before sent location"
            ItemPickupStep = Convert.ToInt32(connectedPacket.SlotData["itemPickupStep"]) + 1;
            // TODO ItemPickupStep should be set by ArchipelagoClient.cs instead of here (for consistency)
            TotalChecks = connectedPacket.LocationsChecked.Count() + connectedPacket.MissingChecks.Count();
            ChecksTogether = connectedPacket.LocationsChecked.Concat(connectedPacket.MissingChecks).ToArray();
            ChecksTogether = ChecksTogether.OrderBy(n => n).ToArray();
            MissingChecks = connectedPacket.MissingChecks;
            Log.LogDebug($"Missing Checks {connectedPacket.MissingChecks.Count()} totalChecks {TotalChecks} Locations Checked {connectedPacket.LocationsChecked.Count()}");

            // in the case the id is incorrectly set, attempt to set it again
            if (ItemStartId == -1)
            {
                ItemStartId = session.Locations.GetLocationIdFromName("Risk of Rain 2", "ItemPickup1");
                // in case that fails, just manually set it to a default value
                if (ItemStartId == -1) ItemStartId = 38000;
                // NOTE: that this solution will sometimes result in the id just being blatently wrong the first time someone attempts to join a seed.
                // A more rubust way of checking the first id could be done but is not worth the effort.
                // The player can just restart the lobby and the datapackage should be fixed.

                // TODO maybe go back and write a more rubust way to make sure the CurrentChecks make sense when the DataPackage Packet is recieved
            }

            if (connectedPacket.MissingChecks.Count() == 0)
            {
                CurrentChecks = TotalChecks;
                finishedAllChecks = true;
            }
            // resume pickups with the first missing item
            else if (classic)
            {
                var missingIndex = Array.IndexOf(ChecksTogether, connectedPacket.MissingChecks[0]);
                Log.LogInfo($"Missing index is {missingIndex} first missing id is {connectedPacket.MissingChecks[0]}");
                ItemStartId = ChecksTogether[0];
                Log.LogInfo($"ItemStartId {ItemStartId}");
                CurrentChecks = missingIndex;
            } else
            {
                CurrentChecks = ChecksTogether.Length - connectedPacket.MissingChecks.Count();
            }

            ArchipelagoTotalChecksObjectiveController.CurrentChecks = CurrentChecks;
            ArchipelagoTotalChecksObjectiveController.TotalChecks = TotalChecks;

            new SyncTotalCheckProgress(CurrentChecks, TotalChecks).Send(NetworkDestination.Clients);
            // Add up pickedUpItemCount so that resuming a game is possible. The intended behavior is that you immediately receive
            // all of the items you are granted. This is for restarting (in case you lose a run but are not in commencement). 
            PickedUpItemCount = CurrentChecks * ItemPickupStep;
        }

        private void Check_Locations(ReadOnlyCollection<long> item)
        {
            long[] missing = new long[item.Count];
            item.CopyTo(missing, 0);
            if (MissingChecks != null)
            {
                for(int i = 0; i < missing.Length; i++)
                {
                    var missingList = new List<long>(MissingChecks);
                    var missingIndex = Array.IndexOf(MissingChecks, missing[i]);
                    missingList.RemoveAt(missingIndex);
                    MissingChecks = missingList.ToArray();
                }
                Update_MissingChecks();
            }

        }
        // TODO This does not work on your own items being collected
        private void Update_MissingChecks()
        {
            if(MissingChecks.Count() > 0 && ChecksTogether != null)
            {
                var missingIndex = Array.IndexOf(ChecksTogether, MissingChecks[0]);
                Log.LogInfo($"Last item collected is {missingIndex}/{TotalChecks} next missing id is {MissingChecks[0]}");
                CurrentChecks = missingIndex;
                PickedUpItemCount = missingIndex * ItemPickupStep;
                ArchipelagoTotalChecksObjectiveController.CurrentChecks = CurrentChecks;
            }
            
        }

        private void ChestBehavior_ItemDrop(On.RoR2.ChestBehavior.orig_ItemDrop orig, ChestBehavior self)
        {
            var spawnItem = finishedAllChecks || HandleItemDrop();

            if (OnItemDropProcessed != null)
            {
                OnItemDropProcessed(PickedUpItemCount);
            }

            if (spawnItem) orig(self);

            new SyncTotalCheckProgress(finishedAllChecks ? TotalChecks : CurrentChecks, TotalChecks).Send(NetworkDestination.Clients);

            if (finishedAllChecks)
            {
                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
                new AllChecksComplete().Send(NetworkDestination.Clients);
            }
        }

        private void PickupDropletController_CreatePickupDroplet_CreatePickupInfo(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig, GenericPickupController.CreatePickupInfo pickupInfo, Vector3 position, Vector3 velocity)
        {

            if (Array.IndexOf(skippedItems, pickupInfo._pickupState) >= 0)
            {
                orig(pickupInfo, position, velocity);
                return;
            }

            // Run `HandleItemDrop()` first so that the `PickedUpItemCount` is incremented by the time `ItemDropProcessed()` is called.
            var spawnItem = finishedAllChecks || HandleItemDrop();

            if (OnItemDropProcessed != null)
            {
                OnItemDropProcessed(PickedUpItemCount);
            }

            if (spawnItem)
            {
                orig(pickupInfo, position, velocity);
            }

            if (!spawnItem)
            {
                EffectManager.SpawnEffect(smokescreenPrefab, new EffectData() { origin = position }, true);
            }

            new SyncTotalCheckProgress(finishedAllChecks ? TotalChecks : CurrentChecks, TotalChecks).Send(NetworkDestination.Clients);

            if (finishedAllChecks)
            {
                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
                new AllChecksComplete().Send(NetworkDestination.Clients);
            }
        }
        private bool HandleItemDrop()
        {
            PickedUpItemCount += 1;
            Log.LogDebug($"PickedUpItemCount + 1 {PickedUpItemCount}  ItemPickupStep {ItemPickupStep}");
            if ((PickedUpItemCount % ItemPickupStep) == 0)
            {
                CurrentChecks++;
                //CurrentChecks = PickedUpItemCount / ItemPickupStep;
                //ArchipelagoTotalChecksObjectiveController.CurrentChecks = CurrentChecks;
                var itemSendName = $"ItemPickup{CurrentChecks}";
                var itemLocationId = ItemStartId + CurrentChecks - 1; // because CurrentChecks is incremented first, subtract one to use the current id
                Log.LogDebug($"Sent out location {itemSendName} (id: {itemLocationId})");

                var packet = new LocationChecksPacket();
                packet.Locations = new List<long> { itemLocationId }.ToArray();

                session.Socket.SendPacketAsync(packet);
                if (CurrentChecks == TotalChecks)
                {
                    ArchipelagoTotalChecksObjectiveController.CurrentChecks = ArchipelagoTotalChecksObjectiveController.TotalChecks;
                    finishedAllChecks = true;
                }
                return false;
            }
            return true;
        }
    }
}
