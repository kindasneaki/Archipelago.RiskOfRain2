using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Handlers;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago.RiskOfRain2
{
    //TODO: perhaps only use particular drops as fodder for item pickups (i.e. only chest drops/interactable drops) then set options based on them maybe
    public class ArchipelagoClient : IDisposable
    {
        public delegate void ClientDisconnected(string reason);
        public event ClientDisconnected OnClientDisconnect;

        public Uri LastServerUrl { get; set; }
        internal DeathLinkHandler Deathlinkhandler { get; private set; }
        internal StageBlockerHandler Stageblockerhandler { get; private set; }
        internal LocationHandler Locationhandler { get; private set; }

        public ArchipelagoItemLogicController ItemLogic;
        public ArchipelagoLocationCheckProgressBarUI itemCheckBar;
        public ArchipelagoLocationCheckProgressBarUI shrineCheckBar;

        private ArchipelagoSession session;
        private DeathLinkService deathLinkService;
        private bool finalStageDeath = true;
        private bool isEndingAcceptable = false;
        public GameObject ReleasePanel;
        public GameObject ReleasePromptPanel;
        public delegate void ReleaseClick(bool prompt);
        public static ReleaseClick OnReleaseClick;
        //public static ReleaseClick OnButtonClick;

        public ArchipelagoClient()
        {

        }

        public void Connect(Uri url, string slotName, string password = null)
        {
            if (session != null)
            {
                if(session.Socket.Connected)
                {
                    return;
                }
            }
            ChatMessage.SendColored($"Attempting to connect to Archipelago at ${url}.", Color.green);

            LastServerUrl = url;

            session = ArchipelagoSessionFactory.CreateSession(url);
            ItemLogic = new ArchipelagoItemLogicController(session);
            itemCheckBar = null;
            shrineCheckBar = null;

            var result = session.TryConnectAndLogin("Risk of Rain 2", slotName, ItemsHandlingFlags.AllItems, new Version(0, 3, 5));

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
            if (successResult.SlotData.TryGetValue("FinalStageDeath", out var stageDeathObject))
            {
                finalStageDeath = Convert.ToBoolean(stageDeathObject);
                ChatMessage.SendColored("Connected!", Color.green);
            }

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

            if (successResult.SlotData.TryGetValue("EnvironmentsAsItems", out var enableBlocker))
            {
                // block the stages if they are expected to be recieved as items
                if (Convert.ToBoolean(enableBlocker))
                {
                    Stageblockerhandler = new StageBlockerHandler();
                    ItemLogic.Stageblockerhandler = Stageblockerhandler;
                    Stageblockerhandler.BlockAll();
                }
            }

            if (successResult.SlotData.TryGetValue("DeathLink", out var enabledeathlink))
            {
                if (Convert.ToBoolean(enabledeathlink))
                {
                    Log.LogDebug("Starting DeathLink service");
                    deathLinkService = DeathLinkProvider.CreateDeathLinkService(session);
                    deathLinkService.EnableDeathLink(); // deathlink should just be enabled, the DeathLinkHandler assumes it is already enabled
                    Deathlinkhandler = new DeathLinkHandler(deathLinkService);
                }
            }

            if (successResult.SlotData.TryGetValue("classic_mode", out var classicmode))
            {
                if (Convert.ToBoolean(classicmode))
                {
                    Log.LogDebug("Client detected classic_mode");
                    // classic mode startup is handled within ArchipelagoItemLogicController.Session_PacketReceived
                }
                else
                {
                    Log.LogDebug("Client detected explore_mode");
                    // only start the new location handler for explore mode
                    Locationhandler = new LocationHandler(session, LocationHandler.buildTemplateFromSlotData(successResult.SlotData));

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
            // make the bar if for it has not been created because classic mode or the slot data was missing
            if (null == itemCheckBar)
            {
                Log.LogDebug("Setting up bar for classic");
                itemCheckBar = new ArchipelagoLocationCheckProgressBarUI(Vector2.zero, Vector2.zero);
                SyncLocationCheckProgress.OnLocationSynced += itemCheckBar.UpdateCheckProgress; // the item bar updates from the netcode in classic mode
            }

            itemCheckBar.ItemPickupStep = (int)itemPickupStep;

            session.Socket.PacketReceived += Session_PacketReceived;
            session.Socket.SocketClosed += Session_SocketClosed;
            ItemLogic.OnItemDropProcessed += ItemLogicHandler_ItemDropProcessed;

            HookGame();
            new ArchipelagoStartMessage().Send(NetworkDestination.Clients);

            // TODO Precollecting needs to happen earlier to be actually effective...
            // Connect() is called in is within Run.onRunStartGlobal().
            // Run.Start() calls onRunStartGlobal().
            // Precolect needs to happen before Start() since that is when the first stage's environment is picked.
            // Actually doing this would take a networking rewrite; there is no effective workaround.
            ItemLogic.Precollect();
        }

        public void Dispose()
        {
            if (session != null && session.Socket.Connected)
            {
                Log.LogDebug("dispose called");
                //breaks
                //session.Socket.Disconnect();
                //works
                session.Socket.DisconnectAsync();
            }
            
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
            /*On.RoR2.UI.GameEndReportPanelController.Awake += GameEndReportPanelController_Awake;
            OnReleaseClick += WillRelease;*/

            Deathlinkhandler?.Hook();
            Stageblockerhandler?.Hook();
            Locationhandler?.Hook();
        }

        private void UnhookGame()
        {
            On.RoR2.UI.ChatBox.SubmitChat -= ChatBox_SubmitChat;
            RoR2.Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            On.RoR2.Run.BeginGameOver -= Run_BeginGameOver;
            ArchipelagoChatMessage.OnChatReceivedFromClient -= ArchipelagoChatMessage_OnChatReceivedFromClient;

            Deathlinkhandler?.UnHook();
            Stageblockerhandler?.UnHook();
            Locationhandler?.UnHook();
        }

        private void ArchipelagoChatMessage_OnChatReceivedFromClient(string message)
        {
            if (session.Socket.Connected && !string.IsNullOrEmpty(message))
            {
                var sayPacket = new SayPacket();
                sayPacket.Text = message;
                session.Socket.SendPacket(sayPacket);
            }
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
                session.Socket.SendPacket(sayPacket);

                self.inputField.text = string.Empty;
                orig(self);
            }
            else
            {
                orig(self);
            }
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

        private async void Session_PacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.Print:
                    {
                        var printPacket = packet as PrintPacket;
                        ChatMessage.Send(printPacket.Text);
                        break;
                    }
                case ArchipelagoPacketType.PrintJSON:
                    {
                        var printJsonPacket = packet as PrintJsonPacket;
                        string text = "";
                        //text = await AsynChat(printJsonPacket);
                        //Task<string> task = AsynChat(printJsonPacket);
                        //string text = await task;

                        foreach (var part in printJsonPacket.Data)
                        {
                            switch (part.Type)
                            {
                                case JsonMessagePartType.PlayerId:
                                    {
                                        //TODO check Player to see if its self
                                        int playerId = int.Parse(part.Text);
                                        if (playerId == session.ConnectionInfo.Slot)
                                        {
                                            text += "<color=#cb42f5>" + session.Players.GetPlayerName(playerId) + "</color>";
                                        }
                                        else
                                        {
                                            text += "<color=#3268a8>" + session.Players.GetPlayerName(playerId) + "</color>";
                                        }

                                        break;
                                    }
                                case JsonMessagePartType.ItemId:
                                    {
                                        int itemId = int.Parse(part.Text);
                                        text += session.Items.GetItemName(itemId);
                                        break;
                                    }
                                case JsonMessagePartType.LocationId:
                                    {
                                        int locationId = int.Parse(part.Text);
                                        text += session.Locations.GetLocationNameFromId(locationId);
                                        break;
                                    }
                                default:
                                    {
                                        text += part.Text;
                                        break;
                                    }
                            }
                        }
                        ChatMessage.Send(text);
                        //ChatMessage.SendColored(text, Color.cyan);
                        break;
                    }
            }
        }
        //Async chat to fix lag on someone releasing items.. has a bug where it combines sections of other checks together for some reason
        /*private async Task<string> AsynChat(PrintJsonPacket printJsonPacket)
        {
            string text = "";
            await Task.Run(() =>
            {
                Log.LogDebug("PrintJSON");
                foreach (var part in printJsonPacket.Data)
                {
                    switch (part.Type)
                    {
                        case JsonMessagePartType.PlayerId:
                            {
                                //TODO check Player to see if its self
                                int playerId = int.Parse(part.Text);
                                if (playerId == session.ConnectionInfo.Slot)
                                {
                                    text += "<color=#cb42f5>" + session.Players.GetPlayerName(playerId) + "</color>";
                                }
                                else
                                {
                                    text += "<color=#3268a8>" + session.Players.GetPlayerName(playerId) + "</color>";
                                }

                                break;
                            }
                        case JsonMessagePartType.ItemId:
                            {
                                int itemId = int.Parse(part.Text);
                                text += session.Items.GetItemName(itemId);
                                break;
                            }
                        case JsonMessagePartType.LocationId:
                            {
                                int locationId = int.Parse(part.Text);
                                text += session.Locations.GetLocationNameFromId(locationId);
                                break;
                            }
                        default:
                            {
                                text += part.Text;
                                break;
                            }
                    }
                    
                }
                Task.Delay(10).Wait();
            });
            return text;
        }*/
        private void Run_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            // If ending is acceptable, finish the archipelago run.
            if (IsEndingAcceptable(gameEndingDef))
            {  
                isEndingAcceptable = true;
                // Auto-complete all remaining locations. Substitute for deprecated forced_auto_forfeit.
                //session.Locations.CompleteLocationChecks(session.Locations.AllMissingLocations.ToArray());
             
                var packet = new StatusUpdatePacket();
                packet.Status = ArchipelagoClientState.ClientGoal;
                session.Socket.SendPacket(packet);

                new ArchipelagoEndMessage().Send(NetworkDestination.Clients);
            }
            orig(self, gameEndingDef);
        }

        private bool IsEndingAcceptable(GameEndingDef gameEndingDef)
        {
            // Acceptable ending types
            var acceptableEndings = new[] { 
                RoR2Content.GameEndings.MainEnding, 
                RoR2Content.GameEndings.ObliterationEnding, 
                RoR2Content.GameEndings.LimboEnding, 
                DLC1Content.GameEndings.VoidEnding 
            };

            // Acceptable stages to die on
            var acceptableLosses = new[]
            {
                "moon",
                "moon2",
                "voidraid"
            };

            return acceptableEndings.Contains(gameEndingDef) 
                  ||(finalStageDeath 
                     && gameEndingDef == RoR2Content.GameEndings.StandardLoss 
                     && acceptableLosses.Contains(Stage.instance.sceneDef.baseSceneName)
                    );
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            Dispose();
        }
        //Prompt to release items instead of auto release.. Causes a bug where you get stuck in game and cant click continue
        /*private void GameEndReportPanelController_Awake(On.RoR2.UI.GameEndReportPanelController.orig_Awake orig, GameEndReportPanelController self)
        {
            if (isEndingAcceptable && ReleasePromptPanel == null)
            {
                var rp = GameObject.Instantiate(ReleasePanel);
                var gameEndReportPanel = self.transform.Find("SafeArea (JUICED)/BodyArea");
                Log.LogDebug(self.transform);
                rp.transform.SetParent(gameEndReportPanel.transform, false);
                rp.transform.localPosition = new Vector3(0, 0, 0);
                rp.transform.localScale = Vector3.one;
                var release = self.transform.Find("SafeArea (JUICED)/BodyArea/ReleasePrompt(Clone)/Panel/Release/").gameObject;
                release.AddComponent<HGButton>();
                var cancel = self.transform.Find("SafeArea (JUICED)/BodyArea/ReleasePrompt(Clone)/Panel/Cancel/").gameObject;
                cancel.AddComponent<HGButton>();
                release.GetComponent<HGButton>().onClick.AddListener(() => { OnReleaseClick(true); });
                cancel.GetComponent<HGButton>().onClick.AddListener(() => { OnReleaseClick(false); });
                ReleasePromptPanel = self.transform.Find("SafeArea (JUICED)/BodyArea/ReleasePrompt(Clone)").gameObject;
            }
            orig(self);
        }
        private void WillRelease(bool prompt)
        {
            if (prompt && isEndingAcceptable)
            {
                Log.LogDebug($"Releasing the rest of the items {isEndingAcceptable}");
                session.Locations.CompleteLocationChecks(session.Locations.AllMissingLocations.ToArray());
            }
            ReleasePromptPanel.SetActive(false);
        }*/
    }
}
