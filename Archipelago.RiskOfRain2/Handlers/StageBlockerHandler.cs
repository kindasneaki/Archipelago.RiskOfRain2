﻿using EntityStates;
using Archipelago.RiskOfRain2.Console;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

namespace Archipelago.RiskOfRain2.Handlers
{
    class StageBlockerHandler : IHandler
    {
        // setup all scene indexes as magic numbers
        // scenes from https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
        // main scenes
        public const int ancientloft = 3;       // Aphelian Sanctuary
        public const int arena = 4;             // Void Fields
        public const int lakes = 28;            // Verdant Falls
        public const int blackbeach = 7;        // Distant Roost
        public const int blackbeach2 = 8;       // Distant Roost
        public const int dampcavesimple = 10;   // Abyssal Depths
        public const int foggyswamp = 12;       // Wetland Aspect
        public const int frozenwall = 13;       // Rallypoint Delta
        public const int golemplains = 15;      // Titanic Plains
        public const int golemplains2 = 16;     // Titanic Plains
        public const int goolake = 17;          // Abandoned Aqueduct
        public const int itancientloft = 20;    // The Simulacrum
        public const int itdampcave = 21;       // The Simulacrum
        public const int itfrozenwall = 22;     // The Simulacrum
        public const int itgolemplains = 23;    // The Simulacrum
        public const int itgoolake = 24;        // The Simulacrum
        public const int itmoon = 25;           // The Simulacrum
        public const int itskymeadow = 26;      // The Simulacrum
        public const int moon2 = 32;            // Commencement
        public const int rootjungle = 35;       // Sundered Grove
        public const int shipgraveyard = 37;    // Siren's Call
        public const int skymeadow = 38;        // Sky Meadow
        public const int snowyforest = 39;      // Siphoned Forest
        public const int sulfurpools = 41;      // Sulfur Pools
        public const int voidstage = 46;        // Void Locus
        public const int voidraid = 45;         // The Planetarium
        public const int wispgraveyard = 47;    // Scorched Acres
        // hidden realms
        public const int artifactworld = 5;     // Hidden Realm: Bulwark's Ambry
        public const int bazaar = 6;            // Hidden Realm: Bazaar Between Time
        public const int goldshores = 14;       // Hidden Realm: Gilded Coast
        public const int limbo = 27;            // Hidden Realm: A Moment, Whole
        public const int mysteryspace = 33;     // Hidden Realm: A Moment, Fractured
                                                // TODO these should probably go somewhere else to better keep track of them since they are used in several places
        public static Dictionary<string, bool> stageUnlocks = new()
        {
            { "Stage 1", false },
            { "Stage 2", false },
            { "Stage 3", false },
            { "Stage 4", false },

        };
        public static int amountOfStages = 0;
        public readonly Dictionary<string, int> stageLookup = new()
        {
            { "ancientloft", 1 },
            { "dampcavesimple", 3 },
            { "foggyswamp", 1 },
            { "frozenwall", 2 },
            { "goolake", 1 },
            { "rootjungle", 3 },
            { "shipgraveyard", 3 },
            { "skymeadow", 4 },
            { "sulfurpools", 2 },
            { "wispgraveyard", 2 },
        };
        public readonly Dictionary<string, string> locationNames = new()
        {
            { "ancientloft", "Aphelian Sanctuary" },
            { "dampcavesimple", "Abyssal Depths" },
            { "foggyswamp", "Wetland Aspect" },
            { "frozenwall", "Rallypoint Delta" },
            { "goolake", "Abandoned Aqueduct" },
            { "rootjungle", "Sundered Grove" },
            { "shipgraveyard", "Siren's Call" },
            { "skymeadow", "Sky Meadow" },
            { "sulfurpools", "Sulfur Pools" },
            { "wispgraveyard", "Scorched Acres" },
        };
        public static readonly Dictionary<int, string> locationsNames = new()
        {
            { 3, "ancientloft" },
            { 4, "arena" },
            { 5, "artifactworld" },
            { 6, "bazaar" },
            { 7, "blackbeach" },
            { 8, "blackbeach2" },
            { 10, "dampcavesimple" },
            { 12, "foggyswamp" },
            { 13, "frozenwall" },
            { 14, "goldshores" },
            { 15, "golemplains" },
            { 16, "golemplains2" },
            { 17, "goolake" },
            { 27, "limbo" },
            { 28, "lakes"},
            { 32, "moon2" },
            { 33, "mysteryspace" },
            { 35, "rootjungle" },
            { 37, "shipgraveyard" },
            { 38, "skymeadow" },
            { 39, "snowyforest" },
            { 41, "sulfurpools" },
            { 45, "voidraid" },
            { 46, "voidstage" },
            { 47, "wispgraveyard" },
        };

