using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Net;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace Archipelago.RiskOfRain2
{
    public partial class ArchipelagoClient
    {
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
        public static string victoryCondition;
        // Acceptable ending types
        private GameEndingDef[] acceptableEndings;
        // Acceptable stages to die on
        private string[] acceptableLosses;

        private void ParseVictoryCondition(Dictionary<string, object> slotData)
        {
            if (slotData.TryGetValue("victory", out var victory))
            {
                switch (victory.ToString())
                {
                    // Mithrix
                    case "1":
                        acceptableEndings = new[] { RoR2Content.GameEndings.MainEnding };
                        acceptableLosses = new[] { "moon", "moon2" };
                        victoryCondition = "Mithrix";
                        break;
                    // Voidling
                    case "2":
                        acceptableEndings = new[] { DLC1Content.GameEndings.VoidEnding };
                        acceptableLosses = new[] { "voidraid" };
                        victoryCondition = "Voidling";
                        break;
                    // Limbo
                    case "3":
                        acceptableEndings = new[] { RoR2Content.GameEndings.LimboEnding };
                        acceptableLosses = new[] { "mysteryspace", "limbo" };
                        victoryCondition = "Limbo";
                        break;
                    case "4":
                        acceptableEndings = new[] { DLC2Content.GameEndings.RebirthEndingDef };
                        acceptableLosses = new[] { "meridian" };
                        victoryCondition = "Rebirth";
                        break;
                    case "5":
                        acceptableEndings = new[] { DLC3Content.GameEndings.DecompileEnding };
                        acceptableLosses = new[] { "solusweb" };
                        victoryCondition = "Solus Heart";
                        break;
                    default:
                        victoryCondition = "any";
                        acceptableEndings = new[] {
                            RoR2Content.GameEndings.MainEnding, 
                            //RoR2Content.GameEndings.ObliterationEnding, 
                            RoR2Content.GameEndings.LimboEnding,
                            DLC1Content.GameEndings.VoidEnding,
                            DLC2Content.GameEndings.RebirthEndingDef,
                            DLC3Content.GameEndings.DecompileEnding
                        };
                        acceptableLosses = new[] {
                            "moon",
                            "moon2",
                            "voidraid",
                            "mysteryspace",
                            "limbo",
                            "meridian",
                            "solusweb"
                        };
                        break;

                }
            }
            else
            {
                victoryCondition = "any";
                acceptableEndings = new[] {
                    RoR2Content.GameEndings.MainEnding, 
                    //RoR2Content.GameEndings.ObliterationEnding, 
                    RoR2Content.GameEndings.LimboEnding,
                    DLC1Content.GameEndings.VoidEnding,
                    DLC2Content.GameEndings.RebirthEndingDef,
                    DLC3Content.GameEndings.DecompileEnding
                };
                acceptableLosses = new[] {
                    "moon",
                    "moon2",
                    "voidraid",
                    "mysteryspace",
                    "limbo",
                    "meridian",
                    "solusweb"
                };
            }
        }

        private void PortalDialerPreDialState_OnEnter(On.RoR2.PortalDialerController.PortalDialerPreDialState.orig_OnEnter orig, PortalDialerController.PortalDialerPreDialState self)
        {
            ChatMessage.SendColored($"Victory conditon is {ArchipelagoClient.victoryCondition}.", Color.magenta);
            orig(self);
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
                session.Socket.SendPacketAsync(packet);

                new ArchipelagoEndMessage().Send(NetworkDestination.Clients);
            }
            orig(self, gameEndingDef);
        }

        private bool IsEndingAcceptable(GameEndingDef gameEndingDef)
        {
            Log.LogDebug($"ending stage is {Stage.instance.sceneDef.cachedName}");
            return acceptableEndings.Contains(gameEndingDef) ||
                (finalStageDeath && gameEndingDef == RoR2Content.GameEndings.StandardLoss) && (acceptableLosses.Contains(Stage.instance.sceneDef.cachedName)) ||
                (finalStageDeath && gameEndingDef == RoR2Content.GameEndings.ObliterationEnding) && (acceptableLosses.Contains(Stage.instance.sceneDef.cachedName));
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
                session.Socket.SendPacketAsync(sayPacket);
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
                session.Socket.SendPacketAsync(sayPacket);
            }
            CollectPromptPanel?.SetActive(false);

        }
    }
}
