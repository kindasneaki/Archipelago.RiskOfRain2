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
using R2API.Networking;
using R2API.Utils;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using KinematicCharacterController;

namespace Archipelago.RiskOfRain2
{
    public partial class ArchipelagoItemLogicController : IDisposable
    {
        public int ItemPickupStep { get; set; }
        public int CurrentChecks { get; set; }
        public int TotalChecks { get; set; }
        System.Random rnd = new System.Random();

        internal StageBlockerHandler Stageblockerhandler { get; set; }

        /**
         * An item waiting to be applied.
         * Silent items are ones the server re-sent on reconnect that were already applied in a
         * previous session; they still need to be applied to rebuild state but must not be announced.
         */
        private readonly struct QueuedItem
        {
            public readonly long Id;
            public readonly string Name;
            public readonly bool Silent;

            public QueuedItem(long id, string name, bool silent)
            {
                Id = id;
                Name = name;
                Silent = silent;
            }
        }

        private ArchipelagoSession session;
        private Queue<KeyValuePair<long, string>> itemReceivedQueue = new Queue<KeyValuePair<long, string>>();
        private Queue<QueuedItem> environmentReceivedQueue = new Queue<QueuedItem>();
        private Queue<KeyValuePair<long, string>> fillerReceivedQueue = new Queue<KeyValuePair<long, string>>();
        private Queue<KeyValuePair<long, string>> trapReceivedQueue = new Queue<KeyValuePair<long, string>>();
        private Queue<QueuedItem> stageReceivedQueue = new Queue<QueuedItem>();
        // TODO get magic numbers from somewhere else (eg move to LocationHandler.cs)
        private const long environmentRangeLower = 37700;
        private const long environmentRangeUpper = 37999;
        private const long fillerRangeLower = 37300;
        private const long fillerRangeUpper = 37399;
        private const long trapRangeLower = 37400;
        private const long trapRangeUpper = 37499;
        private const long stageRangeLower = 37500;
        private const long stageRangeUpper = 37599;
        private bool spawnedMonster = false;
        private bool monsterShrineRecently = false;
        private bool teleportedRecently = false;
        private bool exitedPod = false;

        private GameObject smokescreenPrefab;
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

            Log.LogDebug("Okay, finished getting prefab.");
            Log.LogDebug($"smokescreen {smokescreenPrefab}");

            InitializeClassic();
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
            else if (IsReplayable(newItem.ItemId))
            {
                EnqueueItem(newItem.ItemId, true);
            }
        }

        private static bool IsReplayable(long itemId)
        {
            return (environmentRangeLower <= itemId && itemId <= environmentRangeUpper)
                || (stageRangeLower <= itemId && itemId <= stageRangeUpper);
        }
        private void Session_PacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.Connected:
                    {
                        var connectedPacket = packet as ConnectedPacket;
                        HandleClassicConnected(connectedPacket);
                        break;
                    }
/*                case ArchipelagoPacketType.ReceivedItems:
                    var receivedItemsPacket = (ReceivedItemsPacket)packet;


                    break;*/
            }
        }



        public void EnqueueItem(long itemId, bool silent = false)
        {
            // convert the itemId to a name here instead of in the main loop
            // this prevents a call to the session in the RoR2Application_Update
            var itemName = session.Items.GetItemName(itemId);
            // We will keep track of the item id as well as since the name cannot be converted back to an id.

            // Separate the environments and items so that the environments can be precollected
            //  when the run starts.
            if (environmentRangeLower <= itemId && itemId <= environmentRangeUpper)
            {
                environmentReceivedQueue.Enqueue(new QueuedItem(itemId, itemName, silent));
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
                stageReceivedQueue.Enqueue(new QueuedItem(itemId, itemName, silent));
            }
            else
            {
                itemReceivedQueue.Enqueue(new KeyValuePair<long, string>(itemId, itemName));
            }

        }

        public void Dispose()
        {
            DisposeClassicHooks();
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
         * Stage unlocks are precollected for the same reason: CheckBlocked consults them as soon as a stage is picked.
         */
        public void Precollect()
        {
            while (environmentReceivedQueue.Any())
            {
                Log.LogDebug("Precollecting environment...");
                HandleReceivedEnvironmentQueueItem();
            }
            while (stageReceivedQueue.Any())
            {
                Log.LogDebug("Precollecting stage unlock...");
                HandleReceivedStageQueueItem();
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
            QueuedItem itemReceived = environmentReceivedQueue.Dequeue();

            long itemIdReceived = itemReceived.Id;
            string itemNameReceived = itemReceived.Name;
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
            if (!itemReceived.Silent && IsInGame)
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
            QueuedItem itemReceived = stageReceivedQueue.Dequeue();

            long itemIdRecieved = itemReceived.Id;
            string itemNameReceived = itemReceived.Name;
            bool announce = !itemReceived.Silent && IsInGame;
            if (itemIdRecieved == 37505)
            {
                StageBlockerHandler.amountOfStages += 1;
                if (announce)
                {
                    ChatMessage.SendColored($"Received {itemNameReceived} #{StageBlockerHandler.amountOfStages}!", Color.magenta);
                }
            } 
            else
            {
                StageBlockerHandler.stageUnlocks[itemNameReceived] = true;
                if (announce)
                {
                    ChatMessage.SendColored($"Received {itemNameReceived}!", Color.magenta);
                }
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
            inventory.GiveItemPermanent(PickupCatalog.GetPickupDef(pickupIndex)?.itemIndex ?? ItemIndex.None);
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
                    card.GetComponent<GenericPickupController>().pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.MiscPickups.LunarCoin.miscPickupIndex);
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
    }
}
