using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Extensions;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using HarmonyLib;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Archipelago.RiskOfRain2
{
    public class ArchipelagoItemLogicController : IDisposable
    {
        public int PickedUpItemCount { get; set; }
        public int ItemPickupStep { get; set; }
        public int CurrentChecks { get; set; }
        public int TotalChecks { get; set; }

        public long[] ChecksTogether { get; set; }
        public long[] MissingChecks { get; set; }

        public delegate void ItemDropProcessedHandler(int pickedUpCount);
        public event ItemDropProcessedHandler OnItemDropProcessed;

        private bool finishedAllChecks = false;
        private ArchipelagoSession session;
        private Queue<string> itemReceivedQueue = new Queue<string>();
        private PickupIndex[] skippedItems;

        private GameObject smokescreenPrefab;

        private bool IsInGame
        {
            get
            {
                return (RoR2Application.isInSinglePlayer || RoR2Application.isInMultiPlayer) && RoR2.Run.instance != null;
            }
        }

        public ArchipelagoItemLogicController(ArchipelagoSession session)
        {
            this.session = session;
            On.RoR2.PickupDropletController.CreatePickupDroplet_PickupIndex_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet;
            On.RoR2.RoR2Application.Update += RoR2Application_Update;
            session.Socket.PacketReceived += Session_PacketReceived;
            session.Items.ItemReceived += Items_ItemReceived;

            Log.LogDebug("Okay finished hooking.");
            smokescreenPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/RoR2/Junk/Characters/Bandit/Skills/SmokescreenEffect.prefab").WaitForCompletion();
            Log.LogDebug("Okay, finished getting prefab.");

            skippedItems = new PickupIndex[]
            {
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixBlue.equipmentIndex),
                //PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixEcho.equipmentIndex), // Causes NRE... Not sure why.
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixHaunted.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixLunar.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixPoison.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixRed.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Equipment.AffixWhite.equipmentIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.MiscPickups.LunarCoin.miscPickupIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Items.ArtifactKey.itemIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Bomb.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Command.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.EliteOnly.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Enigma.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.FriendlyFire.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Glass.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.MixEnemy.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.MonsterTeamGainsItems.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.RandomSurvivorOnRespawn.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Sacrifice.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.ShadowClone.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.SingleMonsterType.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.Swarms.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.TeamDeath.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.WeakAssKnees.artifactIndex),
                PickupCatalog.FindPickupIndex(RoR2Content.Artifacts.WispOnDeath.artifactIndex),
            };
            Log.LogDebug("Ok, finished browsing catalog.");
        }

        private void Items_ItemReceived(MultiClient.Net.Helpers.ReceivedItemsHelper helper)
        {
            var newItem = helper.DequeueItem();
            EnqueueItem(newItem.Item);
        }

        private void Session_PacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.Connected:
                    {
                        var connectedPacket = packet as ConnectedPacket;
                        // Add 1 because the user's YAML will contain a value equal to "number of pickups before sent location"
                        ItemPickupStep = Convert.ToInt32(connectedPacket.SlotData["itemPickupStep"]) + 1;
                        TotalChecks = connectedPacket.LocationsChecked.Count() + connectedPacket.MissingChecks.Count();

/*                        for (int i = 0; i < connectedPacket.MissingChecks.Length; i++)
                        {
                            Log.LogInfo($"Missing Checks {connectedPacket.MissingChecks[i]}");
                        }
                        for (int i = 0; i < connectedPacket.LocationsChecked.Length; i++)
                        {
                            Log.LogInfo($"Locations Checks {connectedPacket.LocationsChecked[i]}");
                        }*/
                        
                        ChecksTogether = connectedPacket.LocationsChecked.Concat(connectedPacket.MissingChecks).ToArray();
                        ChecksTogether = ChecksTogether.OrderBy(n => n).ToArray();
                        for (int i = 0; i < ChecksTogether.Length; i++)
                        {
                            Log.LogInfo($"Checks Together {ChecksTogether[i]}");
                        }
                        MissingChecks = connectedPacket.MissingChecks;
                        // Old way
                        // CurrentChecks = TotalChecks - connectedPacket.MissingChecks.Count();
                        Log.LogDebug($"Missing Checks {connectedPacket.MissingChecks.Count()} totalChecks {TotalChecks} Locations Checked {connectedPacket.LocationsChecked.Count()}");
                        if (connectedPacket.MissingChecks.Count() > 0) {
                            var missingIndex = Array.IndexOf(ChecksTogether, connectedPacket.MissingChecks[0]);
                            Log.LogInfo($"Missing index is {missingIndex} first missing id is {connectedPacket.MissingChecks[0]}");
                            //var FirstMissing = connectedPacket.MissingChecks[0] - 37000;
                            CurrentChecks = missingIndex;
                            PickedUpItemCount = missingIndex * ItemPickupStep;
                        } else
                        {
                            CurrentChecks = TotalChecks - connectedPacket.MissingChecks.Count();
                            PickedUpItemCount = connectedPacket.LocationsChecked.Count() * ItemPickupStep;
                        }
                        
                        ArchipelagoTotalChecksObjectiveController.CurrentChecks = CurrentChecks;
                        ArchipelagoTotalChecksObjectiveController.TotalChecks = TotalChecks;

                        new SyncTotalCheckProgress(CurrentChecks, TotalChecks).Send(NetworkDestination.Clients);
                        if (CurrentChecks == TotalChecks)
                        {
                            ArchipelagoTotalChecksObjectiveController.CurrentChecks = ArchipelagoTotalChecksObjectiveController.TotalChecks;
                            finishedAllChecks = true;
                        }
                        // Add up pickedUpItemCount so that resuming a game is possible. The intended behavior is that you immediately receive
                        // all of the items you are granted. This is for restarting (in case you lose a run but are not in commencement). 
                        
                        break;
                    }
            }
        }

        public void EnqueueItem(long itemId)
        {
            var item = session.Items.GetItemName(itemId);
            itemReceivedQueue.Enqueue(item);
        }

        public void Dispose()
        {
            On.RoR2.PickupDropletController.CreatePickupDroplet_PickupIndex_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet;
            On.RoR2.RoR2Application.Update -= RoR2Application_Update;

            if (session != null)
            {
                session.Socket.PacketReceived -= Session_PacketReceived;
                session = null;
            }
        }

        private void RoR2Application_Update(On.RoR2.RoR2Application.orig_Update orig, RoR2Application self)
        {
            if (IsInGame && itemReceivedQueue.Any())
            {
                HandleReceivedItemQueueItem();
            }

            orig(self);
        }

        private void HandleReceivedItemQueueItem()
        {
            string itemReceived = itemReceivedQueue.Dequeue();

            switch (itemReceived)
            {
                case "Common Item":
                    var common = Run.instance.availableTier1DropList.Choice();
                    GiveItemToPlayers(common);
                    break;
                case "Uncommon Item":
                    var uncommon = Run.instance.availableTier2DropList.Choice();
                    GiveItemToPlayers(uncommon);
                    break;
                case "Legendary Item":
                    var legendary = Run.instance.availableTier3DropList.Choice();
                    GiveItemToPlayers(legendary);
                    break;
                case "Boss Item":
                    var boss = Run.instance.availableBossDropList.Choice();
                    GiveItemToPlayers(boss);
                    break;
                case "Lunar Item":
                    var lunar = Run.instance.availableLunarCombinedDropList.Choice();
                    var pickupDef = PickupCatalog.GetPickupDef(lunar);
                    if (pickupDef.itemIndex != ItemIndex.None)
                    {
                        GiveItemToPlayers(lunar);
                    }
                    else if (pickupDef.equipmentIndex != EquipmentIndex.None)
                    {
                        GiveEquipmentToPlayers(lunar);
                    }
                    break;
                case "Equipment":
                    var equipment = Run.instance.availableEquipmentDropList.Choice();
                    GiveEquipmentToPlayers(equipment);
                    break;
                case "Item Scrap, White":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapWhite.itemIndex));
                    break;
                case "Item Scrap, Green":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapGreen.itemIndex));
                    break;
                case "Item Scrap, Red":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapRed.itemIndex));
                    break;
                case "Item Scrap, Yellow":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapYellow.itemIndex));
                    break;
                case "Dio's Best Friend":
                    GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLife.itemIndex));
                    break;
            }
        }

        private void GiveEquipmentToPlayers(PickupIndex pickupIndex)
        {
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                var inventory = player.master.inventory;
                var activeEquipment = inventory.GetEquipment(inventory.activeEquipmentSlot);
                if (!activeEquipment.Equals(EquipmentState.empty))
                {
                    var playerBody = player.master.GetBodyObject();

                    if (playerBody == null)
                    {
                        //TODO: maybe deal with this
                        return;
                    }

                    var pickupInfo = new GenericPickupController.CreatePickupInfo()
                    {
                        pickupIndex = PickupCatalog.FindPickupIndex(activeEquipment.equipmentIndex),
                        position = playerBody.transform.position,
                        rotation = Quaternion.identity
                    };
                    GenericPickupController.CreatePickup(pickupInfo);
                }

                inventory.SetEquipmentIndex(PickupCatalog.GetPickupDef(pickupIndex)?.equipmentIndex ?? EquipmentIndex.None);
                DisplayPickupNotification(pickupIndex);
            }
        }

        private void GiveItemToPlayers(PickupIndex pickupIndex)
        {
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                var inventory = player.master.inventory;
                inventory.GiveItem(PickupCatalog.GetPickupDef(pickupIndex)?.itemIndex ?? ItemIndex.None);
                DisplayPickupNotification(pickupIndex);
            }
        }

        private void DisplayPickupNotification(PickupIndex index)
        {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(index);
            var color = pickupDef.baseColor;
            var index_text = pickupDef.nameToken;
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                CharacterMasterNotificationQueue.PushPickupNotification(player.master, index);
                Chat.AddPickupMessage(player.master.GetBody(), index_text, color, 1);
            }
        }

        private void PickupDropletController_CreatePickupDroplet(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_PickupIndex_Vector3_Vector3 orig, PickupIndex pickupIndex, Vector3 position, Vector3 velocity)
        {
            if (skippedItems.Contains(pickupIndex))
            {
                orig(pickupIndex, position, velocity);
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
                orig(pickupIndex, position, velocity);
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
                var itemLocationId = session.Locations.GetLocationIdFromName("Risk of Rain 2", itemSendName);
                Log.LogDebug($"Sent out location {itemSendName} (id: {itemLocationId})");

                var packet = new LocationChecksPacket();
                packet.Locations = new List<long> { itemLocationId }.ToArray();

                session.Socket.SendPacket(packet);
                if (CurrentChecks == TotalChecks)
                {
                    ArchipelagoTotalChecksObjectiveController.CurrentChecks = ArchipelagoTotalChecksObjectiveController.TotalChecks;
                    finishedAllChecks = true;
                }
                MissingChecks = MissingChecks.Skip(1).ToArray();
                var missingIndex = Array.IndexOf(ChecksTogether, MissingChecks[0]);
                CurrentChecks = missingIndex;
                PickedUpItemCount = missingIndex * ItemPickupStep;
                //CurrentChecks = PickedUpItemCount / ItemPickupStep;
                Log.LogDebug($"Current Checks {CurrentChecks}");
                ArchipelagoTotalChecksObjectiveController.CurrentChecks = CurrentChecks;
                return false;
            }
            return true;
        }
    }
}