        // A list of stages that should be blocked because they are locked by archipelago
        // uses scene names: https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
        List<int> blocked_stages;
        List<int> unblocked_stages;
        List<string> blocked_string_stages;
        List<string> unblocked_string_stages;
        private bool manuallyPickingStage = false; // used to keep track of when the call to PickNextStageScene is from the StageBlocker
        private bool voidPortalSpawned = false; // used for the deep void portal in Void Locus.
        private SceneDef prevOrderedStage = null; // used to keep track of what the scene was before the next scene is selected
        public static bool progressivesStages = false;
        public static string revertToBeginningMessage = "";

        public StageBlockerHandler()
        {
            Log.LogDebug($"StageBlocker handler constructor.");
            blocked_stages = new List<int>();
            unblocked_stages = new List<int>();
            blocked_string_stages = new List<string>();
            unblocked_string_stages = new List<string>();
            amountOfStages = 0;

            // blocking stages should be down by the owner of this object
        }

        public void Hook()
        {
            On.RoR2.TeleporterInteraction.AttemptToSpawnAllEligiblePortals += TeleporterInteraction_AttemptToSpawnAllEligiblePortals1;
            On.RoR2.SeerStationController.SetTargetScene += SeerStationController_SetTargetScene;
            On.EntityStates.Interactables.MSObelisk.ReadyToEndGame.OnEnter += ReadyToEndGame_OnEnter;
            On.EntityStates.Interactables.MSObelisk.TransitionToNextStage.FixedUpdate += TransitionToNextStage_FixedUpdate;
            On.RoR2.PortalDialerController.PortalDialerIdleState.OnActivationServer += PortalDialerIdleState_OnActivationServer;
            On.RoR2.FrogController.Pet += FrogController_Pet;
            On.RoR2.Interactor.PerformInteraction += Interactor_PerformInteraction;
            On.RoR2.SceneExitController.Begin += SceneExitController_Begin;
            On.EntityStates.LunarTeleporter.Active.OnEnter += Active_OnEnter;
            On.RoR2.Run.CanPickStage += Run_CanPickStage;
            On.RoR2.Run.PickNextStageScene += Run_PickNextStageScene;
            On.RoR2.UI.ChatBox.OnEnable += ChatBox_OnEnable;
            On.RoR2.VoidStageMissionController.FixedUpdate += VoidStageMissionController_FixedUpdate;
            On.RoR2.VoidStageMissionController.OnDisable += VoidStageMissionController_OnDisable;
            ArchipelagoConsoleCommand.OnArchipelagoShowUnlockedStagesCommandCalled += ArchipelagoConsoleCommand_OnArchipelagoShowUnlockedStagesCommandCalled;
        }



        private void ChatBox_OnEnable(On.RoR2.UI.ChatBox.orig_OnEnable orig, RoR2.UI.ChatBox self)
        {
            orig(self);
            if (revertToBeginningMessage != "")
            {
                ChatMessage.SendColored(revertToBeginningMessage, Color.red);
                revertToBeginningMessage = "";
            }
        }

        public void UnHook()
        {
            On.RoR2.TeleporterInteraction.AttemptToSpawnAllEligiblePortals -= TeleporterInteraction_AttemptToSpawnAllEligiblePortals1;
            On.RoR2.SeerStationController.SetTargetScene -= SeerStationController_SetTargetScene;
            On.EntityStates.Interactables.MSObelisk.ReadyToEndGame.OnEnter -= ReadyToEndGame_OnEnter;
            On.EntityStates.Interactables.MSObelisk.TransitionToNextStage.FixedUpdate -= TransitionToNextStage_FixedUpdate;
            On.RoR2.PortalDialerController.PortalDialerIdleState.OnActivationServer -= PortalDialerIdleState_OnActivationServer;
            On.RoR2.FrogController.Pet -= FrogController_Pet;
            On.RoR2.Interactor.PerformInteraction -= Interactor_PerformInteraction;
            On.RoR2.SceneExitController.Begin -= SceneExitController_Begin;
            On.EntityStates.LunarTeleporter.Active.OnEnter -= Active_OnEnter;
            On.RoR2.Run.CanPickStage -= Run_CanPickStage;
            On.RoR2.Run.PickNextStageScene -= Run_PickNextStageScene;
            On.RoR2.UI.ChatBox.OnEnable -= ChatBox_OnEnable;
            On.RoR2.VoidStageMissionController.FixedUpdate -= VoidStageMissionController_FixedUpdate;
            On.RoR2.VoidStageMissionController.OnDisable -= VoidStageMissionController_OnDisable;
            blocked_stages = null;
            unblocked_stages = null;
            blocked_string_stages = null;
            unblocked_string_stages = null;
        }

