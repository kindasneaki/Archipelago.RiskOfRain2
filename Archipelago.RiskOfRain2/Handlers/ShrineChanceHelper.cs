using RoR2;

namespace Archipelago.RiskOfRain2.Handlers
{
    class ShrineChanceHelper : IHandler
    {

        public void Hook()
        {
            RoR2.SceneDirector.onGenerateInteractableCardSelection += SceneDirector_onGenerateInteractableCardSelection;
            RoR2.SceneExitController.onFinishExit += SceneExitController_onFinishExit;
        }

        private void SceneExitController_onFinishExit(SceneExitController obj)
        {
        }

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
