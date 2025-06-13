using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.RiskOfRain2.Extensions;
using Archipelago.RiskOfRain2.Handlers;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking;
using R2API.Utils;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using System.Collections.ObjectModel;
using KinematicCharacterController;

namespace Archipelago.RiskOfRain2
{
    public class ArchipelagoItemLogicController : IDisposable
    {
        public int PickedUpItemCount { get; set; }
        public int ItemPickupStep { get; set; }
        public long ItemStartId { get; private set; }
        public int CurrentChecks { get; set; }
        public int TotalChecks { get; set; }
        System.Random rnd = new System.Random();

        internal StageBlockerHandler Stageblockerhandler { get; set; }

        public long[] ChecksTogether { get; set; }
        public long[] MissingChecks { get; set; }

        public delegate void ItemDropProcessedHandler(int pickedUpCount);
        public event ItemDropProcessedHandler OnItemDropProcessed;

        private bool finishedAllChecks = false;
        private ArchipelagoSession session;
        private Queue<KeyValuePair<long, string>> itemReceivedQueue = new Queue<KeyValuePair<long, string>>();
        private Queue<KeyValuePair<long, string>> environmentReceivedQueue = new Queue<KeyValuePair<long, string>>();
        private Queue<KeyValuePair<long, string>> fillerReceivedQueue = new Queue<KeyValuePair<long, string>>();
        private Queue<KeyValuePair<long, string>> trapReceivedQueue = new Queue<KeyValuePair<long, string>>();
        private Queue<KeyValuePair<long, string>> stageReceivedQueue = new Queue<KeyValuePair<long, string>>();
        // TODO get magic numbers from somewhere else (eg move to LocationHandler.cs)
        private const long environmentRangeLower = 37700;
        private const long environmentRangeUpper = 37999;
        private const long fillerRangeLower = 37300;
        private const long fillerRangeUpper = 37399;
        private const long trapRangeLower = 37400;
        private const long trapRangeUpper = 37499;
        private const long stageRangeLower = 37500;
        private const long stageRangeUpper = 37599;
        private int lastReceivedItemindex = 0;
        private bool spawnedMonster = false;
        private bool monsterShrineRecently = false;
        private bool teleportedRecently = false;
        private bool exitedPod = false;
        private PickupIndex[] skippedItems;

        private GameObject smokescreenPrefab;
        private GameObject portalPrefab;
        private CombatDirector combatDirector;

        private bool IsInGame
        {
            get
            {
                return (RoR2Application.isInSinglePlayer || RoR2Application.isInMultiPlayer) && Run.instance != null && exitedPod;
            }
        }

