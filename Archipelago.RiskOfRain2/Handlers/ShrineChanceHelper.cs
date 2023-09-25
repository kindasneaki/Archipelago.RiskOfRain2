using RoR2;

namespace Archipelago.RiskOfRain2.Handlers
{
    class ShrineChanceHelper : IHandler
    {

        public void Hook()
        {
            RoR2.SceneDirector.onGenerateInteractableCardSelection += SceneDirector_onGenerateInteractableCardSelection;
            // RoR2.SceneDirector.onPrePopulateMonstersSceneServer += SceneDirector_onPrePopulateMonstersSceneServer;
            // On.RoR2.ClassicStageInfo.Awake += ClassicStageInfo_Awake;
        }

/*        private void ClassicStageInfo_Awake(On.RoR2.ClassicStageInfo.orig_Awake orig, ClassicStageInfo self)
        {
            orig(self);
            Log.LogDebug($"Loop clear count {Run.instance.loopClearCount}");
            Run.stagesPerLoop = 1;
            Log.LogDebug($"monster credits {self.sceneDirectorMonsterCredits} current scene {LocationHandler.sceneDef}");
            foreach (var card in self.monsterCards)
            {
                Log.LogDebug($"card cost {card.cost} card name {card.spawnCard.name}");
            }
            foreach (var families in self.possibleMonsterFamilies)
            {
                Log.LogDebug($"family name {families.monsterFamilyCategories.name} max stage completion {families.maximumStageCompletion} min stage completion {families.minimumStageCompletion}");
            }
            foreach (var pool in self.monsterDccsPool.poolCategories)
            {
                Log.LogDebug($"pool name {pool.name}");
                foreach (var always in pool.alwaysIncluded)
                {
                    Log.LogDebug($"always included {always.dccs.name}");
                }
                foreach (var conditionsMet in pool.includedIfConditionsMet)
                {
                    Log.LogDebug($"conditions met {conditionsMet.dccs.name}");
                }
                foreach (var conditionsNotMet in pool.includedIfNoConditionsMet)
                {
                    Log.LogDebug($"conditions not met {conditionsNotMet.dccs.name}");
                }
            }
        }*/
/*
        private void SceneDirector_onPrePopulateMonstersSceneServer(SceneDirector obj)
        {
            Log.LogDebug($"monster credit {obj.monsterCredit}");
        }*/

        public void UnHook()
        {
            RoR2.SceneDirector.onGenerateInteractableCardSelection -= SceneDirector_onGenerateInteractableCardSelection;
        }
        private void SceneDirector_onGenerateInteractableCardSelection(SceneDirector arg1, DirectorCardCategorySelection arg2)
        {
            Log.LogDebug($"interactible credit {arg1.interactableCredit}");
            arg1.interactableCredit *= 2;
            Log.LogDebug($"interactible credit {arg1.interactableCredit}");
            foreach (var cata in arg2.categories)
            {
                Log.LogDebug($"categories in arg2 {cata.name}");
                if (cata.name == "Shrines")
                {
                    foreach (var card in cata.cards)
                    {
                        Log.LogDebug($"card cost is {card.cost} {card.spawnCard.name}");
                        card.spawnCard.directorCreditCost = 5;
                        Log.LogDebug($"card cost is {card.cost}");
                    }
                }
            }

        }
    }
}
