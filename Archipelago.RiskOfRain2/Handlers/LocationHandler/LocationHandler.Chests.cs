using RoR2;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class LocationHandler
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Chest like objects

        // To not have to write IL code, some weird hooks will be used.
        // The idea is to count the number of items that will be spawned and then intercept them as they are spawning
        //  to prevent only consume items we want to use as locations.

        /// <summary>
        /// Call on opening a chest. This accounts for the step in item pickups uses and submits locations.
        /// </summary>
        /// <returns>Returns true if a location was submitted.</returns>
        private bool chestOpened()
        {
            bool locationavailable = 0 < checkAvailable(LocationTypes.chest);
            // If no chests we dont need the hooks running.
            if (!locationavailable)
            {
                On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop_Chest;
                On.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath -= SacrificeArtifactManager_OnServerCharacterDeath;
                On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_ChestDrop;
            }
            // only count when checks are avaiable OR when counting does not roll over
            if (locationavailable || 0 != (chestitemsPickedUp + 1) % itemPickupStep)
            {
                chestitemsPickedUp++;
                Log.LogDebug("chest counted as towards the locations");
                updateBar(LocationTypes.chest);
            }
            else
            {
                Log.LogDebug("chest not counted as towards the locations");
            }

            // only send checks when rolling over
            if (locationavailable && 0 == chestitemsPickedUp % itemPickupStep) return sendNextAvailable(LocationTypes.chest);
            return false;
        }

        private void ChestBehavior_ItemDrop_Chest(On.RoR2.ChestBehavior.orig_ItemDrop orig, RoR2.ChestBehavior self)
        {
            // All chest like objects drop 1 item, this includes scavenger backpacks which just call this method several times.
            // Therefore we need to manually make sure the call here is not from the backpack.
            if (NetworkServer.active && self.currentPickup != UniquePickup.none && scavbackpackHash != self.GetHashCode())
            {
                chestblockitem = chestOpened();
            }
            if (!chestblockitem)
            {
                orig(self);
            }

             // the original will end up calling PickupDropletController_CreatePickupDroplet as well as other things
            chestblockitem = false;
        }

        private void SacrificeArtifactManager_OnServerCharacterDeath(On.RoR2.Artifacts.SacrificeArtifactManager.orig_OnServerCharacterDeath orig, DamageReport damageReport)
        {
            sacrificeitem = true;
            // OnServerCharacterDeath has a percent chance of calling CreatePickupDroplet_Chest.
            // Only when it is called will we want to treat it as a chest being opened.
            orig(damageReport);
            sacrificeitem = false;
        }

        private void PickupDropletController_CreatePickupDroplet_ChestDrop(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig, GenericPickupController.CreatePickupInfo pickupInfo, UnityEngine.Vector3 position, UnityEngine.Vector3 velocity)
        {
            // check if the item is being dropped by sacrifice
            if (sacrificeitem)
            {
                // if the item is from sacrifice, treat it as opening a chest
                if (chestOpened())
                {
                    Log.LogDebug($"sacrifice chest item {pickupInfo._pickupState} was used to satisfy a location and thus is consumed");
                    return;
                }
                Log.LogDebug($"sacrifice chest item {pickupInfo._pickupState} passed through");
            }
            orig(pickupInfo, position, velocity);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