        public ArchipelagoItemLogicController(ArchipelagoSession session)
        {
            this.session = session;
            // get the initial id from the seed for backwards compatibility
            ItemStartId = session.Locations.GetLocationIdFromName("Risk of Rain 2", "ItemPickup1");

            // TODO all the hooks for ArchipelagoItemLogicController should probably be moved into a hook method
            On.RoR2.RoR2Application.Update += RoR2Application_Update;
            On.RoR2.SceneDirector.Start += SceneDirector_Start;
            session.Socket.PacketReceived += Session_PacketReceived;
            session.Items.ItemReceived += Items_ItemReceived;
            On.RoR2.CombatDirector.Awake += CombatDirector_Awake;
            On.RoR2.SurvivorPodController.OnPassengerExit += SurvivorPodController_OnPassengerExit;
            Log.LogDebug("Okay finished hooking.");
            smokescreenPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/Bandit/SmokescreenEffect.prefab").WaitForCompletion();
            // TODO Spawns the seerStation portal to pick where to go.. changing the id in game doesn't work.. looks to be a NetworkBehavior thing
            // portalPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/bazaar/SeerStation.prefab").WaitForCompletion();
            
            Log.LogDebug("Okay, finished getting prefab.");
            Log.LogDebug($"smokescreen {smokescreenPrefab}");

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

        private void SceneDirector_Start(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            orig(self);
            exitedPod = true;
            ArchipelagoClient.isInGame = true;
        }

        private void SurvivorPodController_OnPassengerExit(On.RoR2.SurvivorPodController.orig_OnPassengerExit orig, SurvivorPodController self, GameObject passenger)
        {
            orig(self, passenger);
            // prevent teleport on exiting pod
            Thread thread = new Thread(() => TeleportedRecently());
            thread.Start();
            teleportedRecently = true;
            exitedPod = true;
            ArchipelagoClient.isInGame = true;
        }

        private void CombatDirector_Awake(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector self)
        {
            orig(self);
            combatDirector = self;
        }

        private void Items_ItemReceived(ReceivedItemsHelper helper)
        {
            var newItem = helper.DequeueItem();
            if (ArchipelagoClient.lastReceivedItemindex < helper.AllItemsReceived.Count)
            {
                EnqueueItem(newItem.ItemId);
                ArchipelagoClient.lastReceivedItemindex = helper.AllItemsReceived.Count;
            }
            else if (environmentRangeLower <= newItem.ItemId && newItem.ItemId <= environmentRangeUpper)
            {
                EnqueueItem(newItem.ItemId);
            }
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
        private void Session_PacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.Connected:
                    {
                        var connectedPacket = packet as ConnectedPacket;

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
                            On.RoR2.PickupDropletController.CreatePickupDroplet_PickupIndex_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet;
                            On.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop;
                            
                            session.Locations.CheckedLocationsUpdated += Check_Locations;
                        }
                        else
                        {
                            On.RoR2.PickupDropletController.CreatePickupDroplet_PickupIndex_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet;
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
                        break;
                    }
/*                case ArchipelagoPacketType.ReceivedItems:
                    var receivedItemsPacket = (ReceivedItemsPacket)packet;


                    break;*/
            }
        }

        public void EnqueueItem(long itemId)
        {
            // convert the itemId to a name here instead of in the main loop
            // this prevents a call to the session in the RoR2Application_Update
            var itemName = session.Items.GetItemName(itemId);
            // We will keep track of the item id as well as since the name cannot be converted back to an id.

            // Separate the environments and items so that the environments can be precollected
            //  when the run starts.
            if (environmentRangeLower <= itemId && itemId <= environmentRangeUpper)
            {
                environmentReceivedQueue.Enqueue(new KeyValuePair<long, string>(itemId, itemName));
            }
            else if (fillerRangeLower <= itemId && itemId <= fillerRangeUpper)
            {
                fillerReceivedQueue.Enqueue(new KeyValuePair<long, string>(itemId, itemName));
            }
            else if (trapRangeLower <= itemId && itemId <= trapRangeUpper) {
                trapReceivedQueue.Enqueue(new KeyValuePair<long, string>(itemId, itemName));
            }
            else if (stageRangeLower <= itemId && itemId <= stageRangeUpper)
            {
                stageReceivedQueue.Enqueue(new KeyValuePair<long, string>(itemId, itemName));
            }
            else
            {
                itemReceivedQueue.Enqueue(new KeyValuePair<long, string>(itemId, itemName));
            }

        }

        public void Dispose()
        {
            On.RoR2.PickupDropletController.CreatePickupDroplet_PickupIndex_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet;
            On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop;
            On.RoR2.RoR2Application.Update -= RoR2Application_Update;

            if (session != null)
            {
                session.Socket.PacketReceived -= Session_PacketReceived;
                session.Items.ItemReceived -= Items_ItemReceived;
                session = null;
            }
        }

        /**
         * At the start of a run, we need to precollect all environments before environments are picked for stages.
         */
        public void Precollect()
        {
            while (environmentReceivedQueue.Any())
            {
                Log.LogDebug("Precollecting environment...");
                HandleReceivedEnvironmentQueueItem();
            }
        }

