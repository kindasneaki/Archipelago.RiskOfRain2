using RoR2;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class LocationHandler
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Radio scanner

        private void ArchipelagoConsoleCommand_OnArchipelagoHighlightSatelliteCommandCalled(bool highlight)
        {
            highlightOn = highlight;
        }

        // Radio scanners will need to be forcefully spawned even if the player has purchased them
        //  otherwise the check would be impossible to complete.

        private void SceneDirector_PopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            // XXX somehow SceneDirector_PopulateScene can get called several times in a row, thus spawning a bunch of scanners... why do the calls happen?
            Log.LogDebug("SceneDirector_PopulateScene"); // XXX remove after figuring out why this can get called repeatedly
            // XXX perhaps a solution could be to use flags similar to shrines if there is no apparent reason why scene population can repeat

            orig(self); // let the director do it's own thing first as to not get in the way

            if (0 < checkAvailable(LocationTypes.radio_scanner))
            // we always want to always spawn a radio scanner if it is a location
            {
                Log.LogDebug("Environment has radio_scanner locations, spawning an iscRadarTower.");

                // the format for spawning is stolen directly from how rusty/lock boxes are spawned
                Xoroshiro128Plus xoroshiro128PlusRadioScanner = new Xoroshiro128Plus(self.rng.nextUlong);
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscRadarTower"), new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random,
                }, xoroshiro128PlusRadioScanner));
                if (highlightOn)
                {
                    var radar = UnityEngine.GameObject.Find("RadarTower(Clone)");
                    radar.GetComponent<Highlight>().isOn = true;
                }

            }
        }

        private void RadiotowerTerminal_GrantUnlock(On.RoR2.RadiotowerTerminal.orig_GrantUnlock orig, RadiotowerTerminal self, Interactor interactor)
        {
            Log.LogDebug("RadiotowerTerminal_GrantUnlock"); // XXX

            if (0 == checkAvailable(LocationTypes.radio_scanner))
            {
                // there are no checks, treat the scanner as if it were a vanilla scanner
                orig(self, interactor);
                return;
            }
            var radar = UnityEngine.GameObject.Find("RadarTower(Clone)");
            radar.GetComponent<Highlight>().isOn = false;
            sendNextAvailable(LocationTypes.radio_scanner);

            // still play the effect for the scanner and lock it from being used again
            EffectManager.SpawnEffect(self.unlockEffect, new EffectData
            {
                origin = self.transform.position
            }, transmit: true);
            self.SetHasBeenPurchased(newHasBeenPurchased: true);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
