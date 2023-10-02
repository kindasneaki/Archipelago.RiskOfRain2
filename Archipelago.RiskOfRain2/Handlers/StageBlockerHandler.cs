using EntityStates;
using Archipelago.RiskOfRain2.Console;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    class StageBlockerHandler : IHandler
    {
        // setup all scene indexes as megic numbers
        // scenes from https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
        // main scenes
        public const int ancientloft = 3;       // Aphelian Sanctuary
        public const int arena = 4;             // Void Fields
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


        // A list of stages that should be blocked because they are locked by archipelago
        // uses scene names: https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
        List<int> blocked_stages;
        List<int> unblocked_stages;
        private bool manuallyPickingStage = false; // used to keep track of when the call to PickNextStageScene is from the StageBlocker
        private bool voidPortalSpawned = false; // used for the deep void portal in Void Locus.
        private SceneDef prevOrderedStage = null; // used to keep track of what the scene was before the next scene is selected

        public StageBlockerHandler()
        {
            Log.LogDebug($"StageBlocker handler constructor.");
            blocked_stages = new List<int>();
            unblocked_stages = new List<int>();

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
            On.RoR2.SceneExitController.SetState += SceneExitController_SetState;
            On.EntityStates.LunarTeleporter.Active.OnEnter += Active_OnEnter;
            On.RoR2.Run.CanPickStage += Run_CanPickStage;
            On.RoR2.Run.PickNextStageScene += Run_PickNextStageScene;
            On.RoR2.VoidStageMissionController.FixedUpdate += VoidStageMissionController_FixedUpdate;
            On.RoR2.VoidStageMissionController.OnDisable += VoidStageMissionController_OnDisable;
            ArchipelagoConsoleCommand.OnArchipelagoShowUnlockedStagesCommandCalled += ArchipelagoConsoleCommand_OnArchipelagoShowUnlockedStagesCommandCalled;
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
            On.RoR2.SceneExitController.SetState -= SceneExitController_SetState;
            On.EntityStates.LunarTeleporter.Active.OnEnter -= Active_OnEnter;
            On.RoR2.Run.CanPickStage -= Run_CanPickStage;
            On.RoR2.Run.PickNextStageScene -= Run_PickNextStageScene;
            On.RoR2.VoidStageMissionController.FixedUpdate -= VoidStageMissionController_FixedUpdate;
            On.RoR2.VoidStageMissionController.OnDisable -= VoidStageMissionController_OnDisable;
        }

        public void BlockAll()
        {
            // TODO add support for only blocking environments known to be in the pool
            // (eg. simulacrum should not be blocked if not in the pool, otherwise it would be permanently locked)
            Log.LogDebug($"StageBlocker blocking all...");

            // scenes from https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
            // block all main scenes
            Block(ancientloft);        // Aphelian Sanctuary
            Block(arena);              // Void Fields
            Block(blackbeach);         // Distant Roost
            Block(blackbeach2);        // Distant Roost
            Block(dampcavesimple);     // Abyssal Depths
            Block(foggyswamp);         // Wetland Aspect
            Block(frozenwall);         // Rallypoint Delta
            Block(golemplains);        // Titanic Plains
            Block(golemplains2);       // Titanic Plains
            Block(goolake);            // Abandoned Aqueduct
            Block(itancientloft);      // The Simulacrum
            Block(itdampcave);         // The Simulacrum
            Block(itfrozenwall);       // The Simulacrum
            Block(itgolemplains);      // The Simulacrum
            Block(itgoolake);          // The Simulacrum
            Block(itmoon);             // The Simulacrum
            Block(itskymeadow);        // The Simulacrum
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
            Block(mysteryspace);       // Hidden Realm: A Moment, Fractured
        }

        public void UnBlockAll()
        {
            blocked_stages.Clear();
        }

        /**
         * Blocks a given environment.
         * Returns true if the stage was blocked by this call.
         */
        public bool Block(int index)
        {
            if (blocked_stages.Contains(index))
            {
                Log.LogDebug($"Environment already blocked: index {index}.");
                return false;
            }
            Log.LogDebug($"Blocking environment: index {index}.");
            blocked_stages.Add(index);
            return true;
        }

        /**
         * Unblocks a given environment.
         * Returns true if the stage was unblocked by this call.
         */
        public bool UnBlock(int index)
        {
            Log.LogDebug($"UnBlocking environment: index {index}.");
            unblocked_stages.Add(index);
            return blocked_stages.Remove(index);
        }

        /**
         * Returns true if a stage is blocked.
         */
        public bool CheckBlocked(int index)
        {
            // Checking the list linearly should be fine.
            // Hooking update methods were avoided as much as they could be and the list itself is short.
            foreach (int block in blocked_stages)
            {
                if (index == block) return true;
            }
            return false;
        }
        private void ArchipelagoConsoleCommand_OnArchipelagoShowUnlockedStagesCommandCalled()
        {
            foreach (var scene in unblocked_stages)
            {
                if (LocationHandler.locationsNames.ContainsKey(scene))
                {
                    ChatMessage.Send($"{LocationHandler.locationsNames[scene]}");
                }
            }
        }

        /**
         * Unalign the teleporter when Commencement is not unlocked.
         */
        private void Active_OnEnter(On.EntityStates.LunarTeleporter.Active.orig_OnEnter orig, EntityStates.LunarTeleporter.Active self)
        {
            if (CheckBlocked(moon2))
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
        private void SceneExitController_SetState(On.RoR2.SceneExitController.orig_SetState orig, SceneExitController self, SceneExitController.ExitState newState)
        {
            // Suppose the player(s) enters a scene where they do not have a valid destination currently.
            // They would be garunteed to be stuck in that level on the next stage.
            // By forcefully repicking the next scene, the player(s) can go to a scene that was unblocked while in the current scene.
            if (SceneExitController.ExitState.Finished == newState && self.useRunNextStageScene)
            {
                manuallyPickingStage = true;
                Run.instance.PickNextStageSceneFromCurrentSceneDestinations();
                Log.LogDebug("SceneExitController_SetState forcefully reroll next stagescene");
                manuallyPickingStage = false;
            }
            orig(self, newState);
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
                                if (CheckBlocked(arena))
                                {
                                    ChatMessage.SendColored("The void rejects you.", new Color(0x88, 0x02, 0xd6));
                                    gi.SetInteractabilityConditionsNotMet();
                                }
                                else gi.SetInteractabilityAvailable();
                                break;
                            case "PORTAL_VOID_CONTEXT":
                                if (CheckBlocked(voidstage))
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

            if (CheckBlocked(voidraid))
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
            // TODO add goal message
            ChatMessage.SendColored($"Victory conditon is {ArchipelagoClient.victoryCondition}.", Color.magenta);
            if (CheckBlocked(artifactworld))
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
            if (CheckBlocked(limbo))
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
            if (NetworkServer.active && CheckBlocked(limbo))
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

            int index = (int) sceneDef.sceneDefIndex;
            if (CheckBlocked(index))
            {
                self.GetComponent<PurchaseInteraction>().SetAvailable(false);
                Log.LogDebug($"Bazaar Seer attempted to pick scene {index}; blocked.");
                return;
            } else
            {
                Log.LogDebug($"Bazaar Seer picked scene {index}");
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

            if (CheckBlocked(bazaar))
            {
                if (self.shouldAttemptToSpawnShopPortal)
                {
                    Log.LogDebug("Blue / bazaar portal blocked.");
                    ChatMessage.Send("The blue portal was too shy to come out!");
                }
                self.shouldAttemptToSpawnShopPortal = false;
            }
            if (CheckBlocked(goldshores))
            {
                if (self.shouldAttemptToSpawnGoldshoresPortal)
                {
                    Log.LogDebug("Gold / goldshores portal blocked.");
                    ChatMessage.Send("The gold portal was missing the key to enter and disappeared!");
                }
                self.shouldAttemptToSpawnGoldshoresPortal = false;
            }
            if (CheckBlocked(mysteryspace))
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
            int index = (int) scenedef.sceneDefIndex;
            if (CheckBlocked(index))
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
            if (SceneCatalog.mostRecentSceneDef.sceneDefIndex.ToString() == "46" && CheckBlocked(voidraid))
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
                // populate choices (in some manner) when there are no choices
                if (0 == choices.Count)
                {
                    Log.LogDebug("no choices for next scene; setting up alternate choices");

                    if (prevOrderedStage) Log.LogDebug($"prev scene {prevOrderedStage.sceneDefIndex} in stage {prevOrderedStage.stageOrder}");
                    else Log.LogDebug("no prev scene");

                    Log.LogDebug("adding choices for stage 1");
                    self.startingSceneGroup.AddToWeightedSelection(choices, self.CanPickStage);
                }
                else Log.LogDebug("there are choices for the next scene; skipping tampering said choices");

                prevOrderedStage = SceneCatalog.mostRecentSceneDef;
            }

            orig(self, choices);
            Log.LogDebug($"next scene {self.nextStageScene.sceneDefIndex} in stage {self.nextStageScene.stageOrder}");
        }

        // Checks to see when the Deep Portal spawns and to see if you have The Planetarium to proceed.
        private void VoidStageMissionController_FixedUpdate(On.RoR2.VoidStageMissionController.orig_FixedUpdate orig, VoidStageMissionController self)
        {
            orig(self);
            if (!CheckBlocked(voidraid))
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