        public void BlockAll()
        {
            foreach (SceneDef scenedef in SceneCatalog.allSceneDefs)
            {
                Log.LogDebug($"scene index {SceneCatalog.FindSceneIndex(scenedef.cachedName)} scene name {scenedef.cachedName}");
                if (scenedef.sceneType == SceneType.Stage || scenedef.sceneType == SceneType.Intermission)
                {
                    SceneIndex index = SceneCatalog.FindSceneIndex(scenedef.cachedName);
                    if (index == SceneIndex.Invalid) return;
                    
                    Block(scenedef.cachedName);

                }
            }
            // TODO add support for only blocking environments known to be in the pool
            // (eg. simulacrum should not be blocked if not in the pool, otherwise it would be permanently locked)
            Log.LogDebug($"StageBlocker blocking all...");

            // scenes from https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
            // block all main scenes
/*            Block(ancientloft);        // Aphelian Sanctuary
            Block(arena);              // Void Fields
            Block(lakes);              // Verdant Falls
            Block(blackbeach);         // Distant Roost
            Block(blackbeach2);        // Distant Roost
            Block(dampcavesimple);     // Abyssal Depths
            Block(foggyswamp);         // Wetland Aspect
            Block(frozenwall);         // Rallypoint Delta
            Block(golemplains);        // Titanic Plains
            Block(golemplains2);       // Titanic Plains
            Block(goolake);            // Abandoned Aqueduct
*//*            Block(itancientloft);      // The Simulacrum
            Block(itdampcave);         // The Simulacrum
            Block(itfrozenwall);       // The Simulacrum
            Block(itgolemplains);      // The Simulacrum
            Block(itgoolake);          // The Simulacrum
            Block(itmoon);             // The Simulacrum
            Block(itskymeadow);        // The Simulacrum*//*
            Block(moon2);              // Commencement
            Block(rootjungle);         // Sundered Grove
            Block(shipgraveyard);      // Siren's Call
            Block(skymeadow);          // Sky Meadow
            Block(snowyforest);        // Siphoned Forest
            Block(sulfurpools);        // Sulfur Pools
            Block(voidstage);          // Void Locus
            Block(voidraid);           // The Planetarium
            Block(wispgraveyard);      // Scorched Acres
            // block all hidden realms
            Block(artifactworld);      // Hidden Realm: Bulwark's Ambry
            Block(bazaar);             // Hidden Realm: Bazaar Between Time
            Block(goldshores);         // Hidden Realm: Gilded Coast
            Block(limbo);              // Hidden Realm: A Moment, Whole
            Block(mysteryspace);       // Hidden Realm: A Moment, Fractured*/
        }

        public void UnBlockAll()
        {
            blocked_string_stages.Clear();
        }

        /**
         * Blocks a given environment.
         * Returns true if the stage was blocked by this call.
         */
        public bool Block(string stageName)
        {
            if (blocked_string_stages.Contains(stageName))
            {
                Log.LogDebug($"Environment already blocked: index {stageName}.");
                return false;
            }
            Log.LogDebug($"Blocking environment: index {stageName}.");
            blocked_string_stages.Add(stageName);
            return true;
        }

        /**
         * Unblocks a given environment.
         * Returns true if the stage was unblocked by this call.
         */
        public bool UnBlock(int index)
        {
            string stageName = locationsNames[index];
            Log.LogDebug($"UnBlocking environment: index {stageName}.");
            unblocked_string_stages.Add(stageName);
            return blocked_string_stages.Remove(stageName);
        }

