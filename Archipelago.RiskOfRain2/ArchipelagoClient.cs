using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class ArchipelagoClient : IDisposable
    {
        public delegate void ClientDisconnected(string reason);
        public event ClientDisconnected OnClientDisconnect;

        public Uri LastServerUrl { get; set; }
        internal DeathLinkHandler Deathlinkhandler { get; private set; }
        internal StageBlockerHandler Stageblockerhandler { get; private set; }
        internal LocationHandler Locationhandler { get; private set; }
        internal ShrineChanceHelper shrineChanceHelper { get; private set; }

        public ArchipelagoItemLogicController ItemLogic;
        public ArchipelagoLocationCheckProgressBarUI itemCheckBar;
        public ArchipelagoLocationCheckProgressBarUI shrineCheckBar;

        private ArchipelagoSession session;
        private DeathLinkService deathLinkService;
        private bool finalStageDeath = false;
        private bool isEndingAcceptable = false;
        public GameObject ReleasePanel;
        public GameObject CollectPanel;
        public GameObject ReleasePromptPanel;
        public GameObject CollectPromptPanel;
        public delegate void ReleaseClick(bool prompt);
        public static ReleaseClick OnReleaseClick;
        public delegate void CollectClick(bool prompt);
        public static CollectClick OnCollectClick;
        private GameObject genericMenuButton;
        //public static ReleaseClick OnButtonClick;
        public static string connectedPlayerName;
        public static string victoryCondition;
        // Acceptable ending types
        private GameEndingDef[] acceptableEndings;
        // Acceptable stages to die on
        private string[] acceptableLosses;

        public ArchipelagoClient()
        {

        }

        public void Connect(string url, string slotName, string password = null)
        {
            if (session != null)
            {
                if (session.Socket.Connected)
                {
                    return;
                }
            }
            isEndingAcceptable = false;
            ChatMessage.SendColored($"Attempting to connect to Archipelago at {url}.", Color.green);

            //LastServerUrl = url;

            session = ArchipelagoSessionFactory.CreateSession(url);
            ItemLogic = new ArchipelagoItemLogicController(session);
            itemCheckBar = null;
            shrineCheckBar = null;

            var result = session.TryConnectAndLogin("Risk of Rain 2", slotName, ItemsHandlingFlags.AllItems, new Version(0, 4, 3), password: password);

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
            if (successResult.SlotData.TryGetValue("finalStageDeath", out var stageDeathObject))
            {
                finalStageDeath = Convert.ToBoolean(stageDeathObject);
                ChatMessage.SendColored("Connected!", Color.green);
            }
            // to keep this setting working in previous versions of AP
            // TODO remove at ap version 3.9
            else if (successResult.SlotData.TryGetValue("FinalStageDeath", out var oldStageDeathObject))
            {
                finalStageDeath = Convert.ToBoolean(oldStageDeathObject);
                ChatMessage.SendColored("Connected!", Color.green);
            }
            Log.LogDebug($"finalStageDeath {finalStageDeath} ");

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
                    shrineChanceHelper = new ShrineChanceHelper();

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

            if (successResult.SlotData.TryGetValue("victory", out var victory))
            {
                Log.LogDebug($"Victory condition {victory}");
                victoryCondition = victory.ToString();
                    switch (victory)
                {
                    case "mithrix":
                        acceptableEndings = new[] { RoR2Content.GameEndings.MainEnding };
                        acceptableLosses = new[] { "moon", "moon2" };
                        break;
                    case "voidling":
                        acceptableEndings = new[] { DLC1Content.GameEndings.VoidEnding };
                        acceptableLosses = new[] { "voidraid" };
                        break;
                    case "limbo":
                        acceptableEndings = new[] { RoR2Content.GameEndings.LimboEnding };
                        acceptableLosses = new[] { "mysterspace" };
                        break;
                    default:
                        victoryCondition = "any";
                        acceptableEndings = new[] {
                            RoR2Content.GameEndings.MainEnding, 
                            //RoR2Content.GameEndings.ObliterationEnding, 
                            RoR2Content.GameEndings.LimboEnding,
                            DLC1Content.GameEndings.VoidEnding
                        };
                        acceptableLosses = new[] {
                            "moon",
                            "moon2",
                            "voidraid",
                            "mysterspace"
                        };
                        break;
                }
            } else
            {
                victoryCondition = "any";
                acceptableEndings = new[] {
                    RoR2Content.GameEndings.MainEnding, 
                    //RoR2Content.GameEndings.ObliterationEnding, 
                    RoR2Content.GameEndings.LimboEnding,
                    DLC1Content.GameEndings.VoidEnding
                };
                acceptableLosses = new[] {
                    "moon",
                    "moon2",
                    "voidraid",
                    "mysterspace"
                };
            }
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

            ItemLogic.Precollect();
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

        }
        private void SceneObjectToggleGroup_Awake(On.RoR2.SceneObjectToggleGroup.orig_Awake orig, SceneObjectToggleGroup self)
        {
            Log.LogDebug($"Scene group length {self.toggleGroups.Length}");
            for (var i = 0; i < self.toggleGroups.Length; i++)
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
            Log.LogDebug($"ending stage is {Stage.instance.sceneDef.baseSceneName}");
            return acceptableEndings.Contains(gameEndingDef) ||
                (finalStageDeath && gameEndingDef == RoR2Content.GameEndings.StandardLoss) && (acceptableLosses.Contains(Stage.instance.sceneDef.baseSceneName)) ||
                (finalStageDeath && gameEndingDef == RoR2Content.GameEndings.ObliterationEnding) && (acceptableLosses.Contains(Stage.instance.sceneDef.baseSceneName));
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            Disconnect();
        }

        public void Disconnect()
        {
            if (session != null && session.Socket.Connected)
            {
                session.Socket.DisconnectAsync();
            }
        }
        private void GameEndReportPanelController_Awake(On.RoR2.UI.GameEndReportPanelController.orig_Awake orig, GameEndReportPanelController self)
        {
            if (isEndingAcceptable && ReleasePromptPanel == null)
            {
                GameObject menuOutline;
                if (genericMenuButton != null)
                {
                    menuOutline = genericMenuButton.transform.Find("HoverOutline").gameObject;
                }
                else
                {
                    menuOutline = null;
                }

                var releasePermission = Convert.ToString(session.RoomState.ReleasePermissions);
                var collectPermission = Convert.ToString(session.RoomState.CollectPermissions);
                bool canRelease = (releasePermission == "Goal" || releasePermission == "Enabled");
                bool canCollect = (collectPermission == "Goal" || collectPermission == "Enabled");
                Log.LogDebug($"can release {releasePermission} can collect {collectPermission}");
                Log.LogDebug($"release? {canRelease} collect? {canCollect}");
                var gameEndReportPanel = self.transform.Find("SafeArea (JUICED)/BodyArea");
                if (canRelease)
                {
                    var rp = GameObject.Instantiate(ReleasePanel);
                    rp.transform.SetParent(gameEndReportPanel.transform, false);
                    rp.transform.localPosition = new Vector3(0, 0, 0);
                    rp.transform.localScale = Vector3.one;
                    var release = self.transform.Find("SafeArea (JUICED)/BodyArea/ReleasePrompt(Clone)/Panel/Release/").gameObject;
                    release.AddComponent<HGButton>();
                    var releaseCancel = self.transform.Find("SafeArea (JUICED)/BodyArea/ReleasePrompt(Clone)/Panel/Cancel/").gameObject;
                    releaseCancel.AddComponent<HGButton>();
                    release.GetComponent<HGButton>().onClick.AddListener(() => { OnReleaseClick(true); });
                    releaseCancel.GetComponent<HGButton>().onClick.AddListener(() => { OnReleaseClick(false); });
                    ReleasePromptPanel = self.transform.Find("SafeArea (JUICED)/BodyArea/ReleasePrompt(Clone)").gameObject;
                    // Outline for collect menu buttons

/*                    if (menuOutline != null)
                    {
                        GameObject releaseOutline = GameObject.Instantiate(menuOutline);
                        releaseOutline.transform.SetParent(release.transform, false);
                        release.GetComponent<HGButton>().imageOnHover = releaseOutline.GetComponent<Image>();
                        release.GetComponent<HGButton>().showImageOnHover = true;
                        GameObject releaseCancelOutline = GameObject.Instantiate(menuOutline);
                        releaseCancelOutline.transform.SetParent(releaseCancel.transform, false);
                        releaseCancel.GetComponent<HGButton>().imageOnHover = releaseCancelOutline.GetComponent<Image>();
                        releaseCancel.GetComponent<HGButton>().showImageOnHover = true;
                    }
*/                }
                if (canCollect)
                {
                    var cp = GameObject.Instantiate(CollectPanel);
                    cp.transform.SetParent(gameEndReportPanel.transform, false);
                    cp.transform.localPosition = new Vector3(0, 0, 0);
                    cp.transform.localScale = Vector3.one;
                    var collect = self.transform.Find("SafeArea (JUICED)/BodyArea/CollectPrompt(Clone)/Panel/Collect/").gameObject;
                    collect.AddComponent<HGButton>();
                    var collectCancel = self.transform.Find("SafeArea (JUICED)/BodyArea/CollectPrompt(Clone)/Panel/Cancel/").gameObject;
                    collectCancel.AddComponent<HGButton>();
                    collect.GetComponent<HGButton>().onClick.AddListener(() => { OnCollectClick(true); });
                    collectCancel.GetComponent<HGButton>().onClick.AddListener(() => { OnCollectClick(false); });
                    CollectPromptPanel = self.transform.Find("SafeArea (JUICED)/BodyArea/CollectPrompt(Clone)").gameObject;
                    CollectPromptPanel.SetActive(false);
                      //TODO Outline for collect menu buttons do not show up like in the release buttons.. no idea why

/*                    if (menuOutline != null)
                    {
                        GameObject collectOutline = GameObject.Instantiate(menuOutline);
                        collectOutline.transform.SetParent(collect.transform, false);
                        collect.GetComponent<HGButton>().imageOnHover = collectOutline.GetComponent<Image>();
                        collect.GetComponent<HGButton>().showImageOnHover = true;
                        GameObject collectCancelOutline = GameObject.Instantiate(menuOutline);
                        collectCancelOutline.transform.SetParent(collectCancel.transform, false);
                        collectCancel.GetComponent<HGButton>().imageOnHover = collectCancelOutline.GetComponent<Image>();
                        collectCancel.GetComponent<HGButton>().showImageOnHover = true;
                    }
*/              }
                if (canCollect && !canRelease)
                {
                    CollectPromptPanel.SetActive(true);
                }



            }
            orig(self);
        }
        private void WillRelease(bool prompt)
        {
            var sayPacket = new SayPacket();
            if (prompt && isEndingAcceptable)
            {
                Log.LogDebug($"Releasing the rest of the items {isEndingAcceptable}");
                sayPacket.Text = "!release";
                session.Socket.SendPacket(sayPacket);
            }
            ReleasePromptPanel.SetActive(false);
            if (CollectPromptPanel != null) 
            {
                CollectPromptPanel.SetActive(true);
            }
        }

        private void WillCollect(bool prompt)
        {
            var sayPacket = new SayPacket();
            if (prompt && isEndingAcceptable)
            {
                Log.LogDebug($"Collect the rest of the items {isEndingAcceptable}");
                sayPacket.Text = "!collect";
                session.Socket.SendPacket(sayPacket);
            }
            CollectPromptPanel?.SetActive(false);

        }
    }
}