        private void RoR2Application_Update(On.RoR2.RoR2Application.orig_Update orig, RoR2Application self)
        {
            if (environmentReceivedQueue.Any())
            {
                HandleReceivedEnvironmentQueueItem();
            }
            if (stageReceivedQueue.Any())
            {
                HandleReceivedStageQueueItem();
            }
            if (IsInGame)
            {
                if (itemReceivedQueue.Any())
                {
                    HandleReceivedItemQueueItem();
                }

                if (fillerReceivedQueue.Any())
                {
                    HandleReceivedFillerQueueItem();
                }
                if (trapReceivedQueue.Any())
                {
                    HandleReceivedTrapQueueItem();
                }
            }

            orig(self);
        }

        private void HandleReceivedEnvironmentQueueItem()
        {
            KeyValuePair<long, string> itemReceived = environmentReceivedQueue.Dequeue();

            long itemIdReceived = itemReceived.Key;
            string itemNameReceived = itemReceived.Value;
            if (itemIdReceived == environmentRangeLower + 46 && itemNameReceived == "The Planetarium")
            {
                itemIdReceived = environmentRangeLower + 45;
                Log.LogDebug($"Changing id to 45");
            }
            else if (itemIdReceived == environmentRangeLower + 45 && itemNameReceived == "Void Locus")
            {
                itemIdReceived = environmentRangeLower + 46;
                Log.LogDebug($"Changing id to 46");
            }
            Log.LogDebug($"Handling environment with itemid {itemIdReceived} with name {itemNameReceived}");
            Stageblockerhandler?.UnBlock((int)(itemIdReceived - environmentRangeLower));
            if (IsInGame)
            {
                ChatMessage.SendColored($"Received {itemNameReceived}!", Color.magenta);
            }
        }
        private void HandleReceivedFillerQueueItem()
        {
            KeyValuePair<long, string> itemReceived = fillerReceivedQueue.Dequeue();

            long itemIdReceived = itemReceived.Key;
            string itemNameReceived = itemReceived.Value;
            switch (itemIdReceived)
            {
                // Money
                case 37301:
                    GiveMoneyToPlayers();
                    break;
                // Lunar Coin
                case 37302:
                    GiveLunarCoinToPlayers();
                    break;
                // EXP
                case 37303:
                    GiveExperienceToPlayers();
                    break;
            }
        }
        private void HandleReceivedTrapQueueItem()
        {
            KeyValuePair<long, string> itemReceived = trapReceivedQueue.Dequeue();

            long itemIdReceived = itemReceived.Key;
            string itemNameReceived = itemReceived.Value;
            switch (itemIdReceived)
            {
                // Adds an extra boss to teleporter
                case 37401:
                    MountainShrineTrap();
                    break;
                // Increases monsters level by adding time to the clock.
                case 37402:
                    TimeWarpTrap();
                    break;
                // Immitate Combat Shrine.
                case 37403:
                    SpawnMonstersTrap();
                    break;
                case 37404:
                    TeleportPlayer();
                    break;
            }
        }
        private void HandleReceivedStageQueueItem()
        {
            KeyValuePair<long, string> itemReceived = stageReceivedQueue.Dequeue();

            long itemIdRecieved = itemReceived.Key;
            string itemNameReceived = itemReceived.Value;
            if (itemIdRecieved == 37505)
            {
                StageBlockerHandler.amountOfStages += 1;
                ChatMessage.SendColored($"Received {itemNameReceived} #{StageBlockerHandler.amountOfStages}!", Color.magenta);
            } 
            else
            {
                StageBlockerHandler.stageUnlocks[itemNameReceived] = true;
                ChatMessage.SendColored($"Received {itemNameReceived}!", Color.magenta);
            }
            
        }

        private void HandleReceivedItemQueueItem()
        {
            KeyValuePair<long, string> itemReceived = itemReceivedQueue.Dequeue();

            long itemIdRecieved = itemReceived.Key;
            string itemNameReceived = itemReceived.Value;

            Log.LogDebug($"Handling item with itemid {itemIdRecieved} with name {itemNameReceived}");

            switch (itemIdRecieved)
            {
                // TODO move the magic numbers to variables
                // "Common Item"
                case 37002:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        var common = Run.instance.availableTier1DropList.Choice();
                        GiveItemToPlayers(common, player);
                    }
                    break;
                // "Uncommon Item"
                case 37003:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        var uncommon = Run.instance.availableTier2DropList.Choice();
                        GiveItemToPlayers(uncommon, player);
                    }