        /**
         * Returns true if a stage is blocked.
         */
        public bool CheckBlocked(string stageName)
        {
            if (Run.instance.nextStageScene != null && stageLookup.ContainsKey(stageName))
            {
                // Checks to make sure you have the Stage item required to get to the next set of stages
                if (!stageUnlocks[$"Stage {stageLookup[stageName]}"] && !progressivesStages)
                {
                    return true;
                } else if(stageLookup[stageName] > amountOfStages && progressivesStages)
                {
                    return true;
                }
            }
            // Checking the list linearly should be fine.
            // Hooking update methods were avoided as much as they could be and the list itself is short.
            foreach (string block in blocked_string_stages)
            {
                if (stageName == block) return true;
            }
            return false;
        }
        private void ArchipelagoConsoleCommand_OnArchipelagoShowUnlockedStagesCommandCalled()
        {
            foreach (var scene in unblocked_string_stages)
            {
                if (locationsNames.ContainsValue(scene))
                {
                    ChatMessage.Send($"{scene}");
                }
            }
        }

        /**
         * Unalign the teleporter when Commencement is not unlocked.
         */
        private void Active_OnEnter(On.EntityStates.LunarTeleporter.Active.orig_OnEnter orig, EntityStates.LunarTeleporter.Active self)
        {
            if (CheckBlocked("moon2"))
            {
                ChatMessage.SendColored("Just not feeling it right now.", new Color(0x5d, 0xd5, 0xe2));
                self.outer.SetNextState(new EntityStates.LunarTeleporter.ActiveToIdle());
                return;
            }
            orig(self);
        }

        /**
         * Force the SceneExitController to rereoll the scene before moving to the next scene.
         * This is to help prevent going into the same environment on the next stage.
         */

        private void SceneExitController_Begin(On.RoR2.SceneExitController.orig_Begin orig, SceneExitController self)
        {
            // Suppose the player(s) enters a scene where they do not have a valid destination currently.
            // They would be guaranteed to be stuck in that level on the next stage.
            // By forcefully repicking the next scene, the player(s) can go to a scene that was unblocked while in the current scene.

            if (self.isColossusPortal)
            {
                self.useRunNextStageScene = true;
            }

            if (self.useRunNextStageScene)
            {
                manuallyPickingStage = true;
                Run.instance.PickNextStageSceneFromCurrentSceneDestinations();
                Log.LogDebug("SceneExitController_SetState forcefully reroll next stagescene");
                manuallyPickingStage = false;
            }
            orig(self);
        }

        /**
         * Block interaction with the Void Fields portal if the environment is not unlocked.
         */
        private void Interactor_PerformInteraction(On.RoR2.Interactor.orig_PerformInteraction orig, Interactor self, GameObject interactableObject)
        {
            // I settled on hooking this method because I tried all other alternatives I could think of first.
            // I attempted using all of the following with little or no success:
            // - PortalSpawner_AttemptSpawnPortalServer: failed to block voidstage from spawning on teleporter
            // - PortalSpawner_Start: failed to block voidstage portal from spawning on teleporter
            // - GenericInteraction_RoR2_IInteractable_GetInteractability: broke all interactables
            // - GenericInteraction_RoR2_IInteractable_OnInteractionBegin: didn't seem to be called when using void portals

            // Blocking the use of void portals here is preferred over SceneExitController_SetState.
            // This is because it's more user friendly to let the user know they cannot travel to the void
            //  rather than redirect them to the next stage without warning.

            if (NetworkServer.active && interactableObject)
            {
                // TODO how much does this affect performance?
                foreach (IInteractable comp in interactableObject.GetComponents<IInteractable>())
                {
                    GenericInteraction gi = comp as GenericInteraction;
                    if (gi)
                    {
                        switch (gi.contextToken)
                        {
                            case "PORTAL_ARENA_CONTEXT":
                                if (CheckBlocked("arena"))
                                {
                                    ChatMessage.SendColored("The void rejects you.", new Color(0x88, 0x02, 0xd6));
                                    gi.SetInteractabilityConditionsNotMet();
                                }
                                else gi.SetInteractabilityAvailable();
                                break;
                            case "PORTAL_VOID_CONTEXT":
                                if (CheckBlocked("voidstage"))
                                {
                                    ChatMessage.SendColored("The void rejects you.", new Color(0x88, 0x02, 0xd6));
                                    gi.SetInteractabilityConditionsNotMet();
                                }
                                else gi.SetInteractabilityAvailable();
                                break;
                            // not blocking voidraid:
                            // NOTE: Planetarium has two entrances, one in Void Locus and one in Commencement
                            // Since this currently seems like an edge case where the player would truely decide to do both
                            //  if the player gets the Planetarium portal from Void Locus, they can travel there.
                            // Only the glass frog interaction in Commencement will be blocked.
                            // This also prevents the player from becoming stuck.

                            // Arguably the other portals could be handled here as well,
                            // however it seems more user friendly to just not spawn the portal at all rather
                            // than spawn the portal and make it unable to be interacted with.
                        }
                    }
                }
            }
            orig(self, interactableObject);
        }

