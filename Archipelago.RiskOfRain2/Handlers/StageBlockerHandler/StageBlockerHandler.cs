using Archipelago.RiskOfRain2.Console;
using Archipelago.RiskOfRain2.Lookup;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class StageBlockerHandler : IHandler
    {
        public int mostRecentStageGroup = 0;

        // A list of stages that should be blocked because they are locked by archipelago
        // uses scene names: https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
        List<int> blocked_stages;
        List<int> unblocked_stages;
        List<string> blocked_string_stages;
        List<string> unblocked_string_stages;
        List<SceneDef> stages_available;
        private bool manuallyPickingStage = false; // used to keep track of when the call to PickNextStageScene is from the StageBlocker
        private bool voidPortalSpawned = false; // used for the deep void portal in Void Locus.
        private SceneDef prevOrderedStage = null; // used to keep track of what the scene was before the next scene is selected

        private SeerPortal seerPortal;

        public StageBlockerHandler()
        {
            Log.LogDebug($"StageBlocker handler constructor.");
            blocked_stages = new List<int>();
            unblocked_stages = new List<int>();
            blocked_string_stages = new List<string>();
            unblocked_string_stages = new List<string>();
            stages_available = new List<SceneDef>();
            amountOfStages = 0;

            // blocking stages should be down by the owner of this object
        }

        public void Hook()
        {
            On.RoR2.SceneDirector.PlaceTeleporter += SceneDirector_PlaceTeleporter;
            On.RoR2.TeleporterInteraction.AttemptToSpawnAllEligiblePortals += TeleporterInteraction_AttemptToSpawnAllEligiblePortals1;
            On.RoR2.SeerStationController.SetTargetScene += SeerStationController_SetTargetScene;
            On.EntityStates.Interactables.MSObelisk.ReadyToEndGame.OnEnter += ReadyToEndGame_OnEnter;
            On.EntityStates.Interactables.MSObelisk.TransitionToNextStage.FixedUpdate += TransitionToNextStage_FixedUpdate;
            On.RoR2.PortalDialerController.PerformActionServer += PortalDialerController_PerformActionServer;
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
            On.RoR2.SceneDef.AddDestinationsToWeightedSelection += SceneDef_AddDestinationsToWeightedSelection;
            On.RoR2.PortalSpawner.Start += PortalSpawner_Start;
        }

        public void UnHook()
        {
            On.RoR2.SceneDirector.PlaceTeleporter -= SceneDirector_PlaceTeleporter;
            On.RoR2.TeleporterInteraction.AttemptToSpawnAllEligiblePortals -= TeleporterInteraction_AttemptToSpawnAllEligiblePortals1;
            On.RoR2.SeerStationController.SetTargetScene -= SeerStationController_SetTargetScene;
            On.EntityStates.Interactables.MSObelisk.ReadyToEndGame.OnEnter -= ReadyToEndGame_OnEnter;
            On.EntityStates.Interactables.MSObelisk.TransitionToNextStage.FixedUpdate -= TransitionToNextStage_FixedUpdate;
            On.RoR2.PortalDialerController.PerformActionServer -= PortalDialerController_PerformActionServer;
            On.RoR2.FrogController.Pet -= FrogController_Pet;
            On.RoR2.Interactor.PerformInteraction -= Interactor_PerformInteraction;
            On.RoR2.SceneExitController.Begin -= SceneExitController_Begin;
            On.EntityStates.LunarTeleporter.Active.OnEnter -= Active_OnEnter;
            On.RoR2.Run.CanPickStage -= Run_CanPickStage;
            On.RoR2.Run.PickNextStageScene -= Run_PickNextStageScene;
            On.RoR2.UI.ChatBox.OnEnable -= ChatBox_OnEnable;
            On.RoR2.VoidStageMissionController.FixedUpdate -= VoidStageMissionController_FixedUpdate;
            On.RoR2.VoidStageMissionController.OnDisable -= VoidStageMissionController_OnDisable;
            On.RoR2.SceneDef.AddDestinationsToWeightedSelection -= SceneDef_AddDestinationsToWeightedSelection;
            On.RoR2.PortalSpawner.Start -= PortalSpawner_Start;

            // Reset values to prevent issues when restarting a run
            blocked_stages = null;
            unblocked_stages = null;
            blocked_string_stages = null;
            unblocked_string_stages = null;
            seerPortal = null;
            stages_available = null;
            mostRecentStageGroup = 0;
        }

        public void BlockAll()
        {
            foreach (SceneDef scenedef in SceneCatalog.allSceneDefs)
            {
                Log.LogDebug($"scene index {SceneCatalog.FindSceneIndex(scenedef.cachedName)} scene name {scenedef.cachedName}");
                Log.LogDebug($"blocked by loop? {scenedef.isLockedBeforeLooping}");               
                scenedef.isLockedBeforeLooping = false; // this is only used for the bazaar to block them before the first loop which we dont want

                if (scenedef.sceneType == SceneType.Stage || scenedef.sceneType == SceneType.Intermission)
                {
                    SceneIndex index = SceneCatalog.FindSceneIndex(scenedef.cachedName);
                    if (index == SceneIndex.Invalid) return;

                    Block(scenedef.cachedName);

                }
            }

            // scenes from https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/
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
            string stageName = LocationNames.cachedLocationsNames[index];
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
                if (LocationNames.cachedLocationsNames.ContainsValue(scene))
                {
                    ChatMessage.Send($"{scene}");
                }
            }
        }

        public void GetAvailableStages()
        {
            stages_available.Clear();
            manuallyPickingStage = true;
            Run.instance.PickNextStageSceneFromCurrentSceneDestinations();
            manuallyPickingStage = false;
            if (stages_available.Count > 0 && showSeerPortals)
            {
                seerPortal.CreatePortal(stages_available);
            }
        }
    }
}
