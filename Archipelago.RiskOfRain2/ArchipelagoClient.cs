using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Console;
using Archipelago.RiskOfRain2.Handlers;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Archipelago.RiskOfRain2
{
    //TODO: perhaps only use particular drops as fodder for item pickups (i.e. only chest drops/interactable drops) then set options based on them maybe
    public partial class ArchipelagoClient : IDisposable
    {
        public delegate void ClientDisconnected(string reason);
        public event ClientDisconnected OnClientDisconnect;

        public string lastServerUrl { get; set; }
        public string lastSlotName { get; set; }
        public string lastPassword { get; set; }
        internal DeathLinkHandler Deathlinkhandler { get; private set; }
        internal StageBlockerHandler Stageblockerhandler { get; private set; }
        internal LocationHandler Locationhandler { get; private set; }
        internal ShrineChanceHandler shrineChanceHelper { get; private set; }

        public ArchipelagoItemLogicController ItemLogic;
        public ArchipelagoLocationCheckProgressBarUI itemCheckBar;
        public ArchipelagoLocationCheckProgressBarUI shrineCheckBar;

        private ArchipelagoSession session;
        private DeathLinkService deathLinkService;
        public bool reconnecting { get; set; } = false;
        public static int lastReceivedItemindex { get; set; } = 0;
        public static bool isInGame { get; set; } = false;
        //public static ReleaseClick OnButtonClick;
        public static string connectedPlayerName;

        public static bool seedHasSOTV { get; set; } = false;
        public static bool seedHasSOTS { get; set; } = false;
        public static bool seedHasALLOYED { get; set; } = false;

        public ArchipelagoClient()
        {

        }

        public void Dispose()
        {
            if (ItemLogic != null)
            {
                ItemLogic.OnItemDropProcessed -= ItemLogicHandler_ItemDropProcessed;
                ItemLogic.Dispose();
            }

            if (itemCheckBar != null)
            {
                SyncLocationCheckProgress.OnLocationSynced -= itemCheckBar.UpdateCheckProgress;
                itemCheckBar.Dispose();
            }

            if (shrineCheckBar != null)
            {
                shrineCheckBar.Dispose();
            }

            UnhookGame();
            session = null;

            // In the case the player joins a lobby that uses different settings, the previous objects may still exist and may be called again when hooks are started.
            // To prevent this, the old objects will be thrown away when disposing.
            Stageblockerhandler = null;
            Locationhandler = null;
            itemCheckBar = null;
            shrineCheckBar = null;
        }

        private void HookGame()
        {
            On.RoR2.UI.ChatBox.SubmitChat += ChatBox_SubmitChat;
            RoR2.Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            On.RoR2.Run.BeginGameOver += Run_BeginGameOver;
            ArchipelagoChatMessage.OnChatReceivedFromClient += ArchipelagoChatMessage_OnChatReceivedFromClient;
            ReleasePanel = AssetBundleHelper.LoadPrefab("ReleasePrompt");
            CollectPanel = AssetBundleHelper.LoadPrefab("CollectPrompt");
            On.RoR2.UI.GameEndReportPanelController.Awake += GameEndReportPanelController_Awake;
            OnReleaseClick += WillRelease;
            OnCollectClick += WillCollect;
            On.RoR2.SceneObjectToggleGroup.Awake += SceneObjectToggleGroup_Awake;

            Stageblockerhandler?.Hook();
            Locationhandler?.Hook();
            shrineChanceHelper?.Hook();
            ArchipelagoConsoleCommand.OnArchipelagoDeathLinkCommandCalled += ArchipelagoConsoleCommand_OnArchipelagoDeathLinkCommandCalled;
            ArchipelagoConsoleCommand.OnArchipelagoFinalStageDeathCommandCalled += ArchipelagoConsoleCommand_OnArchipelagoFinalStageDeathCommandCalled;
            ArchipelagoConsoleCommand.OnArchipelagoReconnectCommandCalled += ArchipelagoConsoleCommand_OnArchipelagoReconnectCommandCalled;
            session.Socket.ErrorReceived += Socket_ErrorReceived;
            On.RoR2.PortalDialerController.PortalDialerPreDialState.OnEnter += PortalDialerPreDialState_OnEnter;
            //On.RoR2.PortalDialerController.PortalDialerIdleState.OnActivationServer += PortalDialerIdleState_OnActivationServer;
            //On.RoR2.PortalDialerButtonController.onActivation
        }

        private void UnhookGame()
        {
            On.RoR2.UI.ChatBox.SubmitChat -= ChatBox_SubmitChat;
            RoR2.Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            On.RoR2.Run.BeginGameOver -= Run_BeginGameOver;
            ArchipelagoChatMessage.OnChatReceivedFromClient -= ArchipelagoChatMessage_OnChatReceivedFromClient;
            session.MessageLog.OnMessageReceived -= Session_OnMessageReceived;
            session.Socket.SocketClosed -= Session_SocketClosed;
            On.RoR2.UI.GameEndReportPanelController.Awake -= GameEndReportPanelController_Awake;
            OnReleaseClick -= WillRelease;
            OnCollectClick -= WillCollect;
            On.RoR2.SceneObjectToggleGroup.Awake -= SceneObjectToggleGroup_Awake;


            Deathlinkhandler?.UnHook();
            Stageblockerhandler?.UnHook();
            Locationhandler?.UnHook();
            shrineChanceHelper?.UnHook();
            ArchipelagoConsoleCommand.OnArchipelagoDeathLinkCommandCalled -= ArchipelagoConsoleCommand_OnArchipelagoDeathLinkCommandCalled;
            ArchipelagoConsoleCommand.OnArchipelagoFinalStageDeathCommandCalled -= ArchipelagoConsoleCommand_OnArchipelagoFinalStageDeathCommandCalled;
            session.Socket.ErrorReceived -= Socket_ErrorReceived;
            On.RoR2.PortalDialerController.PortalDialerPreDialState.OnEnter -= PortalDialerPreDialState_OnEnter;

        }
        private void SceneObjectToggleGroup_Awake(On.RoR2.SceneObjectToggleGroup.orig_Awake orig, SceneObjectToggleGroup self)
        {
            Log.LogDebug($"Scene group length {self.toggleGroups.Length}");
            if (self.toggleGroups != null)
            {
                for (var i = 0; i < self.toggleGroups.Length; i++)
                {
                    if (self.toggleGroups[i].objects != null && self.toggleGroups[i].objects[0] != null)
                    {
                        if (self.toggleGroups[i].objects[0].name == "NewtStatue" || self.toggleGroups[i].objects[0].name == "NewtStatue (1)")
                        {
                            Log.LogDebug($"Scene Object Toggle Group min:{self.toggleGroups[i].minEnabled} max:{self.toggleGroups[i].maxEnabled}");
                            Log.LogDebug("Changing newt alters min and max values");
                            self.toggleGroups[i].minEnabled = 1;
                            self.toggleGroups[i].maxEnabled = 2;
                            Log.LogDebug($"Scene Object Toggle Group  min:{self.toggleGroups[i].minEnabled} max:{self.toggleGroups[i].maxEnabled}");
                            break;
                        }
                    }

                }
            }
            orig(self);



        }
        private void ArchipelagoConsoleCommand_OnArchipelagoDeathLinkCommandCalled(bool link)
        {
            if (link)
            {
                Deathlinkhandler?.Hook();
                deathLinkService.EnableDeathLink();
            }
            else
            {
                Deathlinkhandler?.UnHook();
                deathLinkService.DisableDeathLink();
            }
        }
        private void ArchipelagoConsoleCommand_OnArchipelagoFinalStageDeathCommandCalled(bool finalstage)
        {
            finalStageDeath = finalstage;
        }
        private void ArchipelagoChatMessage_OnChatReceivedFromClient(string message)
        {
            if (session.Socket.Connected && !string.IsNullOrEmpty(message))
            {
                var sayPacket = new SayPacket();
                sayPacket.Text = message;
                session.Socket.SendPacketAsync(sayPacket);
            }
        }

        private void ArchipelagoConsoleCommand_OnArchipelagoReconnectCommandCalled()
        {
            reconnecting = true;
            Session_SocketClosed("Making sure to be disconnected before reconnecting.");
        }

        private void ItemLogicHandler_ItemDropProcessed(int pickedUpCount)
        {
            if (itemCheckBar != null)
            {
                itemCheckBar.CurrentItemCount = pickedUpCount;
                if ((itemCheckBar.CurrentItemCount % ItemLogic.ItemPickupStep) == 0)
                {
                    itemCheckBar.CurrentItemCount = 0;
                }
                else
                {
                    itemCheckBar.CurrentItemCount = itemCheckBar.CurrentItemCount % ItemLogic.ItemPickupStep;
                }
            }
            new SyncLocationCheckProgress(itemCheckBar.CurrentItemCount, itemCheckBar.ItemPickupStep).Send(NetworkDestination.Clients);
        }

        private void ChatBox_SubmitChat(On.RoR2.UI.ChatBox.orig_SubmitChat orig, ChatBox self)
        {
            var text = self.inputField.text;
            if (session.Socket.Connected && !string.IsNullOrEmpty(text))
            {
                var sayPacket = new SayPacket();
                sayPacket.Text = text;
                session.Socket.SendPacketAsync(sayPacket);

                self.inputField.text = string.Empty;
                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        private void Socket_ErrorReceived(Exception e, string message)
        {
            Log.LogDebug($"Error received: {e}, message: {message}");
            reconnecting = true;
            Session_SocketClosed(message);
        }

        private void Session_SocketClosed(string reason)
        {
            Dispose();
            new ArchipelagoEndMessage().Send(NetworkDestination.Clients);

            if (OnClientDisconnect != null)
            {
                OnClientDisconnect(reason);
            }
        }

        //public IEnumerator AttemptConnection()
        //{
        //    reconnecting = true;
        //    var retryCounter = 0;

        //    while ((session == null || !session.Socket.Connected)&& retryCounter < 5)
        //    {
        //        ChatMessage.Send($"Connection attempt #{retryCounter+1}");
        //        retryCounter++;
        //        yield return new WaitForSeconds(3f);
        //        Connect(LastServerUrl, connectPacket.Name, connectPacket.Password);
        //    }

        //    if (session == null || !session.Socket.Connected)
        //    {
        //        ChatMessage.SendColored("Could not connect to Archipelago.", Color.red);
        //        Dispose();
        //    }
        //    else if (session != null && session.Socket.Connected)
        //    {
        //        ChatMessage.SendColored("Established Archipelago connection.", Color.green);
        //        new ArchipelagoStartMessage().Send(NetworkDestination.Clients);
        //    }

        //    reconnecting = false;
        //    RecentlyReconnected = true;
        //}

        public IEnumerator<WaitForSeconds> AttemptReconnection()
        {
            Log.LogDebug("Attempting to reconnect!");
            var retryCounter = 0;
            if (!isInGame)
            {
                ArchipelagoConnectButtonController.ChangeButtonWhenDisconnected();
            }
            while ((session == null || !session.Socket.Connected)&& retryCounter < 5)
            {
                ChatMessage.Send($"Connection attempt #{retryCounter+1}");
                retryCounter++;
                yield return new WaitForSeconds(3f);
                Connect(lastServerUrl, lastSlotName, lastPassword);
            }

            if (session == null || !session.Socket.Connected)
            {
                ChatMessage.SendColored("Could not connect to Archipelago.", Color.red);
                Dispose();
            }
            else if (session != null && session.Socket.Connected)
            {
                ChatMessage.SendColored("Established Archipelago connection.", Color.green);
                new ArchipelagoStartMessage().Send(NetworkDestination.Clients);
                if (Locationhandler != null && isInGame)
                {
                    Locationhandler.CatchUpSceneLocations(LocationHandler.sceneDef.cachedName);
                    Locationhandler.LoadItemPickupHooks();
                }
            }

            reconnecting = false;
        }
        private void Session_OnMessageReceived(LogMessage message)
        {
            Thread thread = new Thread(() => Session_OnMessageReceived_Thread(message));
            thread.Start();
            Thread.Sleep(20);
        }
        private void Session_OnMessageReceived_Thread(LogMessage message)
        {
            string text = "";
            foreach (var part in message.Parts)
            {
                var hex = part.Color.R.ToString("X2") + part.Color.G.ToString("X2") + part.Color.B.ToString("X2");
                text += $"<color=#{hex}>" + part + "</color>";
            }

            ChatMessage.Send(text);
        }
        // When exiting to menu/game this will run
        private void Run_onRunDestroyGlobal(Run obj)
        {
            isInGame = false;
            lastReceivedItemindex = 0;
            Disconnect();
        }

        public void Disconnect()
        {
            if (session != null && session.Socket.Connected)
            {
                ArchipelagoConnectButtonController.ChangeButtonWhenDisconnected();
                session.Socket.DisconnectAsync();
            }
        }
    }
}