        /**
         * Block players from petting the frog and refund them if the Planetarium is not unlocked.
         */
        private void FrogController_Pet(On.RoR2.FrogController.orig_Pet orig, FrogController self, Interactor interactor)
        {
            // We block usage of the frog out of quality of life.
            // It would feel unfail to use 10 coins just to not spawn a portal or spawn a portal the user cannot use.
            // By adding coins back to the users inventory, it shows that the transaction cannot go through.
            // Adding a message also makes this even more clear.

            if (CheckBlocked("voidraid"))
            {
                Log.LogDebug("Blocking petting the frog for planetarium.");
                // Only host can refund the coin and having the host send the message prevents duplicate messages.
                if (NetworkServer.active)
                {
                    Log.LogDebug("blocking planetarium as host.");
                    // refund the lunar coin if the player who payed the coin is this client's player
                    //interactor.GetComponent<NetworkUser>().AwardLunarCoins(1); // (only the server actually executes the contents of this method) // TODO give coin only to one person
                    foreach (NetworkUser local in NetworkUser.readOnlyLocalPlayersList)
                    {
                        Log.LogDebug("Refunding coins...");
                        local.AwardLunarCoins(1);
                        // TODO This does in fact give more coins back in multiplayer since every player would get a coin.
                        // I don't have a solution for this right now : ^)
                    }

                    ChatMessage.SendColored("The frog does not want to be pet.", Color.white);
                }
                return;
            }
            orig(self, interactor);
        }

        /**
         * Prevent the dialer from changing states if the Bulwark's Ambry is not unlocked.
         */
        private void PortalDialerIdleState_OnActivationServer(On.RoR2.PortalDialerController.PortalDialerIdleState.orig_OnActivationServer orig, BaseState self, Interactor interactor)
        {
            // ChatMessage.SendColored($"Victory conditon is {ArchipelagoClient.victoryCondition}.", Color.magenta);
            if (CheckBlocked("artifactworld"))
            {
                // give a message so the user is aware the portal dialer interaction is blocked
                ChatMessage.SendColored($"The code will never work without Hidden Realm: Bulwark's Ambry.", Color.white);
                return;
            }
            orig(self, interactor);
        }

        /**
         * Block going to A Monument, Whole if the environment is not unlocked.
         */
        private void TransitionToNextStage_FixedUpdate(On.EntityStates.Interactables.MSObelisk.TransitionToNextStage.orig_FixedUpdate orig, EntityStates.Interactables.MSObelisk.TransitionToNextStage self)
        {
            // If the player decides to commit to Obliterating,
            //  they transition state should simply end the game normally
            //  (since the player should not be allowed into limbo).
            if (CheckBlocked("limbo"))
            {
                // run normal obliterate ending
                Run.instance.BeginGameOver(RoR2Content.GameEndings.ObliterationEnding);
                self.outer.SetNextState(new Idle());
            }
            orig(self);
        }

        /**
         * Give a warning before attempting to Obliterate while A Monument, Whole is still blocked.
         */
        private void ReadyToEndGame_OnEnter(On.EntityStates.Interactables.MSObelisk.ReadyToEndGame.orig_OnEnter orig, EntityStates.Interactables.MSObelisk.ReadyToEndGame self)
        {
            // Giving this warning is important for fairness.
            // This is because if the player decides to still Obliterate,
            //  we are just going to forcefully end the run.

            // Check if this is the server running this OnEnter, since mutliplayer clients could run this.
            // This is used to prevent duplicate messages being sent in multiplayer.
            if (NetworkServer.active && CheckBlocked("limbo"))
            {
                for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                {
                    if (CharacterMaster.readOnlyInstancesList[i].inventory.GetItemCount(RoR2Content.Items.LunarTrinket) > 0)
                    {
                        ChatMessage.SendColored("Despite having Beads, you are not yet ready...", new Color(0x5d, 0xd5, 0xe2));
                        break;
                    }
                }
            }
            orig(self);
        }

