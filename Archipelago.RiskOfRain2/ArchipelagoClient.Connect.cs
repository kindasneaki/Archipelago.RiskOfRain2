using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.RiskOfRain2.Handlers;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Archipelago.RiskOfRain2
{
    public partial class ArchipelagoClient
    {
        public void Connect(string url, string slotName, string password = null)
        {
            if (session != null)
            {
                if (session.Socket.Connected)
                {
                    Disconnect();
                    return;
                }
            }
            isEndingAcceptable = false;
            ChatMessage.SendColored($"Attempting to connect to Archipelago at {url}.", Color.green);

            lastServerUrl = url;
            lastSlotName = slotName;
            lastPassword = password;
            try
            {
                session = ArchipelagoSessionFactory.CreateSession(url);
            }
            catch (Exception e)
            {
                OnClientDisconnect(e.Message);
            }
            ItemLogic = new ArchipelagoItemLogicController(session);
            itemCheckBar = null;
            shrineCheckBar = null;
            if (!isInGame)
            {
                lastReceivedItemindex = 0;
            }
            var result = session.TryConnectAndLogin("Risk of Rain 2", slotName, ItemsHandlingFlags.AllItems, new Version(0, 6, 4), password: password);

            if (!result.Successful)
            {
                LoginFailure failureResult = (LoginFailure)result;
                foreach (var err in failureResult.Errors)
                {
                    ChatMessage.SendColored(err, Color.red);
                    Log.LogError(err);
                }
                Dispose();
                return;
            }

            LoginSuccessful successResult = (LoginSuccessful)result;
            ArchipelagoConnectButtonController.ChangeButtonWhenConnected();
            // Final Stage Death
            if (successResult.SlotData.TryGetValue("finalStageDeath", out var stageDeathObject))
            {
                finalStageDeath = Convert.ToBoolean(stageDeathObject);
                ChatMessage.SendColored("Connected!", Color.green);
            }
            Log.LogDebug($"finalStageDeath {finalStageDeath} ");

            // Pickup Steps
            uint itemPickupStep = 3;
            uint shrineUseStep = 3;
            if (successResult.SlotData.TryGetValue("itemPickupStep", out var oitemPickupStep))
            {
                itemPickupStep = Convert.ToUInt32(oitemPickupStep);
                Log.LogDebug($"itemPickupStep from slot data: {itemPickupStep}");
                itemPickupStep++; // Add 1 because the user's YAML will contain a value equal to "number of pickups before sent location"
            }
            if (successResult.SlotData.TryGetValue("shrineUseStep", out var oshrineUseStep))
            {
                shrineUseStep = Convert.ToUInt32(oshrineUseStep);
                Log.LogDebug($"shrineUseStep from slot data: {shrineUseStep}");
                shrineUseStep++; // Add 1 because the user's YAML will contain a value equal to "number of pickups before sent location"
            }

            // Deathlink
            deathLinkService = DeathLinkProvider.CreateDeathLinkService(session);
            Log.LogDebug("Starting DeathLink service");
            Deathlinkhandler = new DeathLinkHandler(deathLinkService);
            if (successResult.SlotData.TryGetValue("deathLink", out var enabledeathlink))
            {

                if (Convert.ToBoolean(enabledeathlink))
                {
                    deathLinkService.EnableDeathLink(); // deathlink should just be enabled, the DeathLinkHandler assumes it is already enabled
                    Deathlinkhandler?.Hook();
                }

            }

            // Seed DLC's
            if (successResult.SlotData.TryGetValue("dlcSotv", out var sotv))
            {
                seedHasSOTV = Convert.ToBoolean(sotv);
                Log.LogDebug($"Seed has Survivors: {seedHasSOTV}");
            }
            if (successResult.SlotData.TryGetValue("dlcSots", out var sots))
            {
                seedHasSOTS = Convert.ToBoolean(sots);
                Log.LogDebug($"Seed has Seekers: {seedHasSOTS}");
            }
            if (successResult.SlotData.TryGetValue("dlcAlloyed", out var alloyed))
            {
                seedHasALLOYED = Convert.ToBoolean(alloyed);
                Log.LogDebug($"Seed has Alloyed: {seedHasALLOYED}");
            }

            // Classic Mode vs Explore Mode
            if (successResult.SlotData.TryGetValue("goal", out var classicmode))
            {
                if (!Convert.ToBoolean(classicmode))
                {
                    Log.LogDebug("Client detected classic_mode");
                    ArchipelagoLocationsInEnvironmentController.RemoveObjective();
                    new AllChecksCompleteInStage().Send(NetworkDestination.Clients);
                    // classic mode startup is handled within ArchipelagoItemLogicController.Session_PacketReceived
                }
                else
                {
                    Log.LogDebug("Client detected explore_mode");
                    // only start the new location handler for explore mode
                    Stageblockerhandler = new StageBlockerHandler();
                    ItemLogic.Stageblockerhandler = Stageblockerhandler;
                    Stageblockerhandler.BlockAll();
                    Locationhandler = new LocationHandler(session, LocationHandler.buildTemplateFromSlotData(successResult.SlotData));
                    Stageblockerhandler.Locationhandler = Locationhandler;
                    shrineChanceHelper = new ShrineChanceHandler();

                    // TODO there is a more likely a more reasonable location to create the UI for explore mode
                    itemCheckBar = new ArchipelagoLocationCheckProgressBarUI(new Vector2(-40, 0), Vector2.zero, "Item Check Progress:");

                    shrineCheckBar = new ArchipelagoLocationCheckProgressBarUI(new Vector2(0, 170), new Vector2(50, -50), "Shrine Check Progress:");

                    shrineCheckBar.ItemPickupStep = (int)shrineUseStep;

                    Locationhandler.itemBar = itemCheckBar;
                    Locationhandler.shrineBar = shrineCheckBar;
                    Locationhandler.itemPickupStep = itemPickupStep;
                    Locationhandler.shrineUseStep = shrineUseStep;
                }
            }

            // Progressive Stages
            // These are static, so they must be defaulted rather than left holding the previous seed's value
            // when the slot data omits the key.
            StageBlockerHandler.progressivesStages = true;
            if (successResult.SlotData.TryGetValue("progressiveStages", out var progressive))
            {
                StageBlockerHandler.progressivesStages = Convert.ToBoolean(progressive);
            }

            // Show Seer Portals
            StageBlockerHandler.showSeerPortals = false;
            if (successResult.SlotData.TryGetValue("showSeerPortals", out var showSeerPortals))
            {
                StageBlockerHandler.showSeerPortals = Convert.ToBoolean(showSeerPortals);
            }

            // Victory Condition
            ParseVictoryCondition(successResult.SlotData);

            // make the bar if for it has not been created because classic mode or the slot data was missing
            if (null == itemCheckBar)
            {
                Log.LogDebug("Setting up bar for classic");
                itemCheckBar = new ArchipelagoLocationCheckProgressBarUI(Vector2.zero, Vector2.zero);
                SyncLocationCheckProgress.OnLocationSynced += itemCheckBar.UpdateCheckProgress; // the item bar updates from the netcode in classic mode
            }
            connectedPlayerName = session.Players.GetPlayerName(session.ConnectionInfo.Slot);
            itemCheckBar.ItemPickupStep = (int)itemPickupStep;

            session.MessageLog.OnMessageReceived += Session_OnMessageReceived;
            session.Socket.SocketClosed += Session_SocketClosed;
            ItemLogic.OnItemDropProcessed += ItemLogicHandler_ItemDropProcessed;
            genericMenuButton = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/GenericMenuButton.prefab").WaitForCompletion();
            HookGame();
            new ArchipelagoStartMessage().Send(NetworkDestination.Clients);
            if (!Convert.ToBoolean(classicmode))
            {
                new ArchipelagoStartClassic().Send(NetworkDestination.Clients);
            }
            else
            {
                new ArchipelagoStartExplore().Send(NetworkDestination.Clients);
            }

            StageBlockerHandler.ResetStageProgression();

            // Needed for backwards compatability
            if (session.Items.GetItemName(37501) == null)
            {
                StageBlockerHandler.stageUnlocks["Stage 1"] = true;
                StageBlockerHandler.stageUnlocks["Stage 2"] = true;
                StageBlockerHandler.stageUnlocks["Stage 3"] = true;
                StageBlockerHandler.stageUnlocks["Stage 4"] = true;
            }

            ItemLogic.Precollect();
        }
    }
}
