using System;
using System.Collections.Generic;
using Archipelago.RiskOfRain2.Console;
using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using Archipelago.RiskOfRain2.Handlers;
using BepInEx;
using BepInEx.Bootstrap;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    //[BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.HardDependency)]
    [R2APISubmoduleDependency(nameof(NetworkingAPI), nameof(PrefabAPI), nameof(CommandHelper))]
    public class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.Ijwu.Archipelago";
        public const string PluginAuthor = "Ijwu/Sneaki";
        public const string PluginName = "Archipelago";
        public const string PluginVersion = "1.3.2";
        internal static ArchipelagoPlugin Instance { get; private set; }
        //public string bundleName = "connectbundle";
        //public static AssetBundle localAssetBundle { get; private set; }

        private ArchipelagoClient AP;
        private ClientItemsHandler ClientItems;
        //private bool isInLobbyConfigLoaded = false;
        internal static string apServerUri = "archipelago.gg";
        internal static int apServerPort = 38281;
        private bool willConnectToAP = true;
        private bool isPlayingAP = false;
        internal static string apSlotName = "";
        //private string apSlotName;
        internal static string apPassword;

        public ArchipelagoPlugin()
        {

        }
        public void Awake()
        {
            Log.Init(Logger);
            Instance = this;
            AP = new ArchipelagoClient();
            ArchipelagoConnectButtonController.OnConnectClick += OnClick_ConnectToArchipelagoWithButton;
            AP.OnClientDisconnect += AP_OnClientDisconnect;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            ArchipelagoStartMessage.OnArchipelagoSessionStart += ArchipelagoStartMessage_OnArchipelagoSessionStart;
            ArchipelagoEndMessage.OnArchipelagoSessionEnd += ArchipelagoEndMessage_OnArchipelagoSessionEnd;
            ArchipelagoConsoleCommand.OnArchipelagoCommandCalled += ArchipelagoConsoleCommand_ArchipelagoCommandCalled;
            ArchipelagoConsoleCommand.OnArchipelagoDisconnectCommandCalled += ArchipelagoConsoleCommand_ArchipelagoDisconnectCommandCalled;
            NetworkManagerSystem.onStopClientGlobal += GameNetworkManager_onStopClientGlobal;
            On.RoR2.UI.ChatBox.SubmitChat += ChatBox_SubmitChat;
            AssetBundleHelper.LoadBundle();         

            CreateLobbyFields();

            NetworkingAPI.RegisterMessageType<SyncLocationCheckProgress>();
            NetworkingAPI.RegisterMessageType<ArchipelagoStartMessage>();
            NetworkingAPI.RegisterMessageType<ArchipelagoEndMessage>();
            NetworkingAPI.RegisterMessageType<SyncTotalCheckProgress>();
            NetworkingAPI.RegisterMessageType<AllChecksComplete>();
            NetworkingAPI.RegisterMessageType<AllChecksCompleteInStage>();
            NetworkingAPI.RegisterMessageType<ArchipelagoChatMessage>();
            NetworkingAPI.RegisterMessageType<SyncCurrentEnvironmentCheckProgress>();
            NetworkingAPI.RegisterMessageType<NextStageObjectives>();
            NetworkingAPI.RegisterMessageType<ArchipelagoTeleportClient>();
            NetworkingAPI.RegisterMessageType<SyncShrineCheckProgress>();
            NetworkingAPI.RegisterMessageType<ArchipelagoStartExplore>();

            CommandHelper.AddToConsoleWhenReady();
        }

        public void Start()
        {
            var connectButton = new GameObject("ArchipelagoConnectButtonController");
            connectButton.AddComponent<ArchipelagoConnectButtonController>();
            
            
        }

        private void GameNetworkManager_onStopClientGlobal()
        {
            if (!NetworkServer.active && isPlayingAP)
            {
                if (AP.itemCheckBar != null)
                {
                    AP.itemCheckBar.Dispose();
                }
            }
        }

        private void ChatBox_SubmitChat(On.RoR2.UI.ChatBox.orig_SubmitChat orig, RoR2.UI.ChatBox self)
        {
            if (!NetworkServer.active && isPlayingAP)
            {
                new ArchipelagoChatMessage(self.inputField.text).Send(NetworkDestination.Server);
                self.inputField.text = "";
                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        private void ArchipelagoEndMessage_OnArchipelagoSessionEnd()
        {
            // This is for clients that are in a lobby but not the host of the lobby.
            // They end up with multiple bars if they join multiple sessions otherwise.
            if (!NetworkServer.active && isPlayingAP)
            {
                if (AP.itemCheckBar != null)
                {
                    AP.itemCheckBar.Dispose();
                }
            }
        }

        private void AP_OnClientDisconnect(string reason)
        {
            Log.LogWarning($"Archipelago client was disconnected from the server because `{reason}`");
            ChatMessage.SendColored($"Archipelago client was disconnected from the server. {reason}", Color.red);
            var isHost = NetworkServer.active && RoR2Application.isInMultiPlayer;
            if (isPlayingAP && (isHost || RoR2Application.isInSinglePlayer))
            {
                //StartCoroutine(AP.AttemptConnection());
            }
        }
        public void OnClick_ConnectToArchipelagoWithButton()
        {
            
            isPlayingAP = true;
            string url = apServerUri + ":" + apServerPort;

            Log.LogDebug($"Server {apServerUri} Port: {apServerPort} Slot: {apSlotName} Password: {apPassword}");

            AP.Connect(url, apSlotName, apPassword);
            //Log.LogDebug("On Click Connect");
        }
        private void ArchipelagoConsoleCommand_ArchipelagoCommandCalled(string url, int port, string slot, string password)
        {
            willConnectToAP = true;
            isPlayingAP = true;
            url = url + ":" + port;

            AP.Connect(url, slot, password);
            //StartCoroutine(AP.AttemptConnection());
        }
        private void ArchipelagoConsoleCommand_ArchipelagoDisconnectCommandCalled()
        {
            AP.Disconnect();
        }
        /// <summary>
        /// Server -> Client packet responder. Should not run on server.
        /// </summary>
        private void ArchipelagoStartMessage_OnArchipelagoSessionStart()
        {
            if (!NetworkServer.active)
            {
                ClientItems = new ClientItemsHandler();
                ClientItems?.Hook();
                isPlayingAP = true;
            }
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            if (isPlayingAP)
            {
                ArchipelagoTotalChecksObjectiveController.RemoveObjective();
                ArchipelagoLocationsInEnvironmentController.RemoveObjective();
            }
        }
        private void CreateLobbyFields()
        {
            ArchipelagoConnectButtonController.OnSlotChanged = (newValue) => apSlotName = newValue;
            ArchipelagoConnectButtonController.OnPasswordChanged = (newValue) => apPassword = newValue;
            ArchipelagoConnectButtonController.OnUrlChanged = (newValue) => apServerUri = newValue;
            ArchipelagoConnectButtonController.OnPortChanged = ChangePort;
        }
        private string ChangePort(string newValue)
        {
            apServerPort = int.Parse(newValue);
            return newValue;
        }
    }
}