                    break;
                // "Legendary Item"
                case 37004:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        var legendary = Run.instance.availableTier3DropList.Choice();
                        GiveItemToPlayers(legendary, player);
                    }

                    break;
                // "Boss Item"
                case 37005:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        var boss = Run.instance.availableBossDropList.Choice();
                        GiveItemToPlayers(boss, player);
                    }
                    break;
                // "Lunar Item"
                case 37006:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        var lunar = Run.instance.availableLunarCombinedDropList.Choice();
                        var pickupDef = PickupCatalog.GetPickupDef(lunar);
                        if (pickupDef.itemIndex != ItemIndex.None)
                        {
                            GiveItemToPlayers(lunar, player);
                        }
                        else if (pickupDef.equipmentIndex != EquipmentIndex.None)
                        {
                            GiveEquipmentToPlayers(lunar, player);
                        }
                    }
                    break;
                
                // "Equipment"
                case 37007:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        var equipment = Run.instance.availableEquipmentDropList.Choice();
                        GiveEquipmentToPlayers(equipment, player);
                    }
                    break;
                // "Item Scrap, White"
                case 37008:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapWhite.itemIndex), player);
                    }
                    break;
                // "Item Scrap, Green"
                case 37009:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapGreen.itemIndex), player);
                    }
                    break;
                // "Item Scrap, Red"
                case 37010:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapRed.itemIndex), player);
                    }
                    break;
                // "Item Scrap, Yellow"
                case 37011:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapYellow.itemIndex), player);
                    }
                    break;
                // "Void Item"
                case 37012:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        int voidWeight = 70 + 40 + 10 + 5;
                        int voidChoice = rnd.Next(voidWeight);
                        var voidItem = new PickupIndex();
                        if (voidChoice <= 70)
                        {
                            voidItem = Run.instance.availableVoidTier1DropList.Choice();
                        }
                        else if (voidChoice <= 110)
                        {
                            voidItem = Run.instance.availableVoidTier2DropList.Choice();
                        }
                        else if (voidChoice <= 120)
                        {
                            voidItem = Run.instance.availableVoidTier3DropList.Choice();
                        }
                        else
                        {
                            voidItem = Run.instance.availableVoidBossDropList.Choice();
                        }
                        GiveItemToPlayers(voidItem, player);
                    }
                    break;
                // Beads of Fealty
                case 37013:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.LunarTrinket.itemIndex), player);
                    }
                    break;
                // Radar Scanner Equipment
                case 37014:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        GiveEquipmentToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Equipment.Scanner.equipmentIndex), player);
                    }
                    break;
                // "Dio's Best Friend"
                case 37001:
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        GiveItemToPlayers(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLife.itemIndex), player);
                    }
                    break;
                
            }
        }

        private void GiveEquipmentToPlayers(PickupIndex pickupIndex, PlayerCharacterMasterController player)
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
            if (!NetworkServer.active)
            {
                CharacterMasterNotificationQueue.PushPickupNotification(player.master, pickupIndex);
                return;
            }
            DisplayPickupNotification(pickupIndex, player);
        }

        private void GiveItemToPlayers(PickupIndex pickupIndex, PlayerCharacterMasterController player)
        {
            var inventory = player.master.inventory;
            inventory.GiveItem(PickupCatalog.GetPickupDef(pickupIndex)?.itemIndex ?? ItemIndex.None);
            if (!NetworkServer.active)
            {
                CharacterMasterNotificationQueue.PushPickupNotification(player.master, pickupIndex);
                return;
            }
            DisplayPickupNotification(pickupIndex, player);
        }
        private void GiveMoneyToPlayers()
        {
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                var coefficient = Run.instance.difficultyCoefficient;
                uint money = (uint)(100 * coefficient);
                Log.LogDebug($"Received {money}");
                player.master.money += money;
                // Chat.AddPickupMessage(player.master.GetBody(), $"${Math.Floor(300 * coefficient)}!!!", Color.green, 1);
                Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
                {
                    subjectAsCharacterBody = player.master.GetBody(),
                    baseToken = "PLAYER_PICKUP",
                    pickupToken = $"${money}!!!",
                    pickupColor = Color.green,
                    pickupQuantity = 1

                });
            }
        }
        private void GiveLunarCoinToPlayers()
        {
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                GameObject lunarCoin = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/GenericPickup.prefab").WaitForCompletion();
                //var coin = GameObject.Instantiate(lunarCoin);
                SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
                spawnCard.prefab = lunarCoin;

                Xoroshiro128Plus xoroshiro128PlusRadioScanner = new Xoroshiro128Plus(RoR2Application.rng);
                if (DirectorCore.instance != null)
                {
                    var card = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Direct,
                        spawnOnTarget = player.master.GetBody().transform,
                        minDistance = 1f,
                        maxDistance = 10f,
                    }, xoroshiro128PlusRadioScanner));
                    var position = card.transform.position;
                    card.GetComponent<GenericPickupController>().NetworkpickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.MiscPickups.LunarCoin.miscPickupIndex);
                    Log.LogDebug($"coin position {position + new Vector3(0, 10, 0)}");
                    NetworkServer.Spawn(card);
                }
            }
        }
        private void GiveExperienceToPlayers()
        {
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                player.master.GiveExperience(1000);
                //Chat.AddPickupMessage(player.master.GetBody(), "1000 XP", Color.white, 1);
                Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
                {
                    subjectAsCharacterBody = player.master.GetBody(),
                    baseToken = "PLAYER_PICKUP",
                    pickupToken = "1000 XP",
                    pickupColor = Color.white,
                    pickupQuantity = 1

                });
            }
        }
        private void MountainShrineTrap()
        {
            if (!monsterShrineRecently)
            {
                ChatMessage.SendColored("<style=cShrine>The Mountain has invited you for a challenge..", Color.yellow);
                TeleporterInteraction.instance.AddShrineStack();
                monsterShrineRecently = true;
                Thread thread = new Thread(() => MountainShrineRecently());
                thread.Start();
                PlayShrineSound();
            }
        }
        private void MountainShrineRecently()
        {
            Thread.Sleep(2000);
            Log.LogDebug("You can get another mountain trap now.");
            monsterShrineRecently = false;
        }
        private void PlayShrineSound()
        {
            if (PlayerCharacterMasterController.instances != null)
            {

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                {
                    origin = PlayerCharacterMasterController.instances[0].body.transform.position,
                }, true);
            }
        }
        private void SpawnMonstersTrap()
        {
            if (combatDirector != null && !spawnedMonster)
            {
                var player = PlayerCharacterMasterController.instances[0];
                if (player.master.GetBody() == null)
                {
                    return;
                }
                spawnedMonster = true;
                Thread thread = new Thread(() => SpawnedMonstersRecently());
                thread.Start();
                var coefficient = Run.instance.difficultyCoefficient;
                combatDirector.monsterCredit = 100f * coefficient;
                Log.LogDebug($"player position {player.master.GetBody().transform.localPosition} monster credit  100 * {coefficient} =  {100 * coefficient}");
                combatDirector.SpendAllCreditsOnMapSpawns(player.master.GetBody().transform);
                ChatMessage.SendColored("Incoming Monsters!!", Color.red);
                PlayShrineSound();
            }
        }
        private void SpawnedMonstersRecently()
        {
            Thread.Sleep(2000);
            Log.LogDebug("You can get another monster trap now.");
            spawnedMonster = false;
        }

        // TODO The currently spawns players to the center of the map aka (0, 0, 0) where we would want it to be a random location.
        private void TeleportPlayer()
        {
            if (!teleportedRecently)
            {
                //foreach (var player in PlayerCharacterMasterController.instances)
                foreach (NetworkUser local in NetworkUser.readOnlyLocalPlayersList)
                {
                    if (local)
                    {
                        SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
                        spawnCard = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscBarrel1");

                        Xoroshiro128Plus xoroshiro128PlusRadioScanner = new Xoroshiro128Plus(RoR2Application.rng);
                        if (DirectorCore.instance != null)
                        {
                            var card = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
                            {
                                placementMode = DirectorPlacementRule.PlacementMode.Random
                            }, xoroshiro128PlusRadioScanner));
                            var position = card.transform.position;
                            var directorPlacement = new DirectorPlacementRule
                            {
                                placementMode = DirectorPlacementRule.PlacementMode.Random,
                                minDistance = 5f,
                                maxDistance = 20f,
                            };
                            Log.LogDebug($"directorPlacemnet {directorPlacement.targetPosition} card position {position + new Vector3(0, 10, 0)} player position {local.master.transform.position}");
                            var body = local.master.GetBody();
                            body.GetComponentInChildren<KinematicCharacterMotor>().SetPosition(position + new Vector3(0, 10, 0));
                            new ArchipelagoTeleportClient().Send(NetworkDestination.Clients);
                            card.SetActive(false);
                        }
                    }
                }
            }
        }
        private void TeleportedRecently()
        {
            Thread.Sleep(2000);
            Log.LogDebug("You can teleport again");
            teleportedRecently = false;
        }
        private void TimeWarpTrap()
        {
            var time = Run.instance.GetRunStopwatch();
            time += 180;
            Run.instance.SetRunStopwatch(time);
            ChatMessage.SendColored($"Monsters grow stronger with time!", Color.red);
            TeamManager.instance.SetTeamLevel(TeamIndex.Monster, 1);
        }

        private void DisplayPickupNotification(PickupIndex index, PlayerCharacterMasterController player)
        {
            CharacterMasterNotificationQueue notificationQueueForMaster = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(player.master);
            PickupDef pickupDef = PickupCatalog.GetPickupDef(index);
            ItemIndex itemIndex = pickupDef.itemIndex;
            if (itemIndex != ItemIndex.None)
            {
                notificationQueueForMaster.PushNotification(new CharacterMasterNotificationQueue.NotificationInfo(ItemCatalog.GetItemDef(itemIndex), null), 2f);
            }
            EquipmentIndex equipmentIndex = pickupDef.equipmentIndex;
            if (equipmentIndex != EquipmentIndex.None)
            {
                notificationQueueForMaster.PushNotification(new CharacterMasterNotificationQueue.NotificationInfo(EquipmentCatalog.GetEquipmentDef(equipmentIndex), null), 2f);
            }
            var color = pickupDef.baseColor;
            var index_text = pickupDef.nameToken;
            //CharacterMasterNotificationQueue.PushPickupNotification(player.master, index);
            //Chat.AddPickupMessage(player.master.GetBody(), index_text, color, 1);
            Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
            {
                subjectAsCharacterBody = player.master.GetBody(),
                baseToken = "PLAYER_PICKUP",
                pickupToken = index_text,
                pickupColor = color,
                pickupQuantity = 1

            });

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
                // SpawnPortal(position);
            }

            new SyncTotalCheckProgress(finishedAllChecks ? TotalChecks : CurrentChecks, TotalChecks).Send(NetworkDestination.Clients);

            if (finishedAllChecks)
            {
                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
                new AllChecksComplete().Send(NetworkDestination.Clients);
            }
        }
/*        private void SpawnPortal(Vector3 position)
        {
            if (portalPrefab != null)
            {
                var portal = GameObject.Instantiate(portalPrefab);
                portal.transform.localPosition = position;
                portal.transform.localScale = Vector3.one;
                portal.GetComponent<SeerStationController>().GetNetworkChannel();
                //connect to scene exit
                //add stage id
            }
        }*/
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