        /**
         * Block shop interation with Bazaar Seers for environments that are blocked.
         */
        private void SeerStationController_SetTargetScene(On.RoR2.SeerStationController.orig_SetTargetScene orig, SeerStationController self, SceneDef sceneDef)
        {
            // For the seers, we will not change their behavior for how they pick environments.
            // This behaviour could be changed but would require changing logic in the middle of SetUpSeerStations() which would take IL Hooks.
            // This has the consequence that seers can pick environments that are blocked.
            // In that case, we can just block the seer be able to be interacted with.
            // We also should hide the destination of the Seer since the it will not be reenabled when the player obtains the environment.

            string sceneName = sceneDef.cachedName;
            if (CheckBlocked(sceneName))
            {
                self.GetComponent<PurchaseInteraction>().SetAvailable(false);
                Log.LogDebug($"Bazaar Seer attempted to pick scene {sceneName}; blocked.");
                return;
            } else
            {
                Log.LogDebug($"Bazaar Seer picked scene {sceneName}");
            }
            orig(self, sceneDef);
        }

        /**
         * Block portals for blocked environments that would be spawned by the finishing teleporter event.
         */
        private void TeleporterInteraction_AttemptToSpawnAllEligiblePortals1(On.RoR2.TeleporterInteraction.orig_AttemptToSpawnAllEligiblePortals orig, TeleporterInteraction self)
        {
            // If the player unlocks the environments while they have orbs, they can still recieved the portals.
            // But as soon as the teleporter finishes, we will not give them the portals.
            // There could be a more friendly alternative but this should be fine.

            // the portals spawned by the teleporter event are for:
            // Hidden Realm: Bazaar Between Time
            // Hidden Realm: Gilded Coast
            // Hidden Realm: A Moment, Fractured

            if (CheckBlocked("bazaar"))
            {
                if (self.shouldAttemptToSpawnShopPortal)
                {
                    Log.LogDebug("Blue / bazaar portal blocked.");
                    ChatMessage.Send("The blue portal was too shy to come out!");
                }
                self.shouldAttemptToSpawnShopPortal = false;
            }
            if (CheckBlocked("goldshores"))
            {
                if (self.shouldAttemptToSpawnGoldshoresPortal)
                {
                    Log.LogDebug("Gold / goldshores portal blocked.");
                    ChatMessage.Send("The gold portal was missing the key to enter and disappeared!");
                }
                self.shouldAttemptToSpawnGoldshoresPortal = false;
            }
            if (CheckBlocked("mysteryspace"))
            {
                if (self.shouldAttemptToSpawnMSPortal)
                {
                    Log.LogDebug("Celestial / mysteryspace portal blocked.");
                    ChatMessage.Send("The celestial portal decided you aren't ready!");
                }
                self.shouldAttemptToSpawnMSPortal = false;
            }
            orig(self);
        }

        /**
         * Forcefully fail to the CanPickStage check for stages that are blocked.
         */
        private bool Run_CanPickStage(On.RoR2.Run.orig_CanPickStage orig, Run self, SceneDef scenedef)
        {
            Log.LogDebug($"Checking CanPickStage for {scenedef.nameToken}...");
            string stageName = scenedef.cachedName;
            if (CheckBlocked(stageName))
            {
                // if the stage is blocked, it cannot be picked
                Log.LogDebug("blocking.");
                return false;
            }

            Log.LogDebug("passing through.");

            return orig(self, scenedef);
        }

