using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class StageBlockerHandler
    {
        private void SceneDef_AddDestinationsToWeightedSelection(On.RoR2.SceneDef.orig_AddDestinationsToWeightedSelection orig, SceneDef self, WeightedSelection<SceneDef> dest, Func<SceneDef, bool> canAdd)
        {
            // This forces it to use the normal destination group instead of switching to the looped group after the first loop. (the looped ones are in this group for some reason).
            // This is probably really unstable with updates to the game but I don't see any other way to do this currently.
            if (self.destinationsGroup)
            {
                self.destinationsGroup.AddToWeightedSelection(dest, canAdd);
            }
            else
            {
                orig(self, dest, canAdd);
            }
        }

        private void ChatBox_OnEnable(On.RoR2.UI.ChatBox.orig_OnEnable orig, RoR2.UI.ChatBox self)
        {
            orig(self);
            if (revertToBeginningMessage != "")
            {
                ChatMessage.Send(revertToBeginningMessage);
                revertToBeginningMessage = "";
            }
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
            Log.LogDebug($"SceneExitController_SetState called. isAlternatePath: {self.isAlternatePath}, useRunNextStageScene: {self.useRunNextStageScene}");
            Log.LogDebug($"SceneExitController_SetState called. name: {self.name}, useRunNextStageScene: {self.useRunNextStageScene}");
            if (self.isColossusPortal)
            {
                bool runNextStage = true;
                int stageOrder = SceneCatalog.mostRecentSceneDef.stageOrder;

                Log.LogDebug($"SceneExitController_SetState checking for blocked stages. Current stage order {stageOrder}, mostRecent..{mostRecentStageGroup}.");
                if (stageOrder > 5) stageOrder = mostRecentStageGroup; // if the stage order is greater than 5, use the current scene's stage order instead

                switch (stageOrder)
                {
                    case 1:
                        runNextStage = CheckBlocked("lemuriantemple");
                        break;
                    case 2:
                        // with habitatfall being a stage you usually cant get to without an initial loop we need to add special handling for it
                        runNextStage = CheckBlocked("habitat") && CheckBlocked("habitatfall");
                        WeightedSelection<SceneDef> tier2Selection = new WeightedSelection<SceneDef>();
                        if (!CheckBlocked("habitat")) tier2Selection.AddChoice(SceneCatalog.FindSceneDef("habitat"), 10);
                        if (!CheckBlocked("habitatfall")) tier2Selection.AddChoice(SceneCatalog.FindSceneDef("habitatfall"), 10);
                        // This will prevent what loop you are on to decided what stage you go to.
                        if (!runNextStage)
                        {
                            self.isAlternatePath = false;
                        }
                        Run.instance.PickNextStageScene(tier2Selection);
                        self.tier3AlternateDestinationScene = Run.instance.nextStageScene;
                        self.destinationScene = Run.instance.nextStageScene;
                        break;
                    case 3:  
                    case 4:
                    case 5:
                        runNextStage = CheckBlocked("meridian");
                        break;
                }
               

                self.useRunNextStageScene = runNextStage;
            }
            // If the player has completed more than 3 environments, the game will default the encrypted portal to goto Solutional Haunt and we want the player to always goto Conduit Canyon instead.
            // This might not be necessary anymore because we are forcefully spawning the conduit canyon portal.
            else if (self.name == "HardwareProgPortal_Haunt(Clone)" || self.name == "HardwareProgPortal(Clone)")
            {
                int stageOrder = SceneCatalog.mostRecentSceneDef.stageOrder;
                Log.LogDebug($"SceneExitController_SetState checking for blocked stages. Current stage order {stageOrder}");
                if (stageOrder == 3 && !CheckBlocked("conduitcanyon"))
                {
                    Log.LogDebug($"SceneExitController_SetState changing destination to Conduit Canyon.");
                    SceneDef conduitCanyon = SceneCatalog.FindSceneDef("conduitcanyon");
                    self.destinationScene = conduitCanyon;
                }
                // Solutional Haunt is a special case where it is a stage that is only accessible from Conduit Canyon. If the player has not unlocked Solutional Haunt, they will be sent back to the beginning of the run.
                // This shouldn't be reachable but leaving for now.
                else if (stageOrder != 3 && SceneCatalog.mostRecentSceneDef.cachedName == "conduitcanyon" && CheckBlocked("solutionalhaunt"))
                {
                    self.useRunNextStageScene = true;
                }

            }
            else if (self.name == "Teleporter_ConduitCanyonVariant")
            {
                if (CheckBlocked("solutionalhaunt"))
                {
                    self.useRunNextStageScene = true;
                }
            }
            else if (self.name == "EyePortal(Clone)")
            {
                if (CheckBlocked("computationalexchange"))
                {
                    self.useRunNextStageScene = true;
                }
            }

            if (self.useRunNextStageScene)
            {
                manuallyPickingStage = true;
                Run.instance.PickNextStageSceneFromCurrentSceneDestinations();
                Log.LogDebug("SceneExitController_SetState forcefully reroll next stagescene");
                manuallyPickingStage = false;
            }
            mostRecentStageGroup = SceneCatalog.mostRecentSceneDef.stageOrder;
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
            stages_available.Add(scenedef);
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

            bool hasHabitat = false;
            bool hasHabitatFall = false;

            // Since hatitatfall is a stage you usually cant get to without an initial loop we need to add special handling for it
            choices.choices.ForEachTry( choice =>
            {
                if (choice.value.cachedName == "habitat") hasHabitat = true;
                if (choice.value.cachedName == "habitatfall") hasHabitatFall = true;
            });

            if (hasHabitat || hasHabitatFall)
            {
                // We need a new sceneGroup here because startingSceneGroup has all the first stages in it and we only want to roll the two alternate stages at this level.
                SceneCollection habitatSceneGroup = new SceneCollection();
                SceneCollection originalStartingSceneGroup = self.startingSceneGroup;
                self.startingSceneGroup = habitatSceneGroup;
                self.startingSceneGroup.AddToWeightedSelection(choices, self.CanPickStage);
                orig(self, choices);
                self.startingSceneGroup = originalStartingSceneGroup;
                return;
            }


            // 46 = Void Locus and if you are on that stage and you dont have The Planetarium the player will be moved back to orderedstage 1.
            if (SceneCatalog.mostRecentSceneDef.cachedName == "voidstage" && CheckBlocked("voidraid"))
            {
                Log.LogDebug("loaded Void Locus without The Planetarium");
                SceneCatalog.mostRecentSceneDef.stageOrder = 1;
                Log.LogDebug("Switching to stage 1");
                self.startingSceneGroup.AddToWeightedSelection(choices, self.CanPickStage);
                
            }

            if (SceneCatalog.mostRecentSceneDef.cachedName == "conduitcanyon" && !CheckBlocked("solutionalhaunt"))
            {
                manuallyPickingStage = false;
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
                        reason = $"you need <color=#dda0dd>Stage {SceneCatalog.mostRecentSceneDef.stageOrder}</color>";
                    }
                    else if (SceneCatalog.mostRecentSceneDef.stageOrder > amountOfStages && progressivesStages)
                    {
                        reason = $"you need {SceneCatalog.mostRecentSceneDef.stageOrder} <color=#dda0dd>Progressive Stages</color>";
                    } else
                    {
                        List<string> stagesNeeded = new List<string>();
                        reason = $"you're missing ";
                        foreach (KeyValuePair<string, int> entry in stageLookup)
                        {

                            if(entry.Value == SceneCatalog.mostRecentSceneDef.stageOrder && SceneIsInSeed(entry.Key))
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
                                    reason += $"<color=#dda0dd>{locationNames[stagesNeeded[i]]}</color>";
                                    if (stagesNeeded.Count > 1) reason += ", ";
                                }
                                else
                                {
                                    reason += $"or <color=#dda0dd>{locationNames[stagesNeeded[i]]}</color>";
                                }
                            }
                        }

                    }
                    revertToBeginningMessage = $"Archipelago: <color=#FF0000>Unable to advance to the next set of stages because</color> {reason}!";

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

        private bool SceneIsInSeed(string sceneName)
        {
            if (!dlcLookup.TryGetValue(sceneName, out string dlc))
            {
                return true;
            }
            switch(dlc)
            {
                case "sots":
                    return ArchipelagoClient.seedHasSOTS;
                case "sotv":
                    return ArchipelagoClient.seedHasSOTV;
                case "alloyed":
                    return ArchipelagoClient.seedHasALLOYED;
                default:
                    return false;
            }
        }
    }
}
