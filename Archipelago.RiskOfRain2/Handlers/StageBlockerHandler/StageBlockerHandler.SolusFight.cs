using RoR2;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class StageBlockerHandler
    {
        private void SolusFight_TriggerSirensCallPortal(On.RoR2.SolusFight.orig_TriggerSirensCallPortal orig, SolusFight self)
        {
            Log.LogDebug("SolusFight.TriggerSirensCallPortal FIRED");
            orig(self);
            int? conduitLocationChecks = Locationhandler.GetSceneTotalLocations("conduitcanyon");
            if (conduitLocationChecks.HasValue && conduitLocationChecks.Value == 0 && CharacterHasAlloyedBossKey())
            {
                Log.LogDebug($"SolusFight.TriggerSirensCallPortal: forcing portal to Computational Exchange");
                try { ForcePortal(self, self.conduitCanyonPortalSpawner); }
                catch (System.Exception e) { Log.LogError($"ForcePortal threw: {e}"); }
            }
            else
            {
                try { ForcePortal(self, self.conduitCanyonPortalSpawner); }
                catch (System.Exception e) { Log.LogError($"ForcePortal threw: {e}"); }
            }
        }

        private void SolusFight_TriggerServer(On.RoR2.SolusFight.orig_TriggerServer orig, SolusFight self)
        {
            Log.LogDebug($"SolusFight.TriggerServer: stageClearCountInCurrentLoop={Run.instance.stageClearCountInCurrentLoop} " +
                 $"SolusWingBeaten={Run.instance.GetEventFlag("SolusWingBeaten")} " +
                 $"hasExpansion={self.RunHasRequiredExpansion()}");

            orig(self);
            int? conduitLocationChecks = Locationhandler.GetSceneTotalLocations("conduitcanyon");
            if (conduitLocationChecks.HasValue && conduitLocationChecks.Value == 0 && CharacterHasAlloyedBossKey())
            {
                Log.LogDebug($"SolusFight.TriggerServer: forcing portal to Computational Exchange");
                try { ForcePortal(self, self.computationalExchangePortalSpawner); }
                catch (System.Exception e) { Log.LogError($"ForcePortal threw: {e}"); }
            }
            else
            {
                try { ForcePortal(self, self.conduitCanyonPortalSpawner); }
                catch (System.Exception e) { Log.LogError($"ForcePortal threw: {e}"); }
            }
        }

        private void AccessCodesMissionController_OnStartServer(On.RoR2.AccessCodesMissionController.orig_OnStartServer orig, AccessCodesMissionController self)
        {
            if (Run.instance.GetEventFlag("SolusWingBeaten"))
            {
                Log.LogDebug($"Solus Wing has been beaten, ignoring Solus Wing death.");
                self.ignoreSolusWingDeath = true;

                // Run.instance.ResetEventFlag("SolusWingBeaten");
            }
            orig(self);
        }


        /// <summary>
        /// Makes <paramref name="wanted"/> the only SolusFight portal that opens when the teleporter
        /// finishes charging. Call this after orig() so vanilla's own choice has already been applied.
        /// </summary>
        private void ForcePortal(SolusFight fight, PortalSpawner wanted)
        {
            if (!wanted)
            {
                Log.LogWarning("ForcePortal: requested spawner is null, leaving vanilla's choice in place.");
                return;
            }

            DisableOtherPortals(fight, wanted);
            EnablePortal(wanted);
        }

        private void DisableOtherPortals(SolusFight fight, PortalSpawner keep)
        {
            foreach (PortalSpawner spawner in GetPortalSpawners(fight))
            {
                if (spawner && spawner != keep && spawner.WillSpawn)
                {
                    spawner.WillSpawn = false;
                    Log.LogDebug($"ForcePortal: disabled {Describe(spawner)}");
                }
            }
        }

        private void EnablePortal(PortalSpawner spawner)
        {
            TeleporterInteraction teleporter = TeleporterInteraction.instance;
            if (teleporter)
            {
                // A PortalSpawner only registers itself with the teleporter on a false -> true
                // transition of WillSpawn, so register explicitly in case it is already true.
                teleporter.TryAddingPortal(spawner);
                spawner.spawnReferenceLocationOverride = teleporter.transform;
            }

            spawner.WillSpawn = true;
            Log.LogDebug($"ForcePortal: enabled {Describe(spawner)}");
        }

        private PortalSpawner[] GetPortalSpawners(SolusFight fight) => new[]
        {
            fight.conduitCanyonPortalSpawner,
            fight.solutionalHauntPortalSpawner,
            fight.computationalExchangePortalSpawner,
        };

        private string Describe(PortalSpawner spawner) =>
            $"{spawner.gameObject.name} (card={(spawner.PortalSpawnCard ? spawner.PortalSpawnCard.name : "<null>")})";

        private bool CharacterHasAlloyedBossKey() {
            int itemCount = 0;
            foreach (var playerController in PlayerCharacterMasterController.instances)
            {
                if (playerController.master != null && playerController.master.inventory != null)
                {
                    itemCount += playerController.master.inventory.GetItemCountPermanent(DLC3Content.Items.MasterCore);
                    Log.LogDebug($"There are a total of {itemCount} MasterCore Keys so far.");
                }
            }
            return itemCount > 0;
         }
    
    }
}