        private void Run_PickNextStageScene(On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
        {
            // When the does not have a valid next environment, we will move them to an environment within the same orderedstage.
            // When this happens, we will consider the player as "lost".
            // If the player doesn't have a next environment when lost, the player will be moved back to orderedstage 1.
            // The reason for this is if the player is playing with explore mode, the player's next environment could be in a different already unlocked environment.
            // Thus if the next unlock is somewhere, it would be nice to the the player get to that somewhere without restarting the run.

            Log.LogDebug($"recent scene {SceneCatalog.mostRecentSceneDef.sceneDefIndex} in stage {SceneCatalog.mostRecentSceneDef.stageOrder}");

            // 46 = Void Locus and if you are on that stage and you dont have The Planetarium the player will be moved back to orderedstage 1.
            if (SceneCatalog.mostRecentSceneDef.cachedName == "voidstage" && CheckBlocked("voidraid"))
            {
                Log.LogDebug("loaded Void Locus without The Planetarium");
                SceneCatalog.mostRecentSceneDef.stageOrder = 1;
                Log.LogDebug("Switching to stage 1");
                self.startingSceneGroup.AddToWeightedSelection(choices, self.CanPickStage);
                
            }

            // there are 2 conditions when we should mess with this call:
            // - the call to PickNextStageScene should have originated from stage blocker
            //      (since it gets called at the beginning of the scene by the game, and at the end by the stage blocker)
            // - this should do nothing special unless the current scene happens to be an ordered stage
            if (manuallyPickingStage && SceneCatalog.mostRecentSceneDef &&  1 <= SceneCatalog.mostRecentSceneDef.stageOrder && 5 >= SceneCatalog.mostRecentSceneDef.stageOrder)
            {
                //string nextStage = $"Stage {self.nextStageScene.stageOrder - 1}";
                //Log.LogDebug($"Stage {self.nextStageScene.stageOrder} == {stageUnlocks[nextStage]}");
                // populate choices (in some manner) when there are no choices
                if (0 == choices.Count)
                {
                    string reason = "";
                    Log.LogDebug("no choices for next scene; setting up alternate choices");

                    if (prevOrderedStage) Log.LogDebug($"prev scene {prevOrderedStage.sceneDefIndex} in stage {prevOrderedStage.stageOrder}");
                    else Log.LogDebug("no prev scene");
                    Log.LogDebug($"Most recent scene stage order Stage {SceneCatalog.mostRecentSceneDef.stageOrder}");
                    if (!stageUnlocks[$"Stage {SceneCatalog.mostRecentSceneDef.stageOrder}"] && !progressivesStages)
                    {
                        reason = $"you need Stage {SceneCatalog.mostRecentSceneDef.stageOrder}";
                    }
                    else if (SceneCatalog.mostRecentSceneDef.stageOrder > amountOfStages && progressivesStages)
                    {
                        reason = $"you need {SceneCatalog.mostRecentSceneDef.stageOrder} Progressive Stages";
                    } else
                    {
                        List<string> stagesNeeded = new List<string>();
                        reason = $"you are missing ";
                        foreach (KeyValuePair<string, int> entry in stageLookup)
                        {

                            if(entry.Value == SceneCatalog.mostRecentSceneDef.stageOrder)
                            {
                                stagesNeeded.Add(entry.Key);
                            }
                        }
                        if (stagesNeeded != null && stagesNeeded.Count > 0)
                        {
                            for (var i = 0; i < stagesNeeded.Count; i++)
                            {
                                if (i < stagesNeeded.Count - 1 || stagesNeeded.Count == 1)
                                {
                                    reason += $"{locationNames[stagesNeeded[i]]}, ";
                                }
                                else
                                {
                                    reason += $"or {locationNames[stagesNeeded[i]]}";
                                }
                            }
                        }

                    }
                    revertToBeginningMessage = $"Unable to advance to the next set of stages because {reason}!";

                    Log.LogDebug("adding choices for stage 1");
                    self.startingSceneGroup.AddToWeightedSelection(choices, self.CanPickStage);
                }
                else Log.LogDebug("there are choices for the next scene; skipping tampering said choices");

                prevOrderedStage = SceneCatalog.mostRecentSceneDef;
            }

            orig(self, choices);
            Log.LogDebug($"next scene {self.nextStageScene.cachedName} in stage {self.nextStageScene.stageOrder}");
        }

        // Checks to see when the Deep Portal spawns and to see if you have The Planetarium to proceed.
        private void VoidStageMissionController_FixedUpdate(On.RoR2.VoidStageMissionController.orig_FixedUpdate orig, VoidStageMissionController self)
        {
            orig(self);
            if (!CheckBlocked("voidraid"))
            {
                return;
            }
            if (self.numBatteriesActivated >= self.numBatteriesSpawned && self.numBatteriesSpawned > 0 && !voidPortalSpawned)
            {
                Log.LogDebug("Portal Activated");
                voidPortalSpawned = true;
                var deepPortal = GameObject.Find("DeepVoidPortal(Clone)");
                deepPortal.GetComponent<SceneExitController>().useRunNextStageScene = true;
            }
        }
        // Needed to reset voidPortalSpawned to false for the next time the user is on Void Locus.
        private void VoidStageMissionController_OnDisable(On.RoR2.VoidStageMissionController.orig_OnDisable orig, VoidStageMissionController self)
        {
            orig(self);
            voidPortalSpawned = false;
        }

    }
}
